using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Novashelf.Api.Data;
using Novashelf.Api.Images;
using Novashelf.Api.Reading;

namespace Novashelf.Api.Learn;

public sealed record LessonSection(
    string Kind,
    string Title,
    string SourceUrl,
    string Attribution,
    string? LicenseUrl,
    string? Html,
    int ReadingMinutes,
    bool Truncated);

public sealed class LessonService(
    PageExtractor extractor,
    IArticleStore store,
    ImageUrlSigner images,
    IMemoryCache cache,
    TimeProvider time,
    ILogger<LessonService> logger)
{
    public const int FormatVersion = 6;

    private const int WordsPerMinute = 200;
    private static readonly TimeSpan MemoryTtl = TimeSpan.FromHours(12);
    private static readonly TimeSpan FailureTtl = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan StoredMaxAge = TimeSpan.FromDays(14);

    private static readonly ConcurrentDictionary<string, Lazy<Task<string?>>> InFlight = new();
    private static readonly SemaphoreSlim WarmingSlots = new(2);

    public async Task<IReadOnlyList<LessonSection>> GetSectionsAsync(Lesson lesson, CancellationToken ct)
    {
        var bodies = await Task.WhenAll(lesson.Sources.Select(source => GetBodyAsync(source).WaitAsync(ct)));
        return lesson.Sources.Select((source, i) => ToSection(source, bodies[i])).ToList();
    }

    public void Warm(Lesson lesson)
    {
        foreach (var source in lesson.Sources)
        {
            if (cache.TryGetValue(CacheKey(source), out _)) continue;
            _ = Task.Run(async () =>
            {
                await WarmingSlots.WaitAsync();
                try
                {
                    await GetBodyAsync(source);
                }
                finally
                {
                    WarmingSlots.Release();
                }
            });
        }
    }

    private LessonSection ToSection(LessonSource source, string? body)
    {
        var (shortened, truncated) = body is null ? (null, false) : LessonText.Shorten(body);
        // Shortening can end the section on a mid-page link card; clean the new ending too.
        if (truncated && shortened is not null) shortened = LessonText.RemoveTrailingPromotions(shortened);
        var html = shortened is null ? null : ArticleImages.Rewrite(shortened, images);
        var minutes = shortened is null ? 0 : Math.Max(1, (int)Math.Round(CountWords(shortened) / (double)WordsPerMinute));

        return source.Kind == LessonSource.Wikipedia
            ? new LessonSection(source.Kind, source.Title, WikipediaArticles.ArticleUrl(source.Page!),
                $"Adapted from the Simple English Wikipedia article “{source.Title}” by its contributors, " +
                "licensed under CC BY-SA 4.0. Shortened and reformatted by Novashelf.",
                "https://creativecommons.org/licenses/by-sa/4.0/", html, minutes, truncated)
            : new LessonSection(source.Kind, source.Title, source.Url!,
                "From NASA. Shortened and reformatted by Novashelf.", null, html, minutes, truncated);
    }

    private Task<string?> GetBodyAsync(LessonSource source)
    {
        var key = CacheKey(source);
        if (cache.TryGetValue(key, out string? cached)) return Task.FromResult(cached);

        // Shared between readers and warming, and not cancelled when the first reader leaves.
        return InFlight.GetOrAdd(key, _ => new Lazy<Task<string?>>(async () =>
        {
            try
            {
                var body = await LoadStoredAsync(key) ?? await FetchAndStoreAsync(key, source);
                cache.Set(key, body, body is null ? FailureTtl : MemoryTtl);
                return body;
            }
            finally
            {
                InFlight.TryRemove(key, out Lazy<Task<string?>>? _);
            }
        })).Value;
    }

    private async Task<string?> LoadStoredAsync(string key)
    {
        var stored = await store.GetAsync(key, CancellationToken.None);
        return stored is not null
               && stored.FormatVersion == FormatVersion
               && time.GetUtcNow() - stored.ExtractedAt <= StoredMaxAge
            ? stored.Html
            : null;
    }

    private async Task<string?> FetchAndStoreAsync(string key, LessonSource source)
    {
        var body = await FetchAsync(source);
        if (body is not null)
        {
            await store.SaveAsync(new CachedArticle
            {
                ContentId = key,
                Html = body,
                FormatVersion = FormatVersion,
                ExtractedAt = time.GetUtcNow(),
            }, CancellationToken.None);
        }
        return body;
    }

    private async Task<string?> FetchAsync(LessonSource source)
    {
        try
        {
            if (source.Kind == LessonSource.Nasa)
            {
                var page = await extractor.ExtractAsync(source.Url!, CancellationToken.None);
                return page is null ? null : LessonText.RemoveTrailingPromotions(page.Html);
            }

            var article = await extractor.FetchHtmlAsync(WikipediaArticles.ApiUrl(source.Page!), CancellationToken.None);
            return article is null ? null : WikipediaArticles.Clean(article.Value.Html, source.Page!);
        }
        catch (Exception ex)
        {
            logger.LogInformation(ex, "Lesson source failed: {Kind} {Source}", source.Kind, source.Page ?? source.Url);
            return null;
        }
    }

    internal static string CacheKey(LessonSource source)
    {
        var identity = source.Kind == LessonSource.Wikipedia ? source.Page! : source.Url!;
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(identity)))[..16];
        return $"learn-{source.Kind}-{hash}";
    }

    private static int CountWords(string html) =>
        System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ")
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
}
