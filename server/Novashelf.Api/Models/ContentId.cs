using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Novashelf.Api.Models;

/// <summary>
/// Content ids are prefixed with their source: <c>sfn-40277</c>, <c>apod-2026-10-05</c>, <c>img-PIA12345</c>.
/// </summary>
public static class ContentId
{
    public const string ArticlePrefix = "sfn-";
    public const string ApodPrefix = "apod-";
    public const string ImagePrefix = "img-";

    public static string ForArticle(int id) => ArticlePrefix + id.ToString(CultureInfo.InvariantCulture);

    public static string ForApod(DateOnly date) => ApodPrefix + date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static string ForImage(string nasaId) => ImagePrefix + nasaId;

    public static bool TryParseArticle(string id, out int articleId)
    {
        articleId = 0;
        return id.StartsWith(ArticlePrefix, StringComparison.Ordinal)
            && int.TryParse(id.AsSpan(ArticlePrefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out articleId)
            && articleId > 0;
    }

    public static bool TryParseApod(string id, out DateOnly date)
    {
        date = default;
        return id.StartsWith(ApodPrefix, StringComparison.Ordinal)
            && DateOnly.TryParseExact(id.AsSpan(ApodPrefix.Length), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    public static bool TryParseImage(string id, [NotNullWhen(true)] out string? nasaId)
    {
        nasaId = null;
        if (!id.StartsWith(ImagePrefix, StringComparison.Ordinal)) return false;

        var rest = id[ImagePrefix.Length..];
        // NASA ids are short and use letters, digits, '-', '_' and '.'.
        if (rest.Length is 0 or > 128 || !rest.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.'))
            return false;

        nasaId = rest;
        return true;
    }
}
