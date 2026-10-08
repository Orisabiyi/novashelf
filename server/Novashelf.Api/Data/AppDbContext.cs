using Microsoft.EntityFrameworkCore;

namespace Novashelf.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<CachedArticle> CachedArticles => Set<CachedArticle>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<CachedArticle>(article =>
        {
            article.ToTable("cached_articles");
            article.HasKey(a => a.ContentId);
            article.Property(a => a.ContentId).HasColumnName("content_id").HasMaxLength(160);
            article.Property(a => a.Html).HasColumnName("html");
            article.Property(a => a.Byline).HasColumnName("byline").HasMaxLength(300);
            article.Property(a => a.ReadingMinutes).HasColumnName("reading_minutes");
            article.Property(a => a.FormatVersion).HasColumnName("format_version");
            article.Property(a => a.ExtractedAt).HasColumnName("extracted_at");
        });
    }
}

/// <summary>A reader extraction kept across restarts, so articles aren't re-fetched from publishers.</summary>
public sealed class CachedArticle
{
    public required string ContentId { get; set; }

    /// <summary>Sanitised article body with original image URLs (proxy links are added when served).</summary>
    public required string Html { get; set; }

    public string? Byline { get; set; }

    public int ReadingMinutes { get; set; }

    /// <summary>Version of the extraction/clean-up rules that produced <see cref="Html"/>.</summary>
    public int FormatVersion { get; set; }

    public DateTimeOffset ExtractedAt { get; set; }
}
