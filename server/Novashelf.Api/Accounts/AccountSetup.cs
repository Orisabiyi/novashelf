using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Novashelf.Api.Data;

namespace Novashelf.Api.Accounts;

public static class AccountSetup
{
    public const string AuthRateLimitPolicy = "auth";
    public const string SessionCookieName = "novashelf_session";

    /// <summary>Reader accounts with ASP.NET Core Identity, signed in with an HttpOnly cookie.</summary>
    public static IServiceCollection AddNovashelfAccounts(this IServiceCollection services, IHostEnvironment environment)
    {
        services
            .AddIdentityCore<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                // Length over composition rules (current NIST guidance): 8+ characters, any kind.
                options.Password.RequiredLength = AccountValidation.PasswordMinLength;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredUniqueChars = 1;

                // Slow down password guessing against a single account.
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);

                // No email sending yet, so accounts work without confirming the address.
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = SessionCookieName;
            options.Cookie.HttpOnly = true;
            // Lax keeps the cookie off cross-site POSTs (CSRF) while links into the site still work.
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
            options.ExpireTimeSpan = TimeSpan.FromDays(30);
            options.SlidingExpiration = true;

            // This is an API: answer with status codes instead of redirecting to a login page.
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        // Cookie encryption keys live in the database. Kept only in memory or on the container's
        // disk, they'd be lost whenever the app restarts or scales to zero, signing everyone out.
        services.AddDataProtection()
            .SetApplicationName("Novashelf")
            .PersistKeysToDbContext<AppDbContext>();

        services.AddAuthorization();

        services.AddScoped<ReadingHistory>();
        services.AddScoped<IReadMarkers>(sp => sp.GetRequiredService<ReadingHistory>());
        services.AddScoped<FollowedTopics>();

        // Per-address limits on sign-in, sign-up, password changes and deletion, on top of
        // per-account lockout. See ClientAddress for where the addresses come from.
        services.AddSingleton<ClientAddress>();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(AuthRateLimitPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                Addresses(context).Visitor(context), _ => PerMinute(10)));

            // Also per connecting address, across the same endpoints. Looser, because many readers
            // share each of Vercel's addresses; it caps callers who forge X-Forwarded-For to look
            // like many readers.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                IsAuthLimited(context)
                    ? RateLimitPartition.GetFixedWindowLimiter(Addresses(context).Connecting(context), _ => PerMinute(60))
                    : RateLimitPartition.GetNoLimiter(""));
        });

        return services;
    }

    private static ClientAddress Addresses(HttpContext context) =>
        context.RequestServices.GetRequiredService<ClientAddress>();

    private static bool IsAuthLimited(HttpContext context) =>
        context.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName == AuthRateLimitPolicy;

    private static FixedWindowRateLimiterOptions PerMinute(int permits) => new()
    {
        PermitLimit = permits,
        Window = TimeSpan.FromMinutes(1),
        QueueLimit = 0,
    };
}
