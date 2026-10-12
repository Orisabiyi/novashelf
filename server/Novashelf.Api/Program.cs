using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpOverrides;
using Novashelf.Api.Accounts;
using Novashelf.Api.Data;
using Novashelf.Api.Endpoints;
using Novashelf.Api.Images;
using Novashelf.Api.Infrastructure;
using Novashelf.Api.Learn;
using Novashelf.Api.Reading;
using Novashelf.Api.Sources;

// Article pages may use legacy encodings such as windows-1252.
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<UpstreamExceptionHandler>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton(TimeProvider.System);

const string userAgent = "Novashelf/1.0 (+https://github.com/Orisabiyi/novashelf)";

builder.Services.AddHttpClient<SpaceflightNewsClient>(c =>
{
    c.BaseAddress = new Uri("https://api.spaceflightnewsapi.net/");
    c.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
}).AddStandardResilienceHandler();

builder.Services.AddHttpClient<ApodClient>(c =>
{
    c.BaseAddress = new Uri("https://science.nasa.gov/wp-json/wp/v2/");
    c.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
}).AddStandardResilienceHandler();

builder.Services.AddHttpClient<NasaImagesClient>(c =>
{
    c.BaseAddress = new Uri("https://images-api.nasa.gov/");
    c.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
}).AddStandardResilienceHandler();

builder.Services.AddScoped<ContentResolver>();
builder.Services.AddNovashelfDatabase(builder.Configuration);

// Accounts need somewhere to live; without a database the rest of the API still works.
var accountsEnabled = !string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("Default"));
if (accountsEnabled) builder.Services.AddNovashelfAccounts(builder.Environment);
else builder.Services.AddSingleton<IReadMarkers, NoReadMarkers>();

builder.Services.AddSingleton<ImageUrlSigner>();
// Image originals can be large and slow (NASA's take minutes on slow links), hence the long timeout.
builder.Services.AddHttpClient(ImageService.HttpClientName, c =>
{
    c.Timeout = TimeSpan.FromSeconds(90);
    c.DefaultRequestHeaders.UserAgent.ParseAdd($"Mozilla/5.0 (compatible; {userAgent})");
}).ConfigurePrimaryHttpMessageHandler(OutboundGuard.CreateHandler);
// A singleton so the image cache and in-flight de-duplication are shared across requests.
builder.Services.AddSingleton<ImageService>();

// Fetches original article pages. No retries (a slow site shouldn't hold the reader up), a
// timeout, and a handler that refuses to connect to private or internal addresses.
builder.Services.AddScoped<ReaderService>();
// Loaded once at startup; an invalid curriculum.json stops the app with a clear message.
builder.Services.AddSingleton(Curriculum.LoadEmbedded());
builder.Services.AddScoped<LessonService>();
builder.Services.AddHttpClient<PageExtractor>(c =>
{
    // Generous: some publishers (science.nasa.gov) take 10–20s, and each article is fetched once.
    c.Timeout = TimeSpan.FromSeconds(25);
    c.DefaultRequestHeaders.UserAgent.ParseAdd($"Mozilla/5.0 (compatible; {userAgent})");
}).ConfigurePrimaryHttpMessageHandler(OutboundGuard.CreateHandler);

// In production requests arrive through Vercel and then the host's load balancer, each adding itself to
// X-Forwarded-For. Without this every visitor would share the proxy's address, and the per-IP
// sign-in limit would lock everyone out together. Proxy:Hops is the number of proxies to trust.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = builder.Configuration.GetValue("Proxy:Hops", 2);
    // The proxies' addresses aren't fixed, so trust the hop count instead of a list.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
var behindProxy = !builder.Environment.IsDevelopment();

var app = builder.Build();

if (behindProxy) app.UseForwardedHeaders();

await app.MigrateDatabaseAsync();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (accountsEnabled)
{
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
}

app.MapContentEndpoints();
app.MapImageEndpoints();
app.MapLearnEndpoints();
app.MapAuthEndpoints(accountsEnabled);
if (accountsEnabled) app.MapMeEndpoints();

app.Run();
