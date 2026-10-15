using System.Net.Mail;
using System.Text.RegularExpressions;
using Novashelf.Api.Data;

namespace Novashelf.Api.Accounts;

public sealed record RegisterRequest(string? Email, string? Password, string? FirstName, bool AgeConfirmed);

public sealed record LoginRequest(string? Email, string? Password, bool Remember = true);

public sealed record UpdateAccountRequest(string? FirstName);

public sealed record ChangePasswordRequest(string? CurrentPassword, string? NewPassword);

public sealed record DeleteAccountRequest(string? Password);

public sealed record AccountResponse(string Email, string? FirstName)
{
    public static AccountResponse From(AppUser user) => new(user.Email!, user.FirstName);
}

public static partial class AccountValidation
{
    public const int EmailMaxLength = 254;
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 128;

    public static Dictionary<string, string[]> Validate(RegisterRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (!IsValidEmail(request.Email))
            errors["email"] = ["Enter a valid email address."];

        var password = request.Password ?? "";
        if (password.Length < PasswordMinLength)
            errors["password"] = [$"Use at least {PasswordMinLength} characters."];
        else if (password.Length > PasswordMaxLength)
            errors["password"] = [$"Use at most {PasswordMaxLength} characters."];

        if ((NormalizeFirstName(request.FirstName)?.Length ?? 0) > AppUser.FirstNameMaxLength)
            errors["firstName"] = [$"Keep it under {AppUser.FirstNameMaxLength} characters."];

        if (!request.AgeConfirmed)
            errors["ageConfirmed"] = ["You need to be 13 or older to create an account."];

        return errors;
    }

    public static bool IsValidEmail(string? email)
    {
        email = email?.Trim();
        if (string.IsNullOrEmpty(email) || email.Length > EmailMaxLength) return false;

        // MailAddress also accepts display-name forms ("Ada <ada@x.org>"); require a bare address
        // with a dotted domain.
        return MailAddress.TryCreate(email, out var address)
            && address.Address == email
            && address.Host.Contains('.')
            && !address.Host.StartsWith('.')
            && !address.Host.EndsWith('.');
    }

    public static string? NormalizeFirstName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return Whitespace().Replace(name.Trim(), " ");
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
