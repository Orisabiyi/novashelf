namespace Novashelf.Api.Data;

public sealed class UserTopic
{
    public required string UserId { get; set; }

    public required string Slug { get; set; }

    public DateTimeOffset FollowedAt { get; set; }
}
