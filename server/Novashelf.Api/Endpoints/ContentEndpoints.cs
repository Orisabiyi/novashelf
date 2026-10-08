using System.Globalization;
using Microsoft.AspNetCore.Http.HttpResults;
using Novashelf.Api.Images;
using Novashelf.Api.Models;
using Novashelf.Api.Reading;
using Novashelf.Api.Sources;

namespace Novashelf.Api.Endpoints;

public static class ContentEndpoints
{
    private const int MaxPage = 500;

    // Image widths the front end asks for first; warming these makes first paint fast.
    private const int CardImageWidth = 640;
    private const int FeaturedImageWidth = 960;
    private const int ReaderHeroWidth = 1280;
    private const int ArticlesWarmedPerPage = 6;

    public static IEndpointRouteBuilder MapContentEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/health", () => TypedResults.Ok(new { status = "ok" }));

        api.MapGet("/topics", () => TypedResults.Ok(Topics.All.Select(t => new { t.Slug, t.Name })));

        api.MapGet("/feed", GetFeed);
        api.MapGet("/content/{id}", GetContent);
        api.MapGet("/content/{id}/read", ReadContent);
        api.MapGet("/apod", GetApod);
        api.MapGet("/apod/archive", GetApodArchive);
        api.MapGet("/images", SearchImages);

        return app;
    }

    private static async Task<Results<Ok<PagedResult<ContentItem>>, ProblemHttpResult>> GetFeed(
        SpaceflightNewsClient articles, ImageUrlSigner images, ImageService imageCache, ReaderService reader,
        CancellationToken ct, string? topic = null, int page = 1)
    {
        if (page is < 1 or > MaxPage) return BadRequest($"page must be between 1 and {MaxPage}");

        IReadOnlyCollection<string>? keywords = null;
        if (!string.IsNullOrWhiteSpace(topic))
        {
            var match = Topics.Find(topic);
            if (match is null) return BadRequest($"Unknown topic '{topic}'");
            keywords = match.Keywords.ToList();
        }

        var feed = await articles.GetArticlesAsync(keywords, page, ct);
        imageCache.Warm(feed.Items.Select(i => i.ImageUrl), CardImageWidth);
        // The first cards on a page are the likeliest to be opened: have them ready.
        reader.Warm(feed.Items.Take(ArticlesWarmedPerPage));
        return TypedResults.Ok(images.Proxied(feed));
    }

    private static async Task<Results<Ok<ContentItem>, NotFound>> GetContent(
        string id, ContentResolver resolver, ImageUrlSigner images, CancellationToken ct)
    {
        var item = await resolver.GetAsync(id, ct);
        return item is null ? TypedResults.NotFound() : TypedResults.Ok(images.Proxied(item));
    }

    private static async Task<Results<Ok<ReadableContent>, NotFound>> ReadContent(
        string id, ReaderService reader, ImageUrlSigner images, ImageService imageCache, CancellationToken ct)
    {
        var readable = await reader.GetAsync(id, ct);
        if (readable is not null) imageCache.Warm([readable.Item.ImageUrl], ReaderHeroWidth);
        // Article images are already proxied by the reader; this covers the hero image.
        return readable is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(readable with { Item = images.Proxied(readable.Item) });
    }

    private static async Task<Results<Ok<ContentItem>, NotFound, ProblemHttpResult>> GetApod(
        ApodClient apod, ImageUrlSigner images, ImageService imageCache, CancellationToken ct, string? date = null)
    {
        if (date is null)
        {
            var latest = await apod.GetLatestAsync(ct);
            if (latest is not null) imageCache.Warm([latest.ImageUrl], FeaturedImageWidth);
            return latest is null ? TypedResults.NotFound() : TypedResults.Ok(images.Proxied(latest));
        }

        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return BadRequest("date must be in yyyy-MM-dd format");

        var item = await apod.GetAsync(parsed, ct);
        return item is null ? TypedResults.NotFound() : TypedResults.Ok(images.Proxied(item));
    }

    private static async Task<Results<Ok<PagedResult<ContentItem>>, ProblemHttpResult>> GetApodArchive(
        ApodClient apod, ImageUrlSigner images, ImageService imageCache, CancellationToken ct, int page = 1)
    {
        if (page is < 1 or > MaxPage) return BadRequest($"page must be between 1 and {MaxPage}");

        var archive = await apod.GetArchivePageAsync(page, ct);
        imageCache.Warm(archive.Items.Select(i => i.ImageUrl), CardImageWidth);
        return TypedResults.Ok(images.Proxied(archive));
    }

    private static async Task<Results<Ok<PagedResult<ContentItem>>, ProblemHttpResult>> SearchImages(
        NasaImagesClient library, ImageUrlSigner images, ImageService imageCache, CancellationToken ct,
        string? q = null, int page = 1)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length > 100) return BadRequest("q is required (max 100 characters)");
        // The NASA API caps results at 10,000 hits.
        if (page is < 1 or > 10_000 / NasaImagesClient.PageSize) return BadRequest("page is out of range");

        var results = await library.SearchAsync(q, page, ct);
        imageCache.Warm(results.Items.Select(i => i.ImageUrl), CardImageWidth);
        return TypedResults.Ok(images.Proxied(results));
    }

    private static ProblemHttpResult BadRequest(string detail) =>
        TypedResults.Problem(detail: detail, statusCode: StatusCodes.Status400BadRequest);
}
