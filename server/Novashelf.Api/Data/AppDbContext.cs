using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Novashelf.Api.Data;

/// <summary>
/// The app's database: reader accounts (ASP.NET Core Identity), the keys that encrypt sign-in
/// cookies (so sessions survive restarts and scale-to-zero), readers' personal data (history,
/// topics, preferences, lesson progress) and cached articles.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser>(options), IDataProtectionKeyContext
{
    public DbSet<CachedArticle> CachedArticles => Set<CachedArticle>();

    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    public DbSet<ReadingHistoryEntry> ReadingHistory => Set<ReadingHistoryEntry>();

    public DbSet<UserTopic> UserTopics => Set<UserTopic>();

    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();

    public DbSet<LessonProgress> LessonProgress => Set<LessonProgress>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);

        model.Entity<AppUser>(user =>
        {
            user.Property(u => u.FirstName).HasMaxLength(AppUser.FirstNameMaxLength);
        });

        model.Entity<ReadingHistoryEntry>(entry =>
        {
            entry.ToTable("reading_history");
            entry.Property(e => e.Id).HasColumnName("id");
            entry.Property(e => e.UserId).HasColumnName("user_id");
            entry.Property(e => e.ContentId).HasColumnName("content_id").HasMaxLength(160);
            entry.Property(e => e.Item).HasColumnName("item").HasColumnType("jsonb");
            entry.Property(e => e.ReadAt).HasColumnName("read_at");
            entry.HasIndex(e => new { e.UserId, e.ContentId }).IsUnique();
            entry.HasIndex(e => new { e.UserId, e.ReadAt });
            // Deleting an account deletes its history.
            entry.HasOne<AppUser>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<UserTopic>(topic =>
        {
            topic.ToTable("user_topics");
            topic.HasKey(t => new { t.UserId, t.Slug });
            topic.Property(t => t.UserId).HasColumnName("user_id");
            topic.Property(t => t.Slug).HasColumnName("slug").HasMaxLength(40);
            topic.Property(t => t.FollowedAt).HasColumnName("followed_at");
            topic.HasOne<AppUser>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<UserPreferences>(prefs =>
        {
            prefs.ToTable("user_preferences");
            prefs.HasKey(p => p.UserId);
            prefs.Property(p => p.UserId).HasColumnName("user_id");
            prefs.Property(p => p.FontSize).HasColumnName("font_size").HasMaxLength(10);
            prefs.Property(p => p.Width).HasColumnName("width").HasMaxLength(10);
            prefs.Property(p => p.Theme).HasColumnName("theme").HasMaxLength(10);
            prefs.Property(p => p.UpdatedAt).HasColumnName("updated_at");
            prefs.HasOne<AppUser>().WithOne().HasForeignKey<UserPreferences>(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<LessonProgress>(progress =>
        {
            progress.ToTable("lesson_progress");
            progress.HasKey(p => new { p.UserId, p.Track, p.Lesson });
            progress.Property(p => p.UserId).HasColumnName("user_id");
            progress.Property(p => p.Track).HasColumnName("track").HasMaxLength(80);
            progress.Property(p => p.Lesson).HasColumnName("lesson").HasMaxLength(80);
            progress.Property(p => p.CompletedAt).HasColumnName("completed_at");
            progress.HasOne<AppUser>().WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
        });

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
            article.Property(a => a.Item).HasColumnName("item").HasColumnType("jsonb");
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

    /// <summary>
    /// The item's details (title, image, source…) as JSON, so a stored article can be served without
    /// looking it up at its source again. Null for rows saved before this was added.
    /// </summary>
    public string? Item { get; set; }
}
