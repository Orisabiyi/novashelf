namespace Novashelf.Api.Data;

public sealed class UserPreferences
{
    public static readonly string[] FontSizes = ["sm", "md", "lg"];
    public static readonly string[] Widths = ["narrow", "normal", "wide"];
    public static readonly string[] Themes = ["dark", "light"];

    public required string UserId { get; set; }

    public required string FontSize { get; set; }

    public required string Width { get; set; }

    public required string Theme { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
