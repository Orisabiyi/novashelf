namespace Novashelf.Api.Data;

/// <summary>Something a reader opened in the reader. One row per reader and item; re-reading updates it.</summary>
public sealed class ReadingHistoryEntry
{
    /// <summary>Most entries kept per reader; older ones are dropped.</summary>
    public const int MaxPerUser = 500;

    public long Id { get; set; }

    public required string UserId { get; set; }

    public required string ContentId { get; set; }

    /// <summary>The item's details as JSON (title, image, source…), so history needs no lookups.</summary>
    public required string Item { get; set; }

    public DateTimeOffset ReadAt { get; set; }
}
