namespace Novashelf.Api.Data;

/// <summary>A lesson a reader has finished.</summary>
public sealed class LessonProgress
{
    public required string UserId { get; set; }

    public required string Track { get; set; }

    public required string Lesson { get; set; }

    public DateTimeOffset CompletedAt { get; set; }
}
