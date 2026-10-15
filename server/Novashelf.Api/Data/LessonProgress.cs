namespace Novashelf.Api.Data;

public sealed class LessonProgress
{
    public required string UserId { get; set; }

    public required string Track { get; set; }

    public required string Lesson { get; set; }

    public DateTimeOffset CompletedAt { get; set; }
}
