using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Novashelf.Api.Data;

namespace Novashelf.Api.Accounts;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app, bool accountsEnabled)
    {
        if (!accountsEnabled)
        {
            // Without a database there's nowhere to keep accounts; say so clearly.
            var unavailable = () => TypedResults.Problem(
                detail: "Accounts aren't available because no database is configured.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
            app.MapMethods("/api/auth/{**rest}", ["GET", "POST"], unavailable);
            app.MapGet("/api/me", unavailable);
            return app;
        }

        var auth = app.MapGroup("/api/auth").RequireRateLimiting(AccountSetup.AuthRateLimitPolicy);
        auth.MapPost("/register", Register);
        auth.MapPost("/login", Login);
        auth.MapPost("/logout", Logout);

        app.MapGet("/api/me", Me).RequireAuthorization();
        return app;
    }

    private static async Task<Results<Ok<AccountResponse>, ValidationProblem, ProblemHttpResult>> Register(
        RegisterRequest request, UserManager<AppUser> users, SignInManager<AppUser> signIn, TimeProvider time)
    {
        var errors = AccountValidation.Validate(request);
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        var email = request.Email!.Trim();
        var now = time.GetUtcNow();
        var user = new AppUser
        {
            UserName = email,
            Email = email,
            FirstName = AccountValidation.NormalizeFirstName(request.FirstName),
            CreatedAt = now,
            AgeConfirmedAt = now,
        };

        var result = await users.CreateAsync(user, request.Password!);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName"))
            {
                return TypedResults.Problem(
                    detail: "An account with this email already exists. Try signing in instead.",
                    statusCode: StatusCodes.Status409Conflict);
            }

            return TypedResults.ValidationProblem(result.Errors
                .GroupBy(e => e.Code.StartsWith("Password", StringComparison.Ordinal) ? "password"
                    : e.Code.Contains("Email", StringComparison.Ordinal) || e.Code.Contains("UserName", StringComparison.Ordinal) ? "email"
                    : "")
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray()));
        }

        await signIn.SignInAsync(user, isPersistent: true);
        return TypedResults.Ok(AccountResponse.From(user));
    }

    private static async Task<Results<Ok<AccountResponse>, ProblemHttpResult>> Login(
        LoginRequest request, UserManager<AppUser> users, SignInManager<AppUser> signIn)
    {
        var email = request.Email?.Trim() ?? "";
        var password = request.Password ?? "";
        var user = email.Length is > 0 and <= AccountValidation.EmailMaxLength
            ? await users.FindByEmailAsync(email)
            : null;

        if (user is null)
        {
            // Spend about as long as a real check, so response time doesn't reveal which emails exist.
            users.PasswordHasher.HashPassword(new AppUser(), password);
            return InvalidCredentials();
        }

        var result = await signIn.PasswordSignInAsync(user, password, request.Remember, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            return TypedResults.Problem(
                detail: "Too many attempts. Wait a few minutes and try again.",
                statusCode: StatusCodes.Status429TooManyRequests);
        }

        return result.Succeeded ? TypedResults.Ok(AccountResponse.From(user)) : InvalidCredentials();
    }

    private static async Task<NoContent> Logout(SignInManager<AppUser> signIn)
    {
        await signIn.SignOutAsync();
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<AccountResponse>, UnauthorizedHttpResult>> Me(
        HttpContext context, UserManager<AppUser> users)
    {
        var user = await users.GetUserAsync(context.User);
        // A valid cookie for an account that no longer exists counts as signed out.
        return user is null ? TypedResults.Unauthorized() : TypedResults.Ok(AccountResponse.From(user));
    }

    private static ProblemHttpResult InvalidCredentials() => TypedResults.Problem(
        detail: "That email and password don't match. Check them and try again.",
        statusCode: StatusCodes.Status401Unauthorized);
}
