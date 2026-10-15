using Novashelf.Api.Models;

namespace Novashelf.Api.Sources;

public sealed class ContentResolver(SpaceflightNewsClient articles, ApodClient apod, NasaImagesClient images)
{
    public Task<ContentItem?> GetAsync(string id, CancellationToken ct)
    {
        if (ContentId.TryParseArticle(id, out var articleId)) return articles.GetArticleAsync(articleId, ct);
        if (ContentId.TryParseApod(id, out var date)) return apod.GetAsync(date, ct);
        if (ContentId.TryParseImage(id, out var nasaId)) return images.GetAsync(nasaId, ct);
        return Task.FromResult<ContentItem?>(null);
    }
}
