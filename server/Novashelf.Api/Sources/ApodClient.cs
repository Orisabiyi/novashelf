using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Novashelf.Api.Models;

namespace Novashelf.Api.Sources;

/// <summary>
/// NASA's Astronomy Picture of the Day, via science.nasa.gov's <c>apod-basic</c> endpoint.
/// <para>
/// APOD moved from apod.nasa.gov to science.nasa.gov in September 2026 and the api.nasa.gov APOD
/// API now returns a placeholder for every date. <c>apod-basic</c> is the new site's own JSON
/// endpoint: it covers the whole archive (1995-06-16 onwards) and needs no API key. It isn't
/// formally documented, so responses are shape-checked and failures surface as an outage.
/// </para>
/// </summary>
public sealed partial class ApodClient(HttpClient http, IMemoryCache cache, TimeProvider time)
{
    /// <summary>The first APOD was published on 16 June 1995.</summary>
    public static readonly DateOnly FirstDate = new(1995, 6, 16);

    private static readonly TimeSpan LatestTtl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan ArchiveTtl = TimeSpan.FromHours(24);

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    /// <summary>The most recently published picture.</summary>
    public async Task<ContentItem?> GetLatestAsync(CancellationToken ct)
    {
        return await cache.GetOrCreateAsync("apod:latest", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = LatestTtl;

            var latest = await http.GetFromJsonAsync<List<ApodBasic>>("apod-basic?per_page=1", Json, ct);
            return latest is { Count: > 0 } ? Map(latest[0]) : null;
        });
    }

    public async Task<ContentItem?> GetAsync(DateOnly date, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        if (date < FirstDate || date > today) return null;

        return await cache.GetOrCreateAsync($"apod:{date:yyyy-MM-dd}", async entry =>
        {
            // Recent entries can still be corrected; archived ones don't change.
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

    /// <summary>
    /// Plain text without the leading "Explanation:" label or the site's trailing navigation
    /// lines ("Tomorrow's picture: …", "Your Sky Surprise: …"), which sometimes come along.
    /// </summary>
    internal static string CleanExplanation(string? html)
    {
        var text = ExplanationLabel().Replace(HtmlToText(html ?? ""), "");
        return PageFooter().Replace(text, "").TrimEnd();
    }

    /// <summary>
    /// hdurl is the full-size original (often 3000px+). NASA's image service resizes on request, but
    /// only under <c>/dynamicimage/assets/</c>; older entries use <c>/content/dam/</c>, which serves the
    /// same file without resizing, so rewrite those. Some early entries point to a generic site
    /// thumbnail instead of their picture — treat that as no image.
    /// </summary>
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
        @"\s*(?:Your Sky Surprise|Tomorrow[’']s picture|Explore Your Universe|Open Science|Almost Hyperspace|Follow APOD)\b[^:]{0,40}:.*$",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex PageFooter();

    /// <summary>"Image Credit:", "Video Credit &amp; Copyright:", "Illustration Credit:" …</summary>
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
