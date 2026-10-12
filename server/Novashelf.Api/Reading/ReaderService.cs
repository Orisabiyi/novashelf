using System.Collections.Concurrent;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Novashelf.Api.Data;
using Novashelf.Api.Images;
using Novashelf.Api.Models;
using Novashelf.Api.Sources;

namespace Novashelf.Api.Reading;

/// <param name="Html">Sanitised article body.</param>
/// <param name="Extracted">
/// True when <paramref name="Html"/> is the full piece. False when the original page couldn't be
/// extracted and this is only the summary; the reader then points to the original.
/// </param>
public sealed record ReadableContent(
    ContentItem Item,
    string Html,
    string? Byline,
    int ReadingMinutes,
    bool Extracted);

/// <summary>
/// Builds the reader view for a content item. Articles are fetched from their original site and run
/// through a Readability port, then sanitised. APOD entries and NASA images are already complete
/// in their description, so they need no fetch.
/// </summary>
public sealed class ReaderService(
    PageExtractor extractor,
    ContentResolver resolver,
    ImageUrlSigner images,
    IArticleStore store,
    IMemoryCache cache,
    TimeProvider time)
{
    /// <summary>
    /// Bump when extraction or clean-up rules change, so stored articles are re-extracted with them.
    /// </summary>
    public const int FormatVersion = 2;

    private static readonly TimeSpan StoredArticleMaxAge = TimeSpan.FromDays(30);

    private const int WordsPerMinute = 230;

    private static readonly TimeSpan ExtractedTtl = TimeSpan.FromHours(12);
    private static readonly TimeSpan FallbackTtl = TimeSpan.FromMinutes(15);

    // Shared across instances (the service is transient): one extraction per article at a time,
    // whether a reader or background warming asked for it first.
    private static readonly ConcurrentDictionary<string, Lazy<Task<ReadableContent>>> InFlight = new();
    private static readonly SemaphoreSlim WarmingSlots = new(2);

    /// <summary>Returns null when the id doesn't exist.</summary>
    public async Task<ReadableContent?> GetAsync(string id, CancellationToken ct)
    {
        if (cache.TryGetValue(CacheKey(id), out ReadableContent? cached)) return cached;

        // A stored article carries its own details, so it's served without asking the source
        // (Spaceflight News can take several seconds to answer).
        if (ContentId.TryParseArticle(id, out _) && await LoadStoredAsync(id, item: null) is { } stored)
        {
            var readable = WithProxiedImages(stored);
            cache.Set(CacheKey(id), readable, ExtractedTtl);
            return readable;
        }

        var item = await resolver.GetAsync(id, ct);
        if (item is null) return null;
        return await GetForItemAsync(item).WaitAsync(ct);
    }

    /// <summary>
    /// Extracts articles in the background (e.g. the top of a feed page as it's served), so the
    /// reader usually finds them ready. Publishers can take 10–20s to serve a page.
    /// </summary>
    public void Warm(IEnumerable<ContentItem> items)
    {
        foreach (var item in items.Where(i => i.Kind == ContentKind.Article && i.Url is not null))
        {
            if (cache.TryGetValue(CacheKey(item.Id), out _) || InFlight.ContainsKey(item.Id)) continue;
            _ = Task.Run(async () =>
            {
                await WarmingSlots.WaitAsync();
                try
                {
                    await GetForItemAsync(item);
                }
                finally
                {
                    WarmingSlots.Release();
                }
            });
        }
    }

    private Task<ReadableContent> GetForItemAsync(ContentItem item)
    {
        if (item.Kind != ContentKind.Article || item.Url is null)
            return Task.FromResult(FromSummary(item, extracted: true));

        if (cache.TryGetValue(CacheKey(item.Id), out ReadableContent? cached)) return Task.FromResult(cached!);

        // Not tied to any one request: if the first reader leaves, the work still finishes for the next.
        var work = InFlight.GetOrAdd(item.Id, key => new Lazy<Task<ReadableContent>>(async () =>
        {
            try
            {
                // Memory, then the database, then the publisher.
                var extracted = await LoadStoredAsync(item.Id, item) ?? await ExtractAndStoreAsync(item);
                var readable = extracted is null ? FromSummary(item, extracted: false) : WithProxiedImages(extracted);
                // Retry failed extractions sooner: the site may have been briefly down.
                cache.Set(CacheKey(item.Id), readable, extracted is null ? FallbackTtl : ExtractedTtl);
                return readable;
            }
            finally
            {
                InFlight.TryRemove(item.Id, out _);
            }
        }));
        return work.Value;
    }

    private static string CacheKey(string id) => $"reader:{id}";

    /// <param name="item">The item if already known; otherwise the stored copy of its details is used.</param>
    private async Task<ReadableContent?> LoadStoredAsync(string id, ContentItem? item)
    {
        var stored = await store.GetAsync(id, CancellationToken.None);
        if (stored is null
            || stored.FormatVersion != FormatVersion
            || time.GetUtcNow() - stored.ExtractedAt > StoredArticleMaxAge)
        {
            return null;
        }

        item ??= DeserializeItem(stored.Item);
        return item is null
            ? null
            : new ReadableContent(item, stored.Html, stored.Byline, stored.ReadingMinutes, Extracted: true);
    }

    // Stored HTML keeps original image URLs; signed proxy links are added on the way out.
    private ReadableContent WithProxiedImages(ReadableContent readable) =>
        readable with { Html = ArticleImages.Rewrite(readable.Html, images) };

    private static readonly JsonSerializerOptions ItemJson = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    internal static string SerializeItem(ContentItem item) => JsonSerializer.Serialize(item, ItemJson);

    internal static ContentItem? DeserializeItem(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<ContentItem>(json, ItemJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task<ReadableContent?> ExtractAndStoreAsync(ContentItem item)
    {
        var extracted = await ExtractAsync(item, CancellationToken.None);
        if (extracted is not null)
        {
            // Only full extractions are kept; a failed one is retried on a later visit.
            await store.SaveAsync(new CachedArticle
            {
                ContentId = item.Id,
                Html = extracted.Html,
                Byline = extracted.Byline,
                ReadingMinutes = extracted.ReadingMinutes,
                FormatVersion = FormatVersion,
                ExtractedAt = time.GetUtcNow(),
                Item = SerializeItem(item),
            }, CancellationToken.None);
        }
        return extracted;
    }

    private async Task<ReadableContent?> ExtractAsync(ContentItem item, CancellationToken ct)
    {
        var page = await extractor.ExtractAsync(item.Url!, ct);
        return page is null ? null : new ReadableContent(item, page.Html, page.Byline, page.ReadingMinutes, Extracted: true);
    }

    /// <summary>Wraps an item's plain-text summary as reader HTML, one paragraph per block.</summary>
    internal static ReadableContent FromSummary(ContentItem item, bool extracted)
    {
        var paragraphs = item.Summary
            .Split(["\r\n\r\n", "\n\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => $"<p>{HtmlEncoder.Default.Encode(p)}</p>");

        var words = item.Summary.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        var minutes = Math.Max(1, (int)Math.Round(words / (double)WordsPerMinute));

        return new ReadableContent(item, string.Join("\n", paragraphs), Byline: null, minutes, extracted);
    }

}
