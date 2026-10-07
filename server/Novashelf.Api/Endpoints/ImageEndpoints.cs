using Novashelf.Api.Images;

namespace Novashelf.Api.Endpoints;

public static class ImageEndpoints
{
    public static IEndpointRouteBuilder MapImageEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(ImageUrlSigner.Path, GetImage);
        return app;
    }

    /// <summary>GET /api/img?src=…&amp;sig=…&amp;w=640: a resized WebP copy of a signed source image.</summary>
    private static async Task<IResult> GetImage(
        HttpContext context, ImageUrlSigner signer, ImageService images, CancellationToken ct,
        string? src = null, string? sig = null, int? w = null)
    {
        if (string.IsNullOrEmpty(src) || !signer.Verify(src, sig))
            return TypedResults.Problem(detail: "Unknown or unsigned image.", statusCode: StatusCodes.Status403Forbidden);

        var image = await images.GetAsync(src, ImageUrlSigner.SnapWidth(w), ct);
        if (image is null)
        {
            // Couldn't fetch or decode it: let the browser try the original (it's a signed, known URL).
            context.Response.Headers.CacheControl = "public, max-age=300";
            return TypedResults.Redirect(src);
        }

        // The URL pins source and width, so the response never changes: cache it everywhere.
        context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        return TypedResults.File(image.Bytes, image.ContentType);
    }
}
