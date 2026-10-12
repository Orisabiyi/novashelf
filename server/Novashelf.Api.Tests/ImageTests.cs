using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Novashelf.Api.Images;
using Novashelf.Api.Models;
using Novashelf.Api.Reading;
using SkiaSharp;

namespace Novashelf.Api.Tests;

public class ImageUrlSignerTests
{
    private static readonly ImageUrlSigner Signer = new(Encoding.UTF8.GetBytes("test-key"));
    private const string Source = "https://www.esa.int/images/mars cloud.png";

    private static (string Src, string Sig) Parse(string proxied)
    {
        var query = QueryHelpers.ParseQuery(new Uri("http://x" + proxied).Query);
        return (query["src"]!, query["sig"]!);
    }

    [Fact]
    public void Proxied_urls_round_trip_and_verify()
    {
        var proxied = Signer.Proxy(Source)!;
        Assert.StartsWith("/api/img?src=", proxied);

        var (src, sig) = Parse(proxied);
        Assert.Equal(Source, src);
        Assert.True(Signer.Verify(src, sig));
    }

    [Fact]
    public void Tampered_source_or_signature_is_rejected()
    {
        var (src, sig) = Parse(Signer.Proxy(Source)!);

        Assert.False(Signer.Verify("https://169.254.169.254/latest/meta-data", sig));
        Assert.False(Signer.Verify(src, sig[..^1] + (sig[^1] == 'A' ? 'B' : 'A')));
        Assert.False(Signer.Verify(src, null));
        Assert.False(new ImageUrlSigner(Encoding.UTF8.GetBytes("other-key")).Verify(src, sig));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("data:image/png;base64,AAAA")]
    [InlineData("javascript:alert(1)")]
    [InlineData("/relative/path.jpg")]
    public void Non_http_sources_are_left_alone(string? source) =>
        Assert.Equal(source, Signer.Proxy(source));

    [Theory]
    [InlineData(null, 1280)]
    [InlineData(0, 1280)]
    [InlineData(1, 320)]
    [InlineData(320, 320)]
    [InlineData(321, 480)]
    [InlineData(700, 960)]
    [InlineData(5000, 2048)]
    public void Widths_snap_up_to_a_standard_size(int? requested, int expected) =>
        Assert.Equal(expected, ImageUrlSigner.SnapWidth(requested));

    [Fact]
    public void Items_and_pages_get_proxied_images()
    {
        var item = new ContentItem("sfn-1", ContentKind.Article, "t", "s", Source, null, null, null);
        var page = Signer.Proxied(new PagedResult<ContentItem>([item, item with { ImageUrl = null }], 1, false));

        Assert.StartsWith("/api/img?", page.Items[0].ImageUrl);
        Assert.Null(page.Items[1].ImageUrl);
    }
}

public class ArticleImagesTests
{
    private static readonly ImageUrlSigner Signer = new(Encoding.UTF8.GetBytes("test-key"));

    [Fact]
    public void Article_images_get_a_proxied_src_and_srcset()
    {
        var html = ArticleImages.Rewrite("<p>Hi</p><img src=\"https://www.esa.int/a.png\" alt=\"Mars\" loading=\"lazy\">", Signer);

        Assert.Contains("src=\"/api/img?src=https%3A%2F%2Fwww.esa.int%2Fa.png&amp;sig=", html);
        Assert.Contains("&amp;w=1280\"", html);
        Assert.Contains("&amp;w=480 480w", html);
        Assert.Contains("&amp;w=1600 1600w", html);
        Assert.Contains("sizes=\"(min-width: 960px) 880px, 100vw\"", html);
        Assert.Contains("alt=\"Mars\"", html);
    }

    [Fact]
    public void Html_without_images_is_unchanged()
    {
        const string html = "<p>No pictures &amp; no problem.</p>";
        Assert.Same(html, ArticleImages.Rewrite(html, Signer));
    }
}

public class ImageResizeTests
{
    private static byte[] Png(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap)) canvas.Clear(SKColors.OrangeRed);
        using var image = SKImage.FromBitmap(bitmap);
        return image.Encode(SKEncodedImageFormat.Png, 100).ToArray();
    }

    private static SKSizeI SizeOf(byte[] bytes)
    {
        using var codec = SKCodec.Create(SKData.CreateCopy(bytes));
        return codec.Info.Size;
    }

    [Fact]
    public void Large_images_are_scaled_to_width_as_webp_keeping_aspect_ratio()
    {
        var result = ImageService.Resize(Png(3000, 2000), 640);

        Assert.NotNull(result);
        Assert.Equal("image/webp", result.ContentType);
        Assert.Equal(new SKSizeI(640, 427), SizeOf(result.Bytes));
        Assert.Equal("RIFF", Encoding.ASCII.GetString(result.Bytes, 0, 4));
    }

    [Fact]
    public void Small_images_are_never_upscaled() =>
        Assert.Equal(new SKSizeI(300, 200), SizeOf(ImageService.Resize(Png(300, 200), 1280)!.Bytes));

    [Fact]
    public void Non_images_return_null() =>
        Assert.Null(ImageService.Resize(Encoding.UTF8.GetBytes("<html>not an image</html>"), 640));

    // A 4×2 image with a marker in its top-left pixel; after orienting, the marker should be
    // where a viewer expects the stored top-left to be shown.
    [Theory]
    [InlineData(SKEncodedOrigin.TopLeft, 4, 2, 0, 0)]
    [InlineData(SKEncodedOrigin.TopRight, 4, 2, 3, 0)]
    [InlineData(SKEncodedOrigin.BottomRight, 4, 2, 3, 1)]
    [InlineData(SKEncodedOrigin.BottomLeft, 4, 2, 0, 1)]
    [InlineData(SKEncodedOrigin.LeftTop, 2, 4, 0, 0)]
    [InlineData(SKEncodedOrigin.RightTop, 2, 4, 1, 0)]
    [InlineData(SKEncodedOrigin.RightBottom, 2, 4, 1, 3)]
    [InlineData(SKEncodedOrigin.LeftBottom, 2, 4, 0, 3)]
    public void Exif_orientation_is_applied(SKEncodedOrigin origin, int width, int height, int markerX, int markerY)
    {
        using var source = new SKBitmap(4, 2, SKColorType.Rgba8888, SKAlphaType.Premul);
        source.Erase(SKColors.Black);
        source.SetPixel(0, 0, SKColors.Red);

        using var oriented = ImageService.Orient(source, origin);

        Assert.Equal((width, height), (oriented.Width, oriented.Height));
        Assert.Equal(SKColors.Red, oriented.GetPixel(markerX, markerY));
    }
}
