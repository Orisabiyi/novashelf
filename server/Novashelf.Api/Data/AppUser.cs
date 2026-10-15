using Microsoft.AspNetCore.Identity;

namespace Novashelf.Api.Data;

public sealed class AppUser : IdentityUser
{
    public const int FirstNameMaxLength = 60;

    public string? FirstName { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset AgeConfirmedAt { get; set; }
}
