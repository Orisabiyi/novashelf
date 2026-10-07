using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Novashelf.Api.Models;

namespace Novashelf.Api.Sources;

/// <summary>Client for https://api.spaceflightnewsapi.net (v4). No API key required.</summary>
public sealed class SpaceflightNewsClient(HttpClient http, IMemoryCache cache)
{
    public const int PageSize = 20;
    private static readonly TimeSpan FeedTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ArticleTtl = TimeSpan.FromHours(6);

    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    /// <param name="keywords">Optional title keywords; an article matches if its title contains any of them.</param>
    public async Task<PagedResult<ContentItem>> GetArticlesAsync(IReadOnlyCollection<string>? keywords, int page, CancellationToken ct)
    {
        var titleFilter = keywords is { Count: > 0 } ? string.Join(',', keywords) : null;
        var key = $"sfn:feed:{titleFilter}:{page}";

        return (await cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = FeedTtl;

            var url = $"v4/articles/?limit={PageSize}&offset={(page - 1) * PageSize}";
            if (titleFilter is not null) url += "&title_contains_one=" + Uri.EscapeDataString(titleFilter);

            var response = await http.GetFromJsonAsync<SfnList>(url, Json, ct)
                ?? throw new HttpRequestException("Empty response from Spaceflight News API");

            var items = response.Results.Where(a => !IsApod(a)).Select(Map).ToList();
            return new PagedResult<ContentItem>(items, page, response.Next is not null);
        }))!;
    }

    public async Task<ContentItem?> GetArticleAsync(int id, CancellationToken ct)
    {
        return await cache.GetOrCreateAsync($"sfn:article:{id}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = ArticleTtl;

            using var response = await http.GetAsync($"v4/articles/{id}/", ct);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();

            var article = await response.Content.ReadFromJsonAsync<SfnArticle>(Json, ct);
            return article is null ? null : Map(article);
        });
    }

    internal static ContentItem Map(SfnArticle a) => new(
        Id: ContentId.ForArticle(a.Id),
        Kind: ContentKind.Article,
        Title: a.Title.Trim(),
        Summary: CleanSummary(a.Summary),
        ImageUrl: string.IsNullOrWhiteSpace(a.ImageUrl) ? null : a.ImageUrl,
        Url: a.Url,
        Publisher: a.NewsSite,
        PublishedAt: a.PublishedAt,
        Topic: Topics.Tag(a.Title));

    /// <summary>
    /// Spaceflight News also indexes NASA's APOD pages, with summaries scraped from the page chrome.
    /// APOD has its own endpoint, so keep it out of the article feed. (The API has no exclude filter.)
    /// </summary>
    internal static bool IsApod(SfnArticle a) =>
        a.Title.TrimStart().StartsWith("APOD:", StringComparison.OrdinalIgnoreCase)
        || a.Url.Contains("science.nasa.gov/image-article/apod", StringComparison.OrdinalIgnoreCase);

    // WordPress feeds append "The post <title> appeared first on <site>." to the summary.
    private static readonly Regex WordPressFooter = new(@"\s*The post .+ appeared first on .+\.\s*$", RegexOptions.Singleline | RegexOptions.Compiled);

    internal static string CleanSummary(string summary) => WordPressFooter.Replace(summary, "").Trim();

    internal sealed record SfnList(List<SfnArticle> Results, string? Next);

    internal sealed record SfnArticle(
        int Id,
        string Title,
        string Url,
        string? ImageUrl,
        string? NewsSite,
        string Summary,
        DateTimeOffset? PublishedAt);
}
