using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace Novashelf.Api.Learn;

/// <summary>Keeps lesson sections to a short read; the full source is a link away.</summary>
public static class LessonText
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

    private static bool IsHeading(IElement element) => element.LocalName is "h2" or "h3" or "h4";
}
