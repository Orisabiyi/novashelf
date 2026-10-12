using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Novashelf.Api.Models;

namespace Novashelf.Api.Images;

/// <summary>
/// Turns third-party image URLs into signed <c>/api/img</c> URLs. The image endpoint only fetches
/// sources signed here, so it can't be used as an open proxy. The width is appended by the client
/// (<c>&amp;w=640</c>) and snapped to <see cref="Widths"/> by the endpoint.
/// </summary>
public sealed class ImageUrlSigner
{
    public const string Path = "/api/img";

    /// <summary>Widths we render. Requests snap up to the next one so caches stay small.</summary>
    public static readonly int[] Widths = [320, 480, 640, 960, 1280, 1600, 2048];

    private readonly byte[] _key;

    public ImageUrlSigner(IConfiguration config, ILogger<ImageUrlSigner> logger)
    {
        var configured = config["Images:SigningKey"];
        if (string.IsNullOrWhiteSpace(configured))
        {
            // Fine for development: URLs just stop verifying after a restart (pages refetch them).
            logger.LogWarning("Images:SigningKey is not set; using a random key for this process.");
            _key = RandomNumberGenerator.GetBytes(32);
        }
        else
        {
            _key = Encoding.UTF8.GetBytes(configured);
        }
    }

    internal ImageUrlSigner(byte[] key) => _key = key;

    /// <summary>The proxied URL for an http(s) image, or the input unchanged if it isn't one.</summary>
    public string? Proxy(string? source)
    {
        if (!IsHttpUrl(source)) return source;
        return $"{Path}?src={Uri.EscapeDataString(source!)}&sig={Sign(source!)}";
    }

    /// <summary>The item with its image served through the proxy (the client adds a width).</summary>
    public ContentItem Proxied(ContentItem item) => item with { ImageUrl = Proxy(item.ImageUrl) };

    public PagedResult<ContentItem> Proxied(PagedResult<ContentItem> page) =>
        page with { Items = page.Items.Select(Proxied).ToList() };

    public bool Verify(string source, string? signature)
    {
        if (string.IsNullOrEmpty(signature)) return false;
        var expected = Encoding.ASCII.GetBytes(Sign(source));
        var actual = Encoding.ASCII.GetBytes(signature);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    public static int SnapWidth(int? requested)
    {
        if (requested is not > 0) return 1280;
        foreach (var width in Widths)
        {
            if (width >= requested) return width;
        }
        return Widths[^1];
    }

    private string Sign(string source)
    {
        var mac = HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(source));
        return WebEncoders.Base64UrlEncode(mac, 0, 16);
    }

    private static bool IsHttpUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";
}
