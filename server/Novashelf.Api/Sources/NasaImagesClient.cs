using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Novashelf.Api.Models;

namespace Novashelf.Api.Sources;

public sealed class NasaImagesClient(HttpClient http, IMemoryCache cache)
{
    public const int PageSize = 24;
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(30);

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public async Task<PagedResult<ContentItem>> SearchAsync(string query, int page, CancellationToken ct)
    {
        var q = query.Trim();
        return (await cache.GetOrCreateAsync($"img:search:{q.ToLowerInvariant()}:{page}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = Ttl;

            var url = $"search?media_type=image&page_size={PageSize}&page={page}&q={Uri.EscapeDataString(q)}";
            var response = await http.GetFromJsonAsync<SearchResponse>(url, Json, ct)
                ?? throw new HttpRequestException("Empty response from NASA Images API");

            var items = response.Collection.Items.Select(Map).OfType<ContentItem>().ToList();
            var hasMore = response.Collection.Links?.Any(l => l.Rel == "next") ?? false;
            return new PagedResult<ContentItem>(items, page, hasMore);
        }))!;
    }

    public async Task<ContentItem?> GetAsync(string nasaId, CancellationToken ct)
    {
        return await cache.GetOrCreateAsync($"img:item:{nasaId}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = Ttl;

            var url = $"search?nasa_id={Uri.EscapeDataString(nasaId)}";
            var response = await http.GetFromJsonAsync<SearchResponse>(url, Json, ct);
            return response?.Collection.Items.Select(Map).FirstOrDefault(i => i?.Id == ContentId.ForImage(nasaId));
        });
    }

    internal static ContentItem? Map(Item item)
    {
        var data = item.Data?.FirstOrDefault();
        if (data is null || string.IsNullOrWhiteSpace(data.NasaId)) return null;

        var image = item.Links?.FirstOrDefault(l => l.Href.Contains("~medium.", StringComparison.Ordinal))?.Href
            ?? item.Links?.FirstOrDefault(l => l.Rel == "preview")?.Href;

        return new ContentItem(
            Id: ContentId.ForImage(data.NasaId),
            Kind: ContentKind.Image,
            Title: data.Title?.Trim() ?? data.NasaId,
            Summary: data.Description?.Trim() ?? "",
            ImageUrl: image,
            Url: $"https://images.nasa.gov/details/{Uri.EscapeDataString(data.NasaId)}",
            Publisher: string.IsNullOrWhiteSpace(data.Photographer) ? $"NASA {data.Center}".Trim() : data.Photographer.Trim(),
            PublishedAt: data.DateCreated);
    }

    internal sealed record SearchResponse(Collection Collection);

    internal sealed record Collection(List<Item> Items, List<Link>? Links);

    internal sealed record Item(List<ItemData>? Data, List<Link>? Links);

    internal sealed record ItemData(
        string NasaId,
        string? Title,
        string? Description,
        string? Center,
        string? Photographer,
        DateTimeOffset? DateCreated);

    internal sealed record Link(string Href, string? Rel);
}
