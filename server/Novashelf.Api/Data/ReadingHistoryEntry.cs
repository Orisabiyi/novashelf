namespace Novashelf.Api.Data;

public sealed class ReadingHistoryEntry
{
    public const int MaxPerUser = 500;

    public long Id { get; set; }

    public required string UserId { get; set; }

    public required string ContentId { get; set; }

    public required string Item { get; set; }

    public DateTimeOffset ReadAt { get; set; }
}
