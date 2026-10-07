using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Caching.Memory;
using Novashelf.Api.Models;
using Novashelf.Api.Sources;
using ReadabilityParser = SmartReader.Reader;

namespace Novashelf.Api.Reading;

/// <param name="Html">Sanitised article body.</param>
/// <param name="Extracted">
/// True when <paramref name="Html"/> is the full piece. False when the original page couldn't be
/// extracted and this is only the summary; the reader then points to the original.
/// </param>
public sealed record ReadableContent(
    ContentItem Item,
    string Html,
    string? Byline,
    int ReadingMinutes,
    bool Extracted);

/// <summary>
/// Builds the reader view for a content item. Articles are fetched from their original site and run
/// through a Readability port, then sanitised. APOD entries and NASA images are already complete
/// in their description, so they need no fetch.
/// </summary>
public sealed class ReaderService(
    HttpClient http,
    ContentResolver resolver,
    IMemoryCache cache,
    ILogger<ReaderService> logger)
{
    private const int MaxPageBytes = 5 * 1024 * 1024;
    private const int MinArticleTextLength = 400;
    private const int WordsPerMinute = 230;

    private static readonly TimeSpan ExtractedTtl = TimeSpan.FromHours(12);
    private static readonly TimeSpan FallbackTtl = TimeSpan.FromMinutes(15);

    /// <summary>Returns null when the id doesn't exist.</summary>
    public async Task<ReadableContent?> GetAsync(string id, CancellationToken ct)
    {
        var item = await resolver.GetAsync(id, ct);
        if (item is null) return null;

        if (item.Kind != ContentKind.Article || item.Url is null)
            return FromSummary(item, extracted: true);

        return await cache.GetOrCreateAsync($"reader:{item.Id}", async entry =>
        {
            var extracted = await ExtractAsync(item, ct);
            // Retry failed extractions sooner: the site may have been briefly down.
            entry.AbsoluteExpirationRelativeToNow = extracted is null ? FallbackTtl : ExtractedTtl;
            return extracted ?? FromSummary(item, extracted: false);
        });
    }

    private async Task<ReadableContent?> ExtractAsync(ContentItem item, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, item.Url);
            request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml;q=0.9,*/*;q=0.1");

            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogInformation("Reader: {Url} returned {Status}", item.Url, (int)response.StatusCode);
                return null;
            }

            var mediaType = response.Content.Headers.ContentType?.MediaType;
            if (mediaType is not null && !mediaType.Contains("html", StringComparison.OrdinalIgnoreCase))
                return null;

            var html = await ReadLimitedAsync(response.Content, ct);
            if (html is null)
            {
                logger.LogInformation("Reader: {Url} is larger than {Max} bytes", item.Url, MaxPageBytes);
                return null;
            }

            // After redirects, resolve relative links against where the page actually lives.
            var pageUrl = response.RequestMessage?.RequestUri?.ToString() ?? item.Url!;
            var article = new ReadabilityParser(pageUrl, html).GetArticle();
            if (!article.IsReadable || string.IsNullOrWhiteSpace(article.Content)) return null;

            var body = ArticleSanitizer.Sanitize(article.Content, pageUrl);
            if ((article.TextContent?.Trim().Length ?? 0) < MinArticleTextLength) return null;

            var minutes = (int)Math.Round(article.TimeToRead.TotalMinutes);
            var byline = FirstNonEmpty(article.Byline, article.Author);
            return new ReadableContent(item, body, byline, Math.Max(1, minutes), Extracted: true);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            // Network errors, refused private addresses, timeouts or a page the parser chokes on:
            // all fall back to the summary rather than failing the request.
            logger.LogInformation(ex, "Reader: couldn't extract {Url}", item.Url);
            return null;
        }
    }

    /// <summary>Reads the body up to <see cref="MaxPageBytes"/>; null if it's bigger.</summary>
    private static async Task<string?> ReadLimitedAsync(HttpContent content, CancellationToken ct)
    {
        if (content.Headers.ContentLength > MaxPageBytes) return null;

        await using var stream = await content.ReadAsStreamAsync(ct);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + read > MaxPageBytes) return null;
            buffer.Write(chunk, 0, read);
        }

        return GetEncoding(content.Headers.ContentType).GetString(buffer.GetBuffer(), 0, (int)buffer.Length);
    }

    private static Encoding GetEncoding(MediaTypeHeaderValue? contentType)
    {
        try
        {
            return contentType?.CharSet is { Length: > 0 } charset
                ? Encoding.GetEncoding(charset.Trim('"'))
                : Encoding.UTF8;
        }
        catch (ArgumentException)
        {
            return Encoding.UTF8;
        }
    }

    /// <summary>Wraps an item's plain-text summary as reader HTML, one paragraph per block.</summary>
    internal static ReadableContent FromSummary(ContentItem item, bool extracted)
    {
        var paragraphs = item.Summary
            .Split(["\r\n\r\n", "\n\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => $"<p>{HtmlEncoder.Default.Encode(p)}</p>");

        var words = item.Summary.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        var minutes = Math.Max(1, (int)Math.Round(words / (double)WordsPerMinute));

        return new ReadableContent(item, string.Join("\n", paragraphs), Byline: null, minutes, extracted);
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.Select(v => v?.Trim()).FirstOrDefault(v => !string.IsNullOrEmpty(v));
}
