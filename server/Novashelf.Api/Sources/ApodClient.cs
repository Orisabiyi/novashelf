using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Novashelf.Api.Models;

namespace Novashelf.Api.Sources;

/// <summary>Client for NASA's Astronomy Picture of the Day (https://api.nasa.gov).</summary>
public sealed class ApodClient(HttpClient http, IMemoryCache cache, IConfiguration config, TimeProvider time)
{
    /// <summary>APOD started on 16 June 1995; the API rejects earlier dates.</summary>
    public static readonly DateOnly FirstDate = new(1995, 6, 16);

    private static readonly TimeSpan Ttl = TimeSpan.FromHours(1);

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private string ApiKey => config["Nasa:ApiKey"] ?? "DEMO_KEY";

    /// <summary>
    /// Today's picture. APOD publishes on US Eastern time and the API can error for "today"
    /// before the new picture is posted, so this falls back to the previous day.
    /// </summary>
    public async Task<ContentItem?> GetLatestAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        for (var date = today; date >= today.AddDays(-2); date = date.AddDays(-1))
        {
            try
            {
                var item = await GetAsync(date, ct);
                if (item is not null) return item;
            }
            catch (HttpRequestException) when (date > today.AddDays(-2))
            {
                // Not published yet (or a transient upstream error); try the day before.
            }
        }

        return null;
    }

    public async Task<ContentItem?> GetAsync(DateOnly date, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        if (date < FirstDate || date > today) return null;

        var dateText = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return await cache.GetOrCreateAsync($"apod:{dateText}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = Ttl;

            using var response = await http.GetAsync($"planetary/apod?api_key={ApiKey}&date={dateText}&thumbs=true", ct);
            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest) return null;
            response.EnsureSuccessStatusCode();

            var apod = await response.Content.ReadFromJsonAsync<ApodResponse>(Json, ct);
            if (apod is null) return null;

            // Since APOD moved to science.nasa.gov the API can return a placeholder (NASA logo,
            // title "NASA Science") for every date. Surface that as an outage, not as content.
            if (IsPlaceholder(apod)) throw new HttpRequestException("APOD API returned placeholder data");

            return Map(apod);
        });
    }

    internal static bool IsPlaceholder(ApodResponse a) =>
        a.Title.Trim() == "NASA Science"
        || (a.Url?.Contains("/nasa-logo", StringComparison.OrdinalIgnoreCase) ?? false);

    internal static ContentItem Map(ApodResponse a)
    {
        var date = DateOnly.ParseExact(a.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var image = a.MediaType == "image" ? a.Url : a.ThumbnailUrl;

        return new ContentItem(
            Id: ContentId.ForApod(date),
            Kind: ContentKind.Apod,
            Title: a.Title.Trim(),
            Summary: a.Explanation.Trim(),
            ImageUrl: image,
            Url: $"https://apod.nasa.gov/apod/ap{date:yyMMdd}.html",
            Publisher: string.IsNullOrWhiteSpace(a.Copyright) ? "NASA APOD" : a.Copyright.Trim(),
            PublishedAt: new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
    }

    internal sealed record ApodResponse(
        string Date,
        string Title,
        string Explanation,
        string MediaType,
        string? Url,
        string? ThumbnailUrl,
        string? Copyright);
}
