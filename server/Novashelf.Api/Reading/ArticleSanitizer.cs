using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Ganss.Xss;

namespace Novashelf.Api.Reading;

public static partial class ArticleSanitizer
{
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

    public static string Sanitize(string html, string baseUrl)
    {
        var document = Parser.ParseDocument($"<body>{html}</body>");
        foreach (var element in document.QuerySelectorAll(DropEntirely)) element.Remove();

        foreach (var image in document.QuerySelectorAll("img"))
        {
            var src = image.GetAttribute("src");
            if (string.IsNullOrWhiteSpace(src) || src.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                image.Remove();
        }

        RemovePageChrome(document.Body!);

        var sanitized = Parser.ParseDocument($"<body>{Sanitizer.Sanitize(document.Body!.InnerHtml, baseUrl)}</body>");
        RemoveEmptyBlocks(sanitized.Body!);
        return sanitized.Body!.InnerHtml.Trim();
    }

    private const string Blocks = "h1, h2, h3, h4, h5, h6, p, li, figure, blockquote, pre, table, img";

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

    private static void RemoveEmptyBlocks(IElement body)
    {
        foreach (var element in body.QuerySelectorAll("p, li, figure, figcaption, blockquote, h2, h3, h4, h5, h6").Reverse())
        {
            if (string.IsNullOrWhiteSpace(element.TextContent) && element.QuerySelector("img") is null)
                element.Remove();
        }
    }

    [GeneratedRegex(
        @"^\s*(?:\d+\s*min(?:ute)?s?\s+read|\d{1,2}[/.-]\d{1,2}[/.-]\d{2,4}|keep exploring)\s*$",
        RegexOptions.IgnoreCase)]
    private static partial Regex PageChromeText();
}
