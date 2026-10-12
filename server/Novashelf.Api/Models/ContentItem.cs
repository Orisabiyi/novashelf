namespace Novashelf.Api.Models;

public enum ContentKind
{
    Article,
    Apod,
    Image,
}

/// <summary>
/// A piece of readable content from any source, normalised to one shape for the front end.
/// </summary>
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
