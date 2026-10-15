namespace Novashelf.Api.Models;

public enum ContentKind
{
    Article,
    Apod,
    Image,
}

public sealed record ContentItem(
    string Id,
    ContentKind Kind,
    string Title,
    string Summary,
    string? ImageUrl,
    string? Url,
    string? Publisher,
    DateTimeOffset? PublishedAt,
    TopicTag? Topic = null,
    bool? Read = null);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, bool HasMore);
