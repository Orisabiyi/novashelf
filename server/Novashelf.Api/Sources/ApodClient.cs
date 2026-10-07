using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Caching.Memory;
using Novashelf.Api.Models;

namespace Novashelf.Api.Sources;

/// <summary>
/// NASA's Astronomy Picture of the Day.
/// <para>
/// APOD moved to science.nasa.gov in 2026 and the api.nasa.gov endpoint now returns a placeholder
/// for every date. The primary source is the new site's APOD RSS feed, which carries structured
/// fields (image, explanation, credit) for the last ~30 days. Older dates still go to the legacy
/// API, so they come back automatically if NASA fixes it.
/// </para>
/// </summary>
public sealed partial class ApodClient(HttpClient http, IMemoryCache cache, IConfiguration config, TimeProvider time)
{
    /// <summary>APOD started on 16 June 1995; the API rejects earlier dates.</summary>
    public static readonly DateOnly FirstDate = new(1995, 6, 16);

    public const string FeedUrl = "https://science.nasa.gov/feed/apod-basic/";

    private static readonly TimeSpan Ttl = TimeSpan.FromHours(1);
    private static readonly XNamespace ApodNs = "https://science.nasa.gov/apod/";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private string ApiKey => config["Nasa:ApiKey"] ?? "DEMO_KEY";

    /// <summary>The most recently published picture.</summary>
    public async Task<ContentItem?> GetLatestAsync(CancellationToken ct)
    {
        var feed = await GetFeedAsync(ct);
        return feed.Items.Count > 0 ? feed.Items[0] : null;
    }

    public async Task<ContentItem?> GetAsync(DateOnly date, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        if (date < FirstDate || date > today) return null;

        var feed = await GetFeedAsync(ct);
        var id = ContentId.ForApod(date);
        var match = feed.Items.FirstOrDefault(i => i.Id == id);
        if (match is not null) return match;

        // Inside the feed's window but absent: not published (yet).
        if (feed.Items.Count > 0 && date >= feed.OldestDate) return null;

        return await GetFromLegacyApiAsync(date, ct);
    }

