using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

    public sealed record PreferencesDto(string? FontSize, string? Width, string? Theme);

    public static IEndpointRouteBuilder MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        var me = app.MapGroup("/api/me").RequireAuthorization();

        me.MapGet("/history", ListHistory);
        me.MapPost("/history", RecordRead);
        me.MapDelete("/history", ClearHistory);
        me.MapDelete("/history/{id}", RemoveFromHistory);

        me.MapGet("/topics", GetTopics);
        me.MapPut("/topics", SetTopics);

        me.MapGet("/preferences", GetPreferences);
        me.MapPut("/preferences", SetPreferences);

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

    /// <summary>The saved reading settings, or 204 when the reader hasn't saved any yet.</summary>
    private static async Task<Results<Ok<PreferencesDto>, NoContent>> GetPreferences(
        HttpContext context, UserManager<AppUser> users, AppDbContext db, CancellationToken ct)
    {
        var userId = users.GetUserId(context.User)!;
        var saved = await db.UserPreferences.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, ct);
        return saved is null
            ? TypedResults.NoContent()
            : TypedResults.Ok(new PreferencesDto(saved.FontSize, saved.Width, saved.Theme));
    }

    private static async Task<Results<Ok<PreferencesDto>, ValidationProblem>> SetPreferences(
        PreferencesDto request, HttpContext context, UserManager<AppUser> users, AppDbContext db,
        TimeProvider time, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        void Check(string field, string? value, string[] allowed)
        {
            if (value is null || !allowed.Contains(value))
                errors[field] = [$"{field} must be one of: {string.Join(", ", allowed)}."];
        }
        Check("fontSize", request.FontSize, Data.UserPreferences.FontSizes);
        Check("width", request.Width, Data.UserPreferences.Widths);
        Check("theme", request.Theme, Data.UserPreferences.Themes);
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        var userId = users.GetUserId(context.User)!;
        try
        {
            await SavePreferencesAsync(db, userId, request, time, ct);
        }
        catch (DbUpdateException)
        {
            // Another tab created the row first: update it instead.
            db.ChangeTracker.Clear();
            await SavePreferencesAsync(db, userId, request, time, ct);
        }

        return TypedResults.Ok(request);
    }

    private static async Task SavePreferencesAsync(
        AppDbContext db, string userId, PreferencesDto request, TimeProvider time, CancellationToken ct)
    {
        var saved = await db.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (saved is null)
        {
            saved = new UserPreferences { UserId = userId, FontSize = "", Width = "", Theme = "" };
            db.UserPreferences.Add(saved);
        }
        saved.FontSize = request.FontSize!;
        saved.Width = request.Width!;
        saved.Theme = request.Theme!;
        saved.UpdatedAt = time.GetUtcNow();
        await db.SaveChangesAsync(ct);
    }
}
