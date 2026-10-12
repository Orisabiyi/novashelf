using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Novashelf.Api.Reading;

namespace Novashelf.Api.Learn;

/// <summary>
/// Turns a Simple English Wikipedia article (Parsoid HTML from the REST API) into a readable lesson
/// section: no reference markers, infoboxes, navigation boxes or reference/link sections, and
/// larger images than the article's thumbnails.
/// </summary>
public static class WikipediaArticles
{
    public const string Host = "simple.wikipedia.org";

    public static string ApiUrl(string page) => $"https://{Host}/api/rest_v1/page/html/{Uri.EscapeDataString(page)}?redirect=true";

    public static string ArticleUrl(string page) => $"https://{Host}/wiki/{Uri.EscapeDataString(page)}";

    // Sections that list references or links rather than explain anything.
    private static readonly HashSet<string> SkippedSections = new(StringComparer.OrdinalIgnoreCase)
    {
        "References", "Notes", "Sources", "Other websites", "Related pages", "Further reading",
        "External links", "See also", "Gallery", "Bibliography", "Footnotes", "More reading",
    };

    private const string Clutter =
        "sup.reference, sup.mw-ref, .mw-ref, .reference, .mw-editsection, .infobox, .navbox, .hatnote, " +
        ".noprint, .metadata, .ambox, .sistersitebox, .mw-empty-elt, table, style, link, meta";

    private static readonly HtmlParser Parser = new();

    public static string Clean(string parsoidHtml, string page)
    {
        var document = Parser.ParseDocument(parsoidHtml);
        var body = document.Body ?? throw new InvalidDataException("Wikipedia returned no body.");

        foreach (var heading in body.QuerySelectorAll("h2").ToList())
        {
            if (!SkippedSections.Contains(heading.TextContent.Trim())) continue;
            // Parsoid wraps each section in <section>; remove the whole thing.
            if (heading.ParentElement is { LocalName: "section" } section) section.Remove();
            else heading.Remove();
        }

        foreach (var element in body.QuerySelectorAll(Clutter).ToList()) element.Remove();

        foreach (var image in body.QuerySelectorAll("img")) UseLargerRendition(image);

        // Relative links ("./Planet") resolve against the article's address.
        return ArticleSanitizer.Sanitize(body.InnerHtml, ArticleUrl(page));
    }

    /// <summary>Article thumbnails are ~220px; take the largest candidate from srcset instead.</summary>
    private static void UseLargerRendition(IElement image)
    {
        var largest = image.GetAttribute("srcset")?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(candidate => candidate.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0])
            .LastOrDefault();
        if (!string.IsNullOrEmpty(largest)) image.SetAttribute("src", largest);
    }
}
