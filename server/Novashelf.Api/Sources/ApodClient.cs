using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Novashelf.Api.Models;

namespace Novashelf.Api.Sources;

// apod-basic is science.nasa.gov's own, undocumented JSON endpoint (api.nasa.gov's APOD API now
// returns a placeholder), so responses are shape-checked and failures surface as an outage.
public sealed partial class ApodClient(HttpClient http, IMemoryCache cache, TimeProvider time)
{
    public static readonly DateOnly FirstDate = new(1995, 6, 16);

    private static readonly TimeSpan LatestTtl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan ArchiveTtl = TimeSpan.FromHours(24);

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public async Task<ContentItem?> GetLatestAsync(CancellationToken ct)
    {
        return await cache.GetOrCreateAsync("apod:latest", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = LatestTtl;

            var latest = await http.GetFromJsonAsync<List<ApodBasic>>("apod-basic?per_page=1", Json, ct);
            return latest is { Count: > 0 } ? Map(latest[0]) : null;
        });
    }

    public const int ArchivePageSize = 24;

    public async Task<PagedResult<ContentItem>> GetArchivePageAsync(int page, CancellationToken ct)
    {
        return (await cache.GetOrCreateAsync($"apod:archive:{page}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = page == 1 ? LatestTtl : TimeSpan.FromHours(6);

            using var response = await http.GetAsync($"apod-basic?per_page={ArchivePageSize}&page={page}", ct);
            response.EnsureSuccessStatusCode();

            var entries = await response.Content.ReadFromJsonAsync<List<ApodBasic>>(Json, ct) ?? [];
            var totalPages = response.Headers.TryGetValues("X-WP-TotalPages", out var values)
                && int.TryParse(values.FirstOrDefault(), out var total) ? total : page;

            var items = new List<ContentItem>(entries.Count);
            foreach (var apod in entries)
            {
                try
                {
                    items.Add(Map(apod));
                }
                catch (JsonException)
                {
                }
            }
            return new PagedResult<ContentItem>(items, page, page < totalPages && items.Count > 0);
        }))!;
    }

    public async Task<ContentItem?> GetAsync(DateOnly date, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        if (date < FirstDate || date > today) return null;

        return await cache.GetOrCreateAsync($"apod:{date:yyyy-MM-dd}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = date >= today.AddDays(-2) ? LatestTtl : ArchiveTtl;

            using var response = await http.GetAsync($"apod-basic/{date.ToString("yyMMdd", CultureInfo.InvariantCulture)}", ct);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();

            var apod = await response.Content.ReadFromJsonAsync<ApodBasic>(Json, ct);
            return apod is null ? null : Map(apod);
        });
    }

    internal static ContentItem Map(ApodBasic a)
    {
        if (string.IsNullOrWhiteSpace(a.Title)
            || !DateOnly.TryParseExact(a.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            throw new JsonException("apod-basic response is missing its date or title");
        }

        var credit = CreditLabel().Replace(HtmlToText(a.Credit ?? a.Copyright ?? ""), "");

        return new ContentItem(
            Id: ContentId.ForApod(date),
            Kind: ContentKind.Apod,
            Title: WebUtility.HtmlDecode(a.Title).Trim(),
            Summary: CleanExplanation(a.Explanation),
            ImageUrl: ResizedImage(a.Hdurl),
            Url: a.Permalink ?? a.Url,
            Publisher: string.IsNullOrEmpty(credit) ? "NASA APOD" : Truncate(credit, 120),
            PublishedAt: new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
    }

    internal static string CleanExplanation(string? html)
    {
        var text = ExplanationLabel().Replace(HtmlToText(html ?? ""), "");
        return PageFooter().Replace(text, "").TrimEnd();
    }

    // hdurl is the full-size original (often 3000px+). NASA's image service resizes on request, but
    // only under /dynamicimage/assets/; older entries use /content/dam/, which serves the
    // same file without resizing, so rewrite those. Some early entries point to a generic site
    // thumbnail instead of their picture — treat that as no image.
    internal static string? ResizedImage(string? hdUrl)
    {
        if (!Uri.TryCreate(hdUrl?.Trim(), UriKind.Absolute, out var uri) || uri.Host != "assets.science.nasa.gov")
            return null;

        var path = uri.AbsolutePath;
        if (path.Contains("/news-thumbnail.", StringComparison.OrdinalIgnoreCase)) return null;
        if (path.StartsWith("/content/dam/", StringComparison.Ordinal))
            path = "/dynamicimage/assets/" + path["/content/dam/".Length..];

        // No size parameters: NASA's on-demand resizer can take tens of seconds on a cold cache,
        // while the standard rendition comes back quickly. Our image proxy resizes it.
        return $"https://assets.science.nasa.gov{path}";
    }

    private static string HtmlToText(string html)
    {
        // Line/paragraph breaks become spaces; inline tags (<a>, <strong>) vanish so punctuation stays attached.
        var text = WebUtility.HtmlDecode(HtmlTag().Replace(BlockBreak().Replace(html, " "), ""));
        return Whitespace().Replace(text, " ").Replace(" ,", ",").Trim();
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..(max - 1)].TrimEnd() + "…";

    [GeneratedRegex(@"<(br|/p|/div|/li)\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex BlockBreak();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTag();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"^Explanation:\s*", RegexOptions.IgnoreCase)]
    private static partial Regex ExplanationLabel();

    [GeneratedRegex(
        @"\s*(?:Your Sky Surprise|Tomorrow[’']s picture|Explore Your Universe|Open Science|Almost Hyperspace|Follow APOD|APOD[’']s email)\b[^:]{0,80}:.*$",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex PageFooter();

    [GeneratedRegex(@"^[\w\s&]*Credit[\w\s&]*:\s*", RegexOptions.IgnoreCase)]
    private static partial Regex CreditLabel();

    internal sealed record ApodBasic(
        string? Date,
        string? Title,
        string? MediaType,
        string? Explanation,
        string? Credit,
        string? Copyright,
        string? Permalink,
        string? Url,
        string? Hdurl);
}
