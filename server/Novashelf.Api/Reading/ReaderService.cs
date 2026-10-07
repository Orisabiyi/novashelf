using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Caching.Memory;
using Novashelf.Api.Images;
using Novashelf.Api.Models;
using Novashelf.Api.Sources;
using ReadabilityParser = SmartReader.Reader;

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
    HttpClient http,
    ContentResolver resolver,
    ImageUrlSigner images,
    IMemoryCache cache,
    ILogger<ReaderService> logger)
{
    private const int MaxPageBytes = 5 * 1024 * 1024;
    private const int MinArticleTextLength = 400;
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
                var extracted = await ExtractAsync(item, CancellationToken.None);
                var readable = extracted ?? FromSummary(item, extracted: false);
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

    private async Task<ReadableContent?> ExtractAsync(ContentItem item, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, item.Url);
            request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml;q=0.9,*/*;q=0.1");

            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogInformation("Reader: {Url} returned {Status}", item.Url, (int)response.StatusCode);
                return null;
            }

            var mediaType = response.Content.Headers.ContentType?.MediaType;
            if (mediaType is not null && !mediaType.Contains("html", StringComparison.OrdinalIgnoreCase))
                return null;

            var html = await ReadLimitedAsync(response.Content, ct);
            if (html is null)
            {
                logger.LogInformation("Reader: {Url} is larger than {Max} bytes", item.Url, MaxPageBytes);
                return null;
            }

            // After redirects, resolve relative links against where the page actually lives.
            var pageUrl = response.RequestMessage?.RequestUri?.ToString() ?? item.Url!;
            var article = new ReadabilityParser(pageUrl, html).GetArticle();
            if (!article.IsReadable || string.IsNullOrWhiteSpace(article.Content)) return null;

            var body = ArticleImages.Rewrite(ArticleSanitizer.Sanitize(article.Content, pageUrl), images);
            if ((article.TextContent?.Trim().Length ?? 0) < MinArticleTextLength) return null;

            var minutes = (int)Math.Round(article.TimeToRead.TotalMinutes);
            var byline = FirstNonEmpty(article.Byline, article.Author);
            return new ReadableContent(item, body, byline, Math.Max(1, minutes), Extracted: true);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            // Network errors, refused private addresses, timeouts or a page the parser chokes on:
            // all fall back to the summary rather than failing the request.
            logger.LogInformation(ex, "Reader: couldn't extract {Url}", item.Url);
            return null;
        }
    }

    /// <summary>Reads the body up to <see cref="MaxPageBytes"/>; null if it's bigger.</summary>
    private static async Task<string?> ReadLimitedAsync(HttpContent content, CancellationToken ct)
    {
        if (content.Headers.ContentLength > MaxPageBytes) return null;

        await using var stream = await content.ReadAsStreamAsync(ct);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + read > MaxPageBytes) return null;
            buffer.Write(chunk, 0, read);
        }

        return GetEncoding(content.Headers.ContentType).GetString(buffer.GetBuffer(), 0, (int)buffer.Length);
    }

    private static Encoding GetEncoding(MediaTypeHeaderValue? contentType)
    {
        try
        {
            return contentType?.CharSet is { Length: > 0 } charset
                ? Encoding.GetEncoding(charset.Trim('"'))
                : Encoding.UTF8;
        }
        catch (ArgumentException)
        {
            return Encoding.UTF8;
        }
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

    private static string? FirstNonEmpty(params string?[] values) =>
        values.Select(v => v?.Trim()).FirstOrDefault(v => !string.IsNullOrEmpty(v));
}
