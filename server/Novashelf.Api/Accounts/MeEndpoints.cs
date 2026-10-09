using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Novashelf.Api.Data;
using Novashelf.Api.Images;
using Novashelf.Api.Learn;
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

    public sealed record CompletedLesson(string Track, string Lesson, DateTimeOffset CompletedAt);

    public static IEndpointRouteBuilder MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        var me = app.MapGroup("/api/me").RequireAuthorization();

        // The account itself (GET /api/me is with sign-in, in AuthEndpoints).
        app.MapPatch("/api/me", UpdateAccount).RequireAuthorization();
        app.MapDelete("/api/me", DeleteAccount).RequireAuthorization().RequireRateLimiting(AccountSetup.AuthRateLimitPolicy);
        me.MapPost("/password", ChangePassword).RequireRateLimiting(AccountSetup.AuthRateLimitPolicy);

        me.MapGet("/history", ListHistory);
        me.MapPost("/history", RecordRead);
        me.MapDelete("/history", ClearHistory);
        me.MapDelete("/history/{id}", RemoveFromHistory);

        me.MapGet("/topics", GetTopics);
        me.MapPut("/topics", SetTopics);

        me.MapGet("/preferences", GetPreferences);
        me.MapPut("/preferences", SetPreferences);

        me.MapGet("/lessons", ListCompletedLessons);
        me.MapPut("/lessons/{track}/{lesson}", CompleteLesson);
        me.MapDelete("/lessons/{track}/{lesson}", UncompleteLesson);

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

    private static async Task<Ok<List<CompletedLesson>>> ListCompletedLessons(
        HttpContext context, UserManager<AppUser> users, AppDbContext db, CancellationToken ct)
    {
        var userId = users.GetUserId(context.User)!;
        var completed = await db.LessonProgress
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.CompletedAt)
            .Select(p => new CompletedLesson(p.Track, p.Lesson, p.CompletedAt))
            .ToListAsync(ct);
        return TypedResults.Ok(completed);
    }

    /// <summary>Marks a lesson finished. Idempotent: finishing it again keeps the first date.</summary>
    private static async Task<Results<NoContent, NotFound>> CompleteLesson(
        string track, string lesson, HttpContext context, UserManager<AppUser> users, AppDbContext db,
        Curriculum curriculum, TimeProvider time, CancellationToken ct)
    {
        if (curriculum.Find(track, lesson) is not { } found) return TypedResults.NotFound();

        var userId = users.GetUserId(context.User)!;
        var (trackSlug, lessonSlug) = (found.Track.Slug, found.Lesson.Slug);
        if (await db.LessonProgress.AnyAsync(p => p.UserId == userId && p.Track == trackSlug && p.Lesson == lessonSlug, ct))
            return TypedResults.NoContent();

        db.LessonProgress.Add(new LessonProgress { UserId = userId, Track = trackSlug, Lesson = lessonSlug, CompletedAt = time.GetUtcNow() });
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Finished in another tab at the same moment; it's recorded either way.
        }
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> UncompleteLesson(
        string track, string lesson, HttpContext context, UserManager<AppUser> users, AppDbContext db, CancellationToken ct)
    {
        var userId = users.GetUserId(context.User)!;
        await db.LessonProgress
            .Where(p => p.UserId == userId && p.Track == track && p.Lesson == lesson)
            .ExecuteDeleteAsync(ct);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<AccountResponse>, ValidationProblem, UnauthorizedHttpResult>> UpdateAccount(
        UpdateAccountRequest request, HttpContext context, UserManager<AppUser> users)
    {
        var firstName = AccountValidation.NormalizeFirstName(request.FirstName);
        if ((firstName?.Length ?? 0) > AppUser.FirstNameMaxLength)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["firstName"] = [$"Keep it under {AppUser.FirstNameMaxLength} characters."],
            });

        var user = await users.GetUserAsync(context.User);
        if (user is null) return TypedResults.Unauthorized();

        user.FirstName = firstName;
        await users.UpdateAsync(user);
        return TypedResults.Ok(AccountResponse.From(user));
    }

    private static async Task<Results<NoContent, ValidationProblem, ProblemHttpResult, UnauthorizedHttpResult>> ChangePassword(
        ChangePasswordRequest request, HttpContext context, UserManager<AppUser> users, SignInManager<AppUser> signIn)
    {
        var user = await users.GetUserAsync(context.User);
        if (user is null) return TypedResults.Unauthorized();

        var newPassword = request.NewPassword ?? "";
        if (newPassword.Length is < AccountValidation.PasswordMinLength or > AccountValidation.PasswordMaxLength)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["newPassword"] = [$"Use {AccountValidation.PasswordMinLength} to {AccountValidation.PasswordMaxLength} characters."],
            });

        switch (await CheckPasswordAsync(signIn, user, request.CurrentPassword))
        {
            case PasswordCheck.LockedOut: return TooManyAttempts();
            case PasswordCheck.Wrong: return WrongPassword("currentPassword");
        }

        var result = await users.ChangePasswordAsync(user, request.CurrentPassword!, newPassword);
        if (!result.Succeeded)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["newPassword"] = result.Errors.Select(e => e.Description).ToArray(),
            });

        // The security stamp changed: re-issue this browser's cookie so it stays signed in.
        // Sessions elsewhere are signed out when their stamp is next checked.
        await signIn.RefreshSignInAsync(user);
        return TypedResults.NoContent();
    }

    /// <summary>Deletes the account and everything stored for it (history, topics, settings, progress).</summary>
    private static async Task<Results<NoContent, ValidationProblem, ProblemHttpResult, UnauthorizedHttpResult>> DeleteAccount(
        [FromBody] DeleteAccountRequest request, HttpContext context, UserManager<AppUser> users,
        SignInManager<AppUser> signIn)
    {
        var user = await users.GetUserAsync(context.User);
        if (user is null) return TypedResults.Unauthorized();

        switch (await CheckPasswordAsync(signIn, user, request.Password))
        {
            case PasswordCheck.LockedOut: return TooManyAttempts();
            case PasswordCheck.Wrong: return WrongPassword("password");
        }

        var result = await users.DeleteAsync(user);
        if (!result.Succeeded)
            return TypedResults.Problem(detail: "We couldn't delete your account. Please try again.", statusCode: 500);

        await signIn.SignOutAsync();
        return TypedResults.NoContent();
    }

    private enum PasswordCheck { Right, Wrong, LockedOut }

    /// <summary>
    /// Confirms the reader's password before a sensitive change. Wrong guesses count towards the
    /// same lockout as sign-in.
    /// </summary>
    private static async Task<PasswordCheck> CheckPasswordAsync(SignInManager<AppUser> signIn, AppUser user, string? password)
    {
        var check = await signIn.CheckPasswordSignInAsync(user, password ?? "", lockoutOnFailure: true);
        return check.IsLockedOut ? PasswordCheck.LockedOut : check.Succeeded ? PasswordCheck.Right : PasswordCheck.Wrong;
    }

    private static ProblemHttpResult TooManyAttempts() => TypedResults.Problem(
        detail: "Too many attempts. Wait a few minutes and try again.",
        statusCode: StatusCodes.Status429TooManyRequests);

    private static ValidationProblem WrongPassword(string field) => TypedResults.ValidationProblem(
        new Dictionary<string, string[]> { [field] = ["That password isn't right."] });
}
