using System.Globalization;
using Microsoft.AspNetCore.Http.HttpResults;
using Novashelf.Api.Models;
using Novashelf.Api.Sources;

namespace Novashelf.Api.Endpoints;

public static class ContentEndpoints
{
    private const int MaxPage = 500;

    public static IEndpointRouteBuilder MapContentEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/health", () => TypedResults.Ok(new { status = "ok" }));

        api.MapGet("/topics", () => TypedResults.Ok(Topics.All.Select(t => new { t.Slug, t.Name })));

        api.MapGet("/feed", GetFeed);
        api.MapGet("/content/{id}", GetContent);
        api.MapGet("/apod", GetApod);
        api.MapGet("/images", SearchImages);

        return app;
    }

    private static async Task<Results<Ok<PagedResult<ContentItem>>, ProblemHttpResult>> GetFeed(
        SpaceflightNewsClient articles, CancellationToken ct, string? topic = null, int page = 1)
    {
        if (page is < 1 or > MaxPage) return BadRequest($"page must be between 1 and {MaxPage}");

        IReadOnlyCollection<string>? keywords = null;
        if (!string.IsNullOrWhiteSpace(topic))
        {
            var match = Topics.Find(topic);
            if (match is null) return BadRequest($"Unknown topic '{topic}'");
            keywords = match.Keywords.ToList();
        }

        return TypedResults.Ok(await articles.GetArticlesAsync(keywords, page, ct));
    }

    private static async Task<Results<Ok<ContentItem>, NotFound>> GetContent(
        string id, ContentResolver resolver, CancellationToken ct)
    {
        var item = await resolver.GetAsync(id, ct);
        return item is null ? TypedResults.NotFound() : TypedResults.Ok(item);
    }

    private static async Task<Results<Ok<ContentItem>, NotFound, ProblemHttpResult>> GetApod(
        ApodClient apod, CancellationToken ct, string? date = null)
    {
        if (date is null)
        {
            var latest = await apod.GetLatestAsync(ct);
            return latest is null ? TypedResults.NotFound() : TypedResults.Ok(latest);
        }

        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return BadRequest("date must be in yyyy-MM-dd format");

        var item = await apod.GetAsync(parsed, ct);
        return item is null ? TypedResults.NotFound() : TypedResults.Ok(item);
    }

    private static async Task<Results<Ok<PagedResult<ContentItem>>, ProblemHttpResult>> SearchImages(
        NasaImagesClient images, CancellationToken ct, string? q = null, int page = 1)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length > 100) return BadRequest("q is required (max 100 characters)");
        // The NASA API caps results at 10,000 hits.
        if (page is < 1 or > 10_000 / NasaImagesClient.PageSize) return BadRequest("page is out of range");

        return TypedResults.Ok(await images.SearchAsync(q, page, ct));
    }

    private static ProblemHttpResult BadRequest(string detail) =>
        TypedResults.Problem(detail: detail, statusCode: StatusCodes.Status400BadRequest);
}
