using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Novashelf.Api.Data;
using Novashelf.Api.Models;
using Novashelf.Api.Reading;

namespace Novashelf.Api.Accounts;

public sealed record HistoryEntry(ContentItem Item, DateTimeOffset ReadAt);

/// <summary>Marks items the signed-in reader has already opened. A no-op without accounts.</summary>
public interface IReadMarkers
{
    Task<PagedResult<ContentItem>> MarkAsync(ClaimsPrincipal user, PagedResult<ContentItem> page, CancellationToken ct);
}

public sealed class NoReadMarkers : IReadMarkers
{
    public Task<PagedResult<ContentItem>> MarkAsync(ClaimsPrincipal user, PagedResult<ContentItem> page, CancellationToken ct) =>
        Task.FromResult(page);
}

public sealed class ReadingHistory(AppDbContext db, UserManager<AppUser> users, TimeProvider time) : IReadMarkers
{
    public const int PageSize = 20;

    public async Task<PagedResult<ContentItem>> MarkAsync(ClaimsPrincipal user, PagedResult<ContentItem> page, CancellationToken ct)
    {
        var userId = users.GetUserId(user);
        if (userId is null || page.Items.Count == 0) return page;

        var ids = page.Items.Select(i => i.Id).ToList();
        var read = await db.ReadingHistory
            .Where(e => e.UserId == userId && ids.Contains(e.ContentId))
            .Select(e => e.ContentId)
            .ToHashSetAsync(ct);

        return page with { Items = page.Items.Select(i => i with { Read = read.Contains(i.Id) }).ToList() };
    }

    /// <summary>Adds or refreshes an entry, keeping at most <see cref="ReadingHistoryEntry.MaxPerUser"/>.</summary>
    public async Task RecordAsync(string userId, ContentItem item, CancellationToken ct)
    {
        var now = time.GetUtcNow();
        var existing = await db.ReadingHistory.FirstOrDefaultAsync(e => e.UserId == userId && e.ContentId == item.Id, ct);
        if (existing is null)
        {
            db.ReadingHistory.Add(new ReadingHistoryEntry
            {
                UserId = userId,
                ContentId = item.Id,
                Item = ReaderService.SerializeItem(item with { Read = null }),
                ReadAt = now,
            });
        }
        else
        {
            existing.ReadAt = now;
            existing.Item = ReaderService.SerializeItem(item with { Read = null });
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException) when (existing is null)
        {
            // The same item recorded twice at once (e.g. two tabs); one row is enough.
            return;
        }

        // Trim the oldest entries now and then rather than on every read.
        if (Random.Shared.Next(20) == 0)
        {
            var cutoff = await db.ReadingHistory.Where(e => e.UserId == userId)
                .OrderByDescending(e => e.ReadAt).Skip(ReadingHistoryEntry.MaxPerUser)
                .Select(e => (DateTimeOffset?)e.ReadAt).FirstOrDefaultAsync(ct);
            if (cutoff is not null)
                await db.ReadingHistory.Where(e => e.UserId == userId && e.ReadAt <= cutoff).ExecuteDeleteAsync(ct);
        }
    }

    public async Task<PagedResult<HistoryEntry>> ListAsync(string userId, int page, CancellationToken ct)
    {
        var rows = await db.ReadingHistory.AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.ReadAt)
            .Skip((page - 1) * PageSize).Take(PageSize + 1)
            .Select(e => new { e.Item, e.ReadAt })
            .ToListAsync(ct);

        var entries = rows.Take(PageSize)
            .Select(r => ReaderService.DeserializeItem(r.Item) is { } item ? new HistoryEntry(item with { Read = true }, r.ReadAt) : null)
            .OfType<HistoryEntry>()
            .ToList();
        return new PagedResult<HistoryEntry>(entries, page, rows.Count > PageSize);
    }

    public Task ClearAsync(string userId, CancellationToken ct) =>
        db.ReadingHistory.Where(e => e.UserId == userId).ExecuteDeleteAsync(ct);

    public Task RemoveAsync(string userId, string contentId, CancellationToken ct) =>
        db.ReadingHistory.Where(e => e.UserId == userId && e.ContentId == contentId).ExecuteDeleteAsync(ct);
}
