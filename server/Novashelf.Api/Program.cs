using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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

var accountsEnabled = !string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("Default"));
if (accountsEnabled) builder.Services.AddNovashelfAccounts(builder.Environment);
else builder.Services.AddSingleton<IReadMarkers, NoReadMarkers>();

builder.Services.AddSingleton<ImageUrlSigner>();
builder.Services.AddHttpClient(ImageService.HttpClientName, c =>
{
    c.Timeout = TimeSpan.FromSeconds(90);
    c.DefaultRequestHeaders.UserAgent.ParseAdd($"Mozilla/5.0 (compatible; {userAgent})");
}).ConfigurePrimaryHttpMessageHandler(OutboundGuard.CreateHandler);
builder.Services.AddSingleton<ImageService>();

builder.Services.AddScoped<ReaderService>();
builder.Services.AddSingleton(Curriculum.LoadEmbedded());
builder.Services.AddScoped<LessonService>();
builder.Services.AddHttpClient<PageExtractor>(c =>
{
    c.Timeout = TimeSpan.FromSeconds(25);
    c.DefaultRequestHeaders.UserAgent.ParseAdd($"Mozilla/5.0 (compatible; {userAgent})");
}).ConfigurePrimaryHttpMessageHandler(OutboundGuard.CreateHandler);

var app = builder.Build();

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
