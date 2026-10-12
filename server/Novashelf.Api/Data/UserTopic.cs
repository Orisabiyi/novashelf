namespace Novashelf.Api.Data;

/// <summary>A topic a reader follows (a slug from <see cref="Models.Topics"/>).</summary>
public sealed class UserTopic
{
    public required string UserId { get; set; }

    public required string Slug { get; set; }

    public DateTimeOffset FollowedAt { get; set; }
}
