using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace Novashelf.Api.Learn;

/// <summary>Keeps lesson sections to a short read; the full source is a link away.</summary>
public static partial class LessonText
{
    /// <summary>About a four-minute read.</summary>
    public const int TargetCharacters = 5000;

    private static readonly HtmlParser Parser = new();

    /// <summary>
    /// Cuts the HTML after roughly <paramref name="target"/> characters of text, at the start of a
    /// section (heading) when one comes soon enough, otherwise after the current block. Never cuts
    /// inside a paragraph.
    /// </summary>
    public static (string Html, bool Truncated) Shorten(string html, int target = TargetCharacters)
    {
        var body = Parser.ParseDocument($"<body>{html}</body>").Body!;
        var blocks = body.Children.ToList();

        var length = 0;
        for (var i = 0; i < blocks.Count; i++)
        {
            var blockLength = blocks[i].TextContent.Length;

            // One huge block (some pages wrap most of their text in a single element) would blow
            // well past the target; if there's already a decent read, stop before it.
            if (length > target * 0.3 && length + blockLength > target * 1.3)
                return Cut(body, blocks, keep: i);

            length += blockLength;
            if (length < target || i == blocks.Count - 1) continue;

            // Past the target: prefer to stop right before the next heading if it's close.
            var cutAfter = i;
            var extra = 0;
            for (var j = i + 1; j < blocks.Count && extra < target / 3; j++)
            {
                if (IsHeading(blocks[j])) { cutAfter = j - 1; break; }
                extra += blocks[j].TextContent.Length;
            }

            return Cut(body, blocks, keep: cutAfter + 1);
        }

        return (html, false);
    }

    private static (string, bool) Cut(IElement body, List<IElement> blocks, int keep)
    {
        foreach (var block in blocks.Skip(keep)) block.Remove();
        // Don't end on a heading with nothing under it.
        while (body.LastElementChild is { } last && IsHeading(last)) last.Remove();
        return (body.InnerHtml, true);
    }

    /// <summary>
    /// NASA pages end with promotional blocks ("Big Bang News", "Do NASA Science with…",
    /// "Discover More Topics From NASA"). Drop everything from the first such heading on.
    /// </summary>
    public static string RemoveTrailingPromotions(string html)
    {
        // Removing one kind of clutter can expose another (a card behind a label behind a card),
        // so repeat until nothing changes.
        for (var pass = 0; pass < 5; pass++)
        {
            var cleaned = RemovePromotionsOnce(html);
            if (cleaned == html) break;
            html = cleaned;
        }
        return html;
    }

    private static string RemovePromotionsOnce(string html)
    {
        var body = Parser.ParseDocument($"<body>{html}</body>").Body!;
        var blocks = body.Children.ToList();
        var start = blocks.FindIndex(b => IsHeading(b) && PromotionHeading().IsMatch(b.TextContent.Trim()));
        if (start >= 0)
        {
            foreach (var block in blocks.Skip(start)) block.Remove();
        }

        // Then any run of link cards at the end. NASA cards are an image, a title, a teaser and a
        // separate "Read More" link; remove each card whole.
        bool removed;
        do
        {
            removed = false;
            if (body.LastElementChild is not { } last) break;

            // Check for a card's "Read More" first: it's also short enough to look like a label.
            if (LinkCard().IsMatch(last.TextContent.Trim()))
            {
                last.Remove();
                // Its teaser and title (a card is only a few blocks long).
                for (var back = 0; back < 3 && body.LastElementChild is { } previous; back++)
                {
                    var wasHeading = IsHeading(previous);
                    previous.Remove();
                    if (wasHeading) break;
                }
                removed = true;
            }
            else if (IsBareFigure(last) || IsStrayLabel(last))
            {
                last.Remove();
                removed = true;
            }
        } while (removed);

        while (body.LastElementChild is { } trailing && IsHeading(trailing)) trailing.Remove();

        return body.InnerHtml;
    }

    /// <summary>
    /// A short label left after a card ("Featured Story", "Mars Science Laboratory"): a paragraph of
    /// a few words that isn't a sentence.
    /// </summary>
    private static bool IsStrayLabel(IElement element)
    {
        if (element.LocalName != "p") return false;
        var text = element.TextContent.Trim();
        return text.Length is > 0 and <= 40 && !text.EndsWith('.') && !text.EndsWith('?') && !text.EndsWith('!')
               && text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length <= 5;
    }

    /// <summary>A picture with no caption text, e.g. a link card's thumbnail.</summary>
    private static bool IsBareFigure(IElement element) =>
        element.LocalName is "figure" or "img" && string.IsNullOrWhiteSpace(element.TextContent);

    [GeneratedRegex(@"(\bread more|\bread the story|\blearn more)\s*$|^read\b", RegexOptions.IgnoreCase)]
    private static partial Regex LinkCard();

    [GeneratedRegex(@"(\bnews$|^do nasa science|^discover more|^explore more|^keep exploring|^related (terms|stories)|^get involved|^media$|photojournal|e-books|resources$)", RegexOptions.IgnoreCase)]
    private static partial Regex PromotionHeading();

    private static bool IsHeading(IElement element) => element.LocalName is "h2" or "h3" or "h4";
}
