using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Novashelf.Api.Data;
using Novashelf.Api.Images;
using Novashelf.Api.Models;
using Novashelf.Api.Sources;

namespace Novashelf.Api.Accounts;

/// <summary>The signed-in reader's own data under /api/me.</summary>
public static class MeEndpoints
{
    public sealed record RecordReadRequest(string? Id);

    public sealed record TopicsRequest(List<string>? Topics);

    public sealed record FollowedTopicsResponse(IEnumerable<string> Topics);

    public static IEndpointRouteBuilder MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        var me = app.MapGroup("/api/me").RequireAuthorization();

        me.MapGet("/history", ListHistory);
        me.MapPost("/history", RecordRead);
        me.MapDelete("/history", ClearHistory);
        me.MapDelete("/history/{id}", RemoveFromHistory);

        me.MapGet("/topics", GetTopics);
        me.MapPut("/topics", SetTopics);

        return app;
    }

    private static async Task<Results<Ok<PagedResult<HistoryEntry>>, ProblemHttpResult>> ListHistory(
        HttpContext context, UserManager<AppUser> users, ReadingHistory history, ImageUrlSigner images,
        CancellationToken ct, int page = 1)
    {
        if (page is < 1 or > 1000) return TypedResults.Problem(detail: "page is out of range", statusCode: 400);

        var result = await history.ListAsync(users.GetUserId(context.User)!, page, ct);
        return TypedResults.Ok(result with
        {
            Items = result.Items.Select(e => e with { Item = images.Proxied(e.Item) }).ToList(),
        });
    }

    /// <summary>
    /// Called by the reader page once an item is actually open (prefetching on hover doesn't count).
    /// The item is looked up here rather than trusted from the request.
    /// </summary>
    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> RecordRead(
        RecordReadRequest request, HttpContext context, UserManager<AppUser> users, ContentResolver resolver,
        ReadingHistory history, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Id) || request.Id.Length > 160)
            return TypedResults.Problem(detail: "id is required", statusCode: 400);

        var item = await resolver.GetAsync(request.Id, ct);
        if (item is null) return TypedResults.NotFound();

        await history.RecordAsync(users.GetUserId(context.User)!, item, ct);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> ClearHistory(
        HttpContext context, UserManager<AppUser> users, ReadingHistory history, CancellationToken ct)
    {
        await history.ClearAsync(users.GetUserId(context.User)!, ct);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> RemoveFromHistory(
        string id, HttpContext context, UserManager<AppUser> users, ReadingHistory history, CancellationToken ct)
    {
        await history.RemoveAsync(users.GetUserId(context.User)!, id, ct);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<FollowedTopicsResponse>> GetTopics(
        HttpContext context, UserManager<AppUser> users, FollowedTopics followed, CancellationToken ct)
    {
        var topics = await followed.GetAsync(users.GetUserId(context.User)!, ct);
        return TypedResults.Ok(new FollowedTopicsResponse(topics.Select(t => t.Slug)));
    }

    /// <summary>Replaces the whole list, so following or unfollowing is one idempotent request.</summary>
    private static async Task<Results<Ok<FollowedTopicsResponse>, ProblemHttpResult>> SetTopics(
        TopicsRequest request, HttpContext context, UserManager<AppUser> users, FollowedTopics followed,
        CancellationToken ct)
    {
        var slugs = request.Topics ?? [];
        if (slugs.Count > Topics.All.Count) return TypedResults.Problem(detail: "Too many topics", statusCode: 400);

        var topics = new List<Topic>();
        foreach (var slug in slugs.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (Topics.Find(slug ?? "") is not { } topic)
                return TypedResults.Problem(detail: $"Unknown topic '{slug}'", statusCode: 400);
            topics.Add(topic);
        }

        var userId = users.GetUserId(context.User)!;
        await followed.SetAsync(userId, topics, ct);
        var saved = await followed.GetAsync(userId, ct);
        return TypedResults.Ok(new FollowedTopicsResponse(saved.Select(t => t.Slug)));
    }
}
