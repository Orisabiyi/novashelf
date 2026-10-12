using Microsoft.AspNetCore.Identity;

namespace Novashelf.Api.Data;

/// <summary>A reader's account. Email and password hash come from <see cref="IdentityUser"/>.</summary>
public sealed class AppUser : IdentityUser
{
    public const int FirstNameMaxLength = 60;

    /// <summary>Optional, used to greet the reader. The email is shown when it's empty.</summary>
    public string? FirstName { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>When the reader confirmed they're 13 or older (we don't store their age).</summary>
    public DateTimeOffset AgeConfirmedAt { get; set; }
}
