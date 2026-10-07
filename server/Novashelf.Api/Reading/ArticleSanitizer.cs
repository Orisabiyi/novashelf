using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Ganss.Xss;

namespace Novashelf.Api.Reading;

/// <summary>
/// Reduces extracted article HTML to a small, safe subset for the reader: text structure, links,
/// images, figures and tables. No scripts, styles, classes, inline handlers or embeds.
/// </summary>
public static partial class ArticleSanitizer
{
    // Removed together with everything inside them (their text is never article content).
    private const string DropEntirely =
        "script, style, noscript, template, iframe, frame, object, embed, form, button, input, select, " +
        "textarea, svg, canvas, video, audio, nav, aside, footer, dialog";

    private static readonly HtmlParser Parser = new();

    private static readonly HtmlSanitizer Sanitizer = CreateSanitizer();

    private static HtmlSanitizer CreateSanitizer()
    {
        var sanitizer = new HtmlSanitizer(new HtmlSanitizerOptions
        {
            AllowedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "p", "br", "hr", "h2", "h3", "h4", "h5", "h6",
                "strong", "b", "em", "i", "u", "s", "sub", "sup", "small", "mark", "abbr", "time",
                "a", "ul", "ol", "li", "dl", "dt", "dd", "blockquote", "q", "cite", "pre", "code",
                "figure", "figcaption", "img",
                "table", "caption", "thead", "tbody", "tfoot", "tr", "th", "td",
            },
            AllowedAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "href", "src", "alt", "title", "width", "height", "colspan", "rowspan", "datetime", "cite",
            },
            AllowedSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "http", "https" },
            UriAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "href", "src", "cite" },
            AllowedCssProperties = new HashSet<string>(),
            AllowedAtRules = new HashSet<AngleSharp.Css.Dom.CssRuleType>(),
            AllowedCssClasses = new HashSet<string>(),
        })
        {
            // Unwrap disallowed containers (div, span, section…) instead of losing their text.
            KeepChildNodes = true,
        };

        sanitizer.PostProcessNode += (_, e) =>
        {
            if (e.Node is not IElement element) return;
            switch (element.LocalName)
            {
                case "a":
                    element.SetAttribute("target", "_blank");
                    element.SetAttribute("rel", "noopener noreferrer nofollow");
                    break;
                case "img":
                    element.SetAttribute("loading", "lazy");
                    element.SetAttribute("decoding", "async");
                    // Some publishers block hot-linked images that carry a foreign Referer.
                    element.SetAttribute("referrerpolicy", "no-referrer");
                    break;
            }
        };

        return sanitizer;
    }

    /// <param name="baseUrl">The article's address; relative links and images resolve against it.</param>
    public static string Sanitize(string html, string baseUrl)
    {
        var document = Parser.ParseDocument($"<body>{html}</body>");
        foreach (var element in document.QuerySelectorAll(DropEntirely)) element.Remove();

        // Images without a usable source (e.g. leftover lazy-load placeholders) only leave gaps.
        foreach (var image in document.QuerySelectorAll("img"))
        {
            var src = image.GetAttribute("src");
            if (string.IsNullOrWhiteSpace(src) || src.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                image.Remove();
        }

        RemovePageChrome(document.Body!);

        return Sanitizer.Sanitize(document.Body!.InnerHtml, baseUrl).Trim();
    }

    private const string Blocks = "h1, h2, h3, h4, h5, h6, p, li, figure, blockquote, pre, table, img";

    /// <summary>
    /// Publisher templates leave bits of page furniture inside the article body: "2 Min Read",
    /// a bare date line, "Keep Exploring", and "More from …" headings with nothing after them.
    /// The reader already shows reading time and date, so drop them.
    /// </summary>
    private static void RemovePageChrome(IElement body)
    {
        foreach (var element in body.QuerySelectorAll("p, div, span, h2, h3, h4, h5, h6"))
        {
            if (element.QuerySelector("img") is null && PageChromeText().IsMatch(element.TextContent))
                element.Remove();
        }

        // A heading that ends the article introduces content that wasn't extracted.
        while (body.QuerySelectorAll(Blocks).LastOrDefault() is { } last
               && last.LocalName is "h1" or "h2" or "h3" or "h4" or "h5" or "h6")
        {
            last.Remove();
        }
    }

    [GeneratedRegex(
        @"^\s*(?:\d+\s*min(?:ute)?s?\s+read|\d{1,2}[/.-]\d{1,2}[/.-]\d{2,4}|keep exploring)\s*$",
        RegexOptions.IgnoreCase)]
    private static partial Regex PageChromeText();
}
