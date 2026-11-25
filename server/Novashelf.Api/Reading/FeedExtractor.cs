using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Microsoft.Extensions.Caching.Memory;

namespace Novashelf.Api.Reading;

// Some publishers put their article pages behind bot challenges (e.g. Cloudflare's "Just a moment...")
// but leave their RSS feed open. WordPress feeds carry the full article in <content:encoded>, so when a
// page can't be fetched we look for the article in the publisher's feed instead. Feeds only list the
// latest posts, so this works for recent articles; ReaderWarmingService stores them while they're there.
public sealed partial class FeedExtractor(HttpClient http, IMemoryCache cache, ILogger<FeedExtractor> logger)
{
    private const int MinArticleTextLength = 400;

    private static readonly TimeSpan FeedTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan MissingFeedTtl = TimeSpan.FromHours(1);

    private static readonly XNamespace ContentNs = "http://purl.org/rss/1.0/modules/content/";
    private static readonly XNamespace DcNs = "http://purl.org/dc/elements/1.1/";

    private static readonly HtmlParser Parser = new();

    internal sealed record FeedEntry(string Link, string? Html, string? Creator);

    public async Task<ExtractedPage?> ExtractAsync(string articleUrl, CancellationToken ct)
    {
        if (!Uri.TryCreate(articleUrl, UriKind.Absolute, out var article)) return null;

        var entries = await GetFeedAsync(article, ct);
        var entry = entries.FirstOrDefault(e => IsSameArticle(e.Link, article));
        if (entry?.Html is null) return null;

        var body = ArticleSanitizer.Sanitize(CleanContent(entry.Html, article), article.ToString());
        var text = Parser.ParseDocument($"<body>{body}</body>").Body!.TextContent.Trim();
        if (text.Length < MinArticleTextLength) return null;

        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        var minutes = Math.Max(1, (int)Math.Round(words / (double)ReaderService.WordsPerMinute));
        var byline = string.IsNullOrWhiteSpace(entry.Creator) ? null : entry.Creator.Trim();
        return new ExtractedPage(body, byline, minutes);
    }

    private async Task<IReadOnlyList<FeedEntry>> GetFeedAsync(Uri article, CancellationToken ct)
    {
        var feedUrl = FeedUrlFor(article);
        var key = $"publisher-feed:{feedUrl}";
        if (cache.TryGetValue(key, out IReadOnlyList<FeedEntry>? cached)) return cached!;

        IReadOnlyList<FeedEntry> entries;
        try
        {
            entries = await FetchFeedAsync(feedUrl, ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogInformation(ex, "Feed fallback: couldn't read {FeedUrl}", feedUrl);
            entries = [];
        }

        cache.Set(key, entries, entries.Count == 0 ? MissingFeedTtl : FeedTtl);
        return entries;
    }

    private async Task<IReadOnlyList<FeedEntry>> FetchFeedAsync(string feedUrl, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, feedUrl);
        request.Headers.Accept.ParseAdd("application/rss+xml,application/xml;q=0.9,text/xml;q=0.8");

        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogInformation("Feed fallback: {FeedUrl} returned {Status}", feedUrl, (int)response.StatusCode);
            return [];
        }

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (mediaType is not null && !mediaType.Contains("xml", StringComparison.OrdinalIgnoreCase)) return [];

        var xml = await PageExtractor.ReadLimitedAsync(response.Content, ct);
        return xml is null ? [] : ParseRss(xml);
    }

    internal static string FeedUrlFor(Uri article) => $"{article.Scheme}://{article.Authority}/feed/";

    internal static IReadOnlyList<FeedEntry> ParseRss(string xml)
    {
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
        using var reader = XmlReader.Create(new StringReader(xml), settings);
        var document = XDocument.Load(reader);

        return document.Descendants("item")
            .Select(item => new FeedEntry(
                Link: item.Element("link")?.Value.Trim() ?? "",
                Html: item.Element(ContentNs + "encoded")?.Value,
                Creator: item.Element(DcNs + "creator")?.Value))
            .Where(e => e.Link.Length > 0)
            .ToList();
    }

    internal static bool IsSameArticle(string link, Uri article) =>
        Uri.TryCreate(link, UriKind.Absolute, out var candidate)
        && string.Equals(BareHost(candidate), BareHost(article), StringComparison.OrdinalIgnoreCase)
        && string.Equals(candidate.AbsolutePath.TrimEnd('/'), article.AbsolutePath.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);

    private static string BareHost(Uri uri) =>
        uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? uri.Host[4..] : uri.Host;

    // Feed content carries things the article page wouldn't show in the reader: the featured image
    // (already the reader's hero), "The post ... appeared first on ..." and banner ads linking elsewhere.
    internal static string CleanContent(string html, Uri article)
    {
        var body = Parser.ParseDocument($"<body>{html}</body>").Body!;

        foreach (var image in body.QuerySelectorAll("img.wp-post-image")) image.Remove();

        foreach (var paragraph in body.QuerySelectorAll("p"))
        {
            if (AppearedFirstOn().IsMatch(paragraph.TextContent)) paragraph.Remove();
        }

        foreach (var link in body.QuerySelectorAll("a"))
        {
            if (IsOffsiteBanner(link, article)) (OnlyChildOf(link) ?? link).Remove();
        }

        return body.InnerHtml;
    }

    private static bool IsOffsiteBanner(IElement link, Uri article) =>
        link.QuerySelector("img") is not null
        && string.IsNullOrWhiteSpace(link.TextContent)
        && Uri.TryCreate(link.GetAttribute("href"), UriKind.Absolute, out var target)
        && !string.Equals(target.Host, article.Host, StringComparison.OrdinalIgnoreCase);

    // The <p> wrapping a banner would be left empty, so remove it with the banner.
    private static IElement? OnlyChildOf(IElement element) =>
        element.ParentElement is { LocalName: "p" } parent
        && parent.Children.Length == 1
        && string.IsNullOrWhiteSpace(parent.TextContent)
            ? parent
            : null;

    [GeneratedRegex(@"^\s*The post .+ appeared first on .+\.\s*$", RegexOptions.Singleline)]
    private static partial Regex AppearedFirstOn();
}
