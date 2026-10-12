using AngleSharp.Html.Parser;
using Novashelf.Api.Images;

namespace Novashelf.Api.Reading;

/// <summary>Points article images at the resizing proxy, with a srcset so phones get small copies.</summary>
public static class ArticleImages
{
    private static readonly int[] SrcsetWidths = [480, 640, 960, 1280, 1600];
    private const int DefaultWidth = 1280;

    // The reader column is at most ~80ch (about 880px); below that, images fill the screen.
    private const string Sizes = "(min-width: 960px) 880px, 100vw";

    private static readonly HtmlParser Parser = new();

    public static string Rewrite(string html, ImageUrlSigner signer)
    {
        var document = Parser.ParseDocument($"<body>{html}</body>");
        var changed = false;

        foreach (var image in document.QuerySelectorAll("img"))
        {
            var proxied = signer.Proxy(image.GetAttribute("src"));
            if (proxied is null || !proxied.StartsWith(ImageUrlSigner.Path, StringComparison.Ordinal)) continue;

            image.SetAttribute("src", $"{proxied}&w={DefaultWidth}");
            image.SetAttribute("srcset", string.Join(", ", SrcsetWidths.Select(w => $"{proxied}&w={w} {w}w")));
            image.SetAttribute("sizes", Sizes);
            changed = true;
        }

        return changed ? document.Body!.InnerHtml : html;
    }
}
