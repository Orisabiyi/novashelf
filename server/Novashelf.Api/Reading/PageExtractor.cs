using System.Net.Http.Headers;
using System.Text;
using ReadabilityParser = SmartReader.Reader;

namespace Novashelf.Api.Reading;

public sealed record ExtractedPage(string Html, string? Byline, int ReadingMinutes);

public sealed class PageExtractor(HttpClient http, ILogger<PageExtractor> logger)
{
    private const int MaxPageBytes = 5 * 1024 * 1024;
    private const int MinArticleTextLength = 400;

    public async Task<ExtractedPage?> ExtractAsync(string url, CancellationToken ct)
    {
        try
        {
            var page = await FetchHtmlAsync(url, ct);
            if (page is null) return null;

            var article = new ReadabilityParser(page.Value.FinalUrl, page.Value.Html).GetArticle();
            if (!article.IsReadable || string.IsNullOrWhiteSpace(article.Content)) return null;
            if ((article.TextContent?.Trim().Length ?? 0) < MinArticleTextLength) return null;

            var body = ArticleSanitizer.Sanitize(article.Content, page.Value.FinalUrl);
            var minutes = (int)Math.Round(article.TimeToRead.TotalMinutes);
            var byline = FirstNonEmpty(article.Byline, article.Author);
            return new ExtractedPage(body, byline, Math.Max(1, minutes));
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogInformation(ex, "Extractor: couldn't extract {Url}", url);
            return null;
        }
    }

    public async Task<(string Html, string FinalUrl)?> FetchHtmlAsync(string url, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml;q=0.9,*/*;q=0.1");

        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogInformation("Extractor: {Url} returned {Status}", url, (int)response.StatusCode);
            return null;
        }

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (mediaType is not null && !mediaType.Contains("html", StringComparison.OrdinalIgnoreCase))
            return null;

        var html = await ReadLimitedAsync(response.Content, ct);
        if (html is null)
        {
            logger.LogInformation("Extractor: {Url} is larger than {Max} bytes", url, MaxPageBytes);
            return null;
        }

        return (html, response.RequestMessage?.RequestUri?.ToString() ?? url);
    }

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

    private static string? FirstNonEmpty(params string?[] values) =>
        values.Select(v => v?.Trim()).FirstOrDefault(v => !string.IsNullOrEmpty(v));
}
