using Microsoft.EntityFrameworkCore;
using Novashelf.Api.Data;
using Novashelf.Api.Models;

namespace Novashelf.Api.Accounts;

public sealed class FollowedTopics(AppDbContext db, TimeProvider time)
{
    public async Task<IReadOnlyList<Topic>> GetAsync(string userId, CancellationToken ct)
    {
        var slugs = await db.UserTopics.Where(t => t.UserId == userId).Select(t => t.Slug).ToHashSetAsync(ct);
        return Topics.All.Where(t => slugs.Contains(t.Slug)).ToList();
    }

    public async Task SetAsync(string userId, IReadOnlyCollection<Topic> topics, CancellationToken ct)
    {
        var wanted = topics.Select(t => t.Slug).ToHashSet();
        var current = await db.UserTopics.Where(t => t.UserId == userId).ToListAsync(ct);

        db.UserTopics.RemoveRange(current.Where(t => !wanted.Contains(t.Slug)));
        var now = time.GetUtcNow();
        foreach (var slug in wanted.Except(current.Select(t => t.Slug)))
            db.UserTopics.Add(new UserTopic { UserId = userId, Slug = slug, FollowedAt = now });

        await db.SaveChangesAsync(ct);
    }
}
