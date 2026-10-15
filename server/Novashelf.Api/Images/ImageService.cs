using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using SkiaSharp;

namespace Novashelf.Api.Images;

public sealed record ProcessedImage(byte[] Bytes, string ContentType);

public sealed class ImageService(IHttpClientFactory httpClients, ILogger<ImageService> logger) : IDisposable
{
    public const string HttpClientName = "images";

    private const int MaxSourceBytes = 25 * 1024 * 1024;
    private const int WebpQuality = 78;

    // Its own cache with a size budget: rendered images are kept, originals only briefly so
    // several widths of the same picture don't each download it again.
    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = 160L * 1024 * 1024 });
    private readonly ConcurrentDictionary<string, Lazy<Task<ProcessedImage?>>> _inFlight = new();
    private readonly SemaphoreSlim _warmingSlots = new(4);

    public void Warm(IEnumerable<string?> sources, int width)
    {
        foreach (var source in sources.OfType<string>().Distinct())
        {
            if (_cache.TryGetValue($"{width}|{source}", out _)) continue;
            _ = Task.Run(async () =>
            {
                await _warmingSlots.WaitAsync();
                try
                {
                    await GetAsync(source, width, CancellationToken.None);
                }
                finally
                {
                    _warmingSlots.Release();
                }
            });
        }
    }

    public async Task<ProcessedImage?> GetAsync(string source, int width, CancellationToken ct)
    {
        var key = $"{width}|{source}";
        if (_cache.TryGetValue(key, out ProcessedImage? cached)) return cached;

        // Concurrent requests for the same image share one download and resize.
        var work = _inFlight.GetOrAdd(key, _ => new Lazy<Task<ProcessedImage?>>(() => RenderAsync(source, width)));
        try
        {
            var result = await work.Value.WaitAsync(ct);
            if (result is not null)
            {
                _cache.Set(key, result, new MemoryCacheEntryOptions
                {
                    Size = result.Bytes.Length,
                    SlidingExpiration = TimeSpan.FromDays(1),
                });
            }
            return result;
        }
        finally
        {
            _inFlight.TryRemove(key, out _);
        }
    }

    private async Task<ProcessedImage?> RenderAsync(string source, int width)
    {
        try
        {
            var original = await GetOriginalAsync(source);
            return original is null ? null : Resize(original, width);
        }
        catch (Exception ex)
        {
            logger.LogInformation(ex, "Image: couldn't process {Source}", source);
            return null;
        }
    }

    private async Task<byte[]?> GetOriginalAsync(string source)
    {
        var key = $"original|{source}";
        if (_cache.TryGetValue(key, out byte[]? bytes)) return bytes;

        using var request = new HttpRequestMessage(HttpMethod.Get, source);
        request.Headers.Accept.ParseAdd("image/avif,image/webp,image/*;q=0.8");
        // Not tied to the caller's request: if the reader navigates away, finishing still warms the cache.
        var http = httpClients.CreateClient(HttpClientName);
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        if (!response.IsSuccessStatusCode) return null;

        var mediaType = response.Content.Headers.ContentType?.MediaType ?? "";
        if (!mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) || mediaType.Contains("svg"))
            return null;
        if (response.Content.Headers.ContentLength > MaxSourceBytes) return null;

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(chunk)) > 0)
        {
            if (buffer.Length + read > MaxSourceBytes) return null;
            buffer.Write(chunk, 0, read);
        }

        bytes = buffer.ToArray();
        _cache.Set(key, bytes, new MemoryCacheEntryOptions
        {
            Size = bytes.Length,
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
        });
        return bytes;
    }

    internal static ProcessedImage? Resize(byte[] original, int width)
    {
        using var data = SKData.CreateCopy(original);
        using var codec = SKCodec.Create(data);
        if (codec is null) return null;

        var info = codec.Info;
        var (sourceWidth, sourceHeight) = SwapsAxes(codec.EncodedOrigin)
            ? (info.Height, info.Width)
            : (info.Width, info.Height);
        var targetWidth = Math.Min(width, sourceWidth);
        var targetHeight = Math.Max(1, (int)Math.Round(sourceHeight * (targetWidth / (double)sourceWidth)));

        // Let the decoder do most of the downscaling (JPEG can decode at 1/2, 1/4, 1/8 cheaply).
        var scale = Math.Min(1f, (float)targetWidth / sourceWidth);
        var decodeSize = codec.GetScaledDimensions(scale);
        var decodeInfo = new SKImageInfo(decodeSize.Width, decodeSize.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var decoded = new SKBitmap(decodeInfo);
        var result = codec.GetPixels(decodeInfo, decoded.GetPixels());
        if (result is not (SKCodecResult.Success or SKCodecResult.IncompleteInput)) return null;

        using var oriented = Orient(decoded, codec.EncodedOrigin);
        using var resized = oriented.Width == targetWidth
            ? oriented.Copy()
            : oriented.Resize(new SKImageInfo(targetWidth, targetHeight, SKColorType.Rgba8888, SKAlphaType.Premul),
                new SKSamplingOptions(SKCubicResampler.Mitchell));
        if (resized is null) return null;

        using var image = SKImage.FromBitmap(resized);
        using var encoded = image.Encode(SKEncodedImageFormat.Webp, WebpQuality);
        return encoded is null ? null : new ProcessedImage(encoded.ToArray(), "image/webp");
    }

    private static bool SwapsAxes(SKEncodedOrigin origin) =>
        origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;

    internal static SKBitmap Orient(SKBitmap bitmap, SKEncodedOrigin origin)
    {
        if (origin is SKEncodedOrigin.TopLeft or SKEncodedOrigin.Default) return bitmap.Copy();

        float w = bitmap.Width, h = bitmap.Height;
        // x' = ScaleX·x + SkewX·y + TransX, y' = SkewY·x + ScaleY·y + TransY
        var matrix = origin switch
        {
            SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, w, 0, 1, 0, 0, 0, 1),     // mirror horizontally
            SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, w, 0, -1, h, 0, 0, 1), // rotate 180°
            SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, h, 0, 0, 1),   // mirror vertically
            SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),       // transpose
            SKEncodedOrigin.RightTop => new SKMatrix(0, -1, h, 1, 0, 0, 0, 0, 1),     // rotate 90° clockwise
            SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, h, -1, 0, w, 0, 0, 1), // transverse
            SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, w, 0, 0, 1),   // rotate 270° clockwise
            _ => SKMatrix.Identity,
        };

        var swap = SwapsAxes(origin);
        var output = new SKBitmap(swap ? bitmap.Height : bitmap.Width, swap ? bitmap.Width : bitmap.Height,
            bitmap.ColorType, bitmap.AlphaType);
        using var canvas = new SKCanvas(output);
        using var image = SKImage.FromBitmap(bitmap);
        canvas.SetMatrix(matrix);
        canvas.DrawImage(image, 0, 0, new SKSamplingOptions(SKFilterMode.Nearest));
        return output;
    }

    public void Dispose()
    {
        _cache.Dispose();
        _warmingSlots.Dispose();
    }
}
