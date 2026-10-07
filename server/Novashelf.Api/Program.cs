using System.Text.Json;
using System.Text.Json.Serialization;
using Novashelf.Api.Endpoints;
using Novashelf.Api.Infrastructure;
using Novashelf.Api.Sources;

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
    c.BaseAddress = new Uri("https://api.nasa.gov/");
    c.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
}).AddStandardResilienceHandler();

builder.Services.AddHttpClient<NasaImagesClient>(c =>
{
    c.BaseAddress = new Uri("https://images-api.nasa.gov/");
    c.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
}).AddStandardResilienceHandler();

builder.Services.AddScoped<ContentResolver>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapContentEndpoints();

app.Run();