    private async Task<ApodFeed> GetFeedAsync(CancellationToken ct)
    {
        return (await cache.GetOrCreateAsync("apod:feed", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = Ttl;
            var xml = await http.GetStringAsync(FeedUrl, ct);
            return ParseFeed(xml);
        }))!;
    }

    internal static ApodFeed ParseFeed(string xml)
    {
        // RSS never needs a DTD; refuse to process one (no entity expansion, no external fetches).
        using var reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Ignore,
            XmlResolver = null,
        });
        var channel = XDocument.Load(reader).Root?.Element("channel");

        var items = (channel?.Elements("item") ?? [])
            .Select(MapFeedItem)
            .OfType<ContentItem>()
            .OrderByDescending(i => i.PublishedAt)
            .ToList();

        var oldest = items.Count > 0 ? DateOnly.FromDateTime(items[^1].PublishedAt!.Value.UtcDateTime) : DateOnly.MaxValue;
        return new ApodFeed(items, oldest);
    }

    private static ContentItem? MapFeedItem(XElement item)
    {
        var link = item.Element("link")?.Value.Trim();
        var title = item.Element("title")?.Value.Trim();
        if (string.IsNullOrEmpty(link) || string.IsNullOrEmpty(title)) return null;

        var date = DateFromLink(link) ?? DateFromPubDate(item.Element("pubDate")?.Value);
        if (date is null) return null;

        var explanation = HtmlToText(item.Element(ApodNs + "explanation")?.Value ?? item.Element("description")?.Value ?? "");
        explanation = ExplanationLabel().Replace(explanation, "");

        var credit = HtmlToText(item.Element(ApodNs + "credit")?.Value ?? "");

        return new ContentItem(
            Id: ContentId.ForApod(date.Value),
            Kind: ContentKind.Apod,
            Title: title,
            Summary: explanation,
            ImageUrl: ResizedImage(item.Element(ApodNs + "hdurl")?.Value),
            Url: link,
            Publisher: string.IsNullOrEmpty(credit) ? "NASA APOD" : Truncate(credit, 120),
            PublishedAt: new DateTimeOffset(date.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
    }

    /// <summary>Links look like <c>…/image-article/apod-2026-october-7-supernova-remnant-pa-30/</c>.</summary>
    private static DateOnly? DateFromLink(string link)
    {
        var m = LinkDate().Match(link);
        if (!m.Success) return null;

        return DateTime.TryParseExact($"{m.Groups[1].Value} {m.Groups[2].Value} {m.Groups[3].Value}", "yyyy MMMM d",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? DateOnly.FromDateTime(parsed)
            : null;
    }

    private static DateOnly? DateFromPubDate(string? pubDate) =>
        DateTimeOffset.TryParse(pubDate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? DateOnly.FromDateTime(parsed.UtcDateTime)
            : null;

    /// <summary>
    /// The feed's hdurl is the full-size original (often 3000px+). NASA's image service resizes on
    /// request, so ask for a web-sized rendition. Non-image media (e.g. video embeds) get no image.
    /// </summary>
    internal static string? ResizedImage(string? hdUrl)
    {
        if (!Uri.TryCreate(hdUrl?.Trim(), UriKind.Absolute, out var uri) || uri.Host != "assets.science.nasa.gov")
            return null;

        return $"{uri.GetLeftPart(UriPartial.Path)}?w=1600&fit=clip";
    }

    private static string HtmlToText(string html)
    {
        // Line/paragraph breaks become spaces; inline tags (<a>, <strong>) vanish so punctuation stays attached.
        var text = WebUtility.HtmlDecode(HtmlTag().Replace(BlockBreak().Replace(html, " "), ""));
        return Whitespace().Replace(text, " ").Trim();
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..(max - 1)].TrimEnd() + "…";

    [GeneratedRegex(@"apod-(\d{4})-([a-z]+)-(\d{1,2})-", RegexOptions.IgnoreCase)]
    private static partial Regex LinkDate();

    [GeneratedRegex(@"<(br|/p|/div|/li)\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex BlockBreak();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTag();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"^Explanation:\s*", RegexOptions.IgnoreCase)]
    private static partial Regex ExplanationLabel();

    internal sealed record ApodFeed(IReadOnlyList<ContentItem> Items, DateOnly OldestDate);

    private async Task<ContentItem?> GetFromLegacyApiAsync(DateOnly date, CancellationToken ct)
    {
        var dateText = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return await cache.GetOrCreateAsync($"apod:{dateText}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = Ttl;

            using var response = await http.GetAsync($"planetary/apod?api_key={ApiKey}&date={dateText}&thumbs=true", ct);
            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest) return null;
            response.EnsureSuccessStatusCode();

            var apod = await response.Content.ReadFromJsonAsync<ApodResponse>(Json, ct);
            if (apod is null) return null;

            // The legacy API returns a placeholder (NASA logo, title "NASA Science") for every date
            // since the move. Surface that as an outage, not as content.
            if (IsPlaceholder(apod)) throw new HttpRequestException("APOD API returned placeholder data");

            return Map(apod);
        });
    }

    internal static bool IsPlaceholder(ApodResponse a) =>
        a.Title.Trim() == "NASA Science"
        || (a.Url?.Contains("/nasa-logo", StringComparison.OrdinalIgnoreCase) ?? false);

    internal static ContentItem Map(ApodResponse a)
    {
        var date = DateOnly.ParseExact(a.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var image = a.MediaType == "image" ? a.Url : a.ThumbnailUrl;

        return new ContentItem(
            Id: ContentId.ForApod(date),
            Kind: ContentKind.Apod,
            Title: a.Title.Trim(),
            Summary: a.Explanation.Trim(),
            ImageUrl: image,
            Url: $"https://apod.nasa.gov/apod/ap{date:yyMMdd}.html",
            Publisher: string.IsNullOrWhiteSpace(a.Copyright) ? "NASA APOD" : a.Copyright.Trim(),
            PublishedAt: new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
    }

    internal sealed record ApodResponse(
        string Date,
        string Title,
        string Explanation,
        string MediaType,
        string? Url,
        string? ThumbnailUrl,
        string? Copyright);
}
