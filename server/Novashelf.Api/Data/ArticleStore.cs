using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Novashelf.Api.Data;

/// <summary>Persistent store for extracted articles. Failures never break the reader.</summary>
public interface IArticleStore
{
    Task<CachedArticle?> GetAsync(string contentId, CancellationToken ct);

    Task SaveAsync(CachedArticle article, CancellationToken ct);
}

/// <summary>Used when no database is configured: the in-memory cache is all there is.</summary>
public sealed class NullArticleStore : IArticleStore
{
    public Task<CachedArticle?> GetAsync(string contentId, CancellationToken ct) => Task.FromResult<CachedArticle?>(null);

    public Task SaveAsync(CachedArticle article, CancellationToken ct) => Task.CompletedTask;
}

/// <summary>
/// Postgres-backed store. Uses a context factory because background warming outlives the request
/// that started it. A database hiccup is logged and treated as a cache miss.
/// </summary>
public sealed class PostgresArticleStore(IDbContextFactory<AppDbContext> contexts, ILogger<PostgresArticleStore> logger)
    : IArticleStore
{
    public async Task<CachedArticle?> GetAsync(string contentId, CancellationToken ct)
    {
        try
        {
            await using var db = await contexts.CreateDbContextAsync(ct);
            return await db.CachedArticles.AsNoTracking().FirstOrDefaultAsync(a => a.ContentId == contentId, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Article store: read failed for {ContentId}", contentId);
            return null;
        }
    }

    public async Task SaveAsync(CachedArticle article, CancellationToken ct)
    {
        try
        {
            await using var db = await contexts.CreateDbContextAsync(ct);
            var existing = await db.CachedArticles.FindAsync([article.ContentId], ct);
            if (existing is null)
            {
                db.CachedArticles.Add(article);
            }
            else
            {
                existing.Html = article.Html;
                existing.Byline = article.Byline;
                existing.ReadingMinutes = article.ReadingMinutes;
                existing.FormatVersion = article.FormatVersion;
                existing.ExtractedAt = article.ExtractedAt;
                existing.Item = article.Item;
            }
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Another instance saved the same article first; theirs is just as good.
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Article store: write failed for {ContentId}", article.ContentId);
        }
    }
}
