using Novashelf.Api.Images;

namespace Novashelf.Api.Endpoints;

public static class ImageEndpoints
{
    public static IEndpointRouteBuilder MapImageEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(ImageUrlSigner.Path, GetImage);
        return app;
    }

    private static async Task<IResult> GetImage(
        HttpContext context, ImageUrlSigner signer, ImageService images, CancellationToken ct,
        string? src = null, string? sig = null, int? w = null)
    {
        if (string.IsNullOrEmpty(src) || !signer.Verify(src, sig))
            return TypedResults.Problem(detail: "Unknown or unsigned image.", statusCode: StatusCodes.Status403Forbidden);

        var image = await images.GetAsync(src, ImageUrlSigner.SnapWidth(w), ct);
        if (image is null)
        {
            context.Response.Headers.CacheControl = "public, max-age=300";
            return TypedResults.Redirect(src);
        }

        context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        return TypedResults.File(image.Bytes, image.ContentType);
    }
}
