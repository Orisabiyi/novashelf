using System.Net;
using Novashelf.Api.Infrastructure;
using Novashelf.Api.Models;
using Novashelf.Api.Reading;

namespace Novashelf.Api.Tests;

public class OutboundGuardTests
{
    [Theory]
    [InlineData("93.184.215.14")]      // example.com
    [InlineData("8.8.8.8")]
    [InlineData("172.32.0.1")]         // just outside 172.16.0.0/12
    [InlineData("100.128.0.1")]        // just outside 100.64.0.0/10
    [InlineData("2606:4700:4700::1111")]
    public void Public_addresses_are_allowed(string ip) =>
        Assert.True(OutboundGuard.IsPublicAddress(IPAddress.Parse(ip)));

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("0.0.0.0")]
    [InlineData("10.1.2.3")]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.255")]
    [InlineData("192.168.1.10")]
    [InlineData("169.254.169.254")]    // cloud metadata endpoint
    [InlineData("100.64.0.1")]
    [InlineData("224.0.0.1")]
    [InlineData("255.255.255.255")]
    [InlineData("::1")]
    [InlineData("::")]
    [InlineData("fe80::1")]
    [InlineData("fd00::1")]
    [InlineData("::ffff:127.0.0.1")]   // IPv4-mapped loopback
    [InlineData("::ffff:169.254.169.254")]
    public void Private_and_special_addresses_are_refused(string ip) =>
        Assert.False(OutboundGuard.IsPublicAddress(IPAddress.Parse(ip)));
}

public class ArticleSanitizerTests
{
    private const string Base = "https://www.esa.int/Science/Mars/article";

    [Fact]
    public void Scripts_styles_and_their_contents_are_removed()
    {
        var html = ArticleSanitizer.Sanitize(
            "<p>Hello</p><script>alert('x')</script><style>p{color:red}</style><noscript>enable js</noscript>", Base);

        Assert.Equal("<p>Hello</p>", html);
    }

    [Fact]
    public void Event_handlers_inline_styles_and_classes_are_stripped()
    {
        var html = ArticleSanitizer.Sanitize(
            "<p class=\"lead\" style=\"color:red\" onclick=\"steal()\">Text</p>", Base);

        Assert.Equal("<p>Text</p>", html);
    }

    [Fact]
    public void Javascript_links_lose_their_href()
    {
        var html = ArticleSanitizer.Sanitize("<a href=\"javascript:steal()\">click</a>", Base);

        Assert.DoesNotContain("javascript", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(">click</a>", html);
    }

    [Fact]
    public void Links_open_safely_in_a_new_tab()
    {
        var html = ArticleSanitizer.Sanitize("<a href=\"https://nasa.gov\">NASA</a>", Base);

        Assert.Contains("target=\"_blank\"", html);
        Assert.Contains("rel=\"noopener noreferrer nofollow\"", html);
    }

    [Fact]
    public void Relative_urls_resolve_against_the_article()
    {
        var html = ArticleSanitizer.Sanitize("<img src=\"/images/mars.jpg\" alt=\"Mars\"><a href=\"next\">next</a>", Base);

        Assert.Contains("src=\"https://www.esa.int/images/mars.jpg\"", html);
        Assert.Contains("href=\"https://www.esa.int/Science/Mars/next\"", html);
    }

    [Fact]
    public void Images_are_lazy_and_send_no_referrer()
    {
        var html = ArticleSanitizer.Sanitize("<img src=\"https://x.org/a.jpg\" alt=\"a\">", Base);

        Assert.Contains("loading=\"lazy\"", html);
        Assert.Contains("referrerpolicy=\"no-referrer\"", html);
    }

    [Fact]
    public void Placeholder_images_are_dropped()
    {
        var html = ArticleSanitizer.Sanitize("<p>x</p><img src=\"data:image/gif;base64,R0lGOD\"><img alt=\"lazy\">", Base);

        Assert.Equal("<p>x</p>", html);
    }

    [Fact]
    public void Embeds_and_forms_are_removed_but_wrappers_keep_their_text()
    {
        var html = ArticleSanitizer.Sanitize(
            "<div><span>Kept text</span></div><iframe src=\"https://evil.example\"></iframe><form><input name=\"q\"></form>", Base);

        Assert.Equal("Kept text", html);
    }
}

public class PageChromeTests
{
    private const string Base = "https://science.nasa.gov/photojournal/x/";

    [Fact]
    public void Template_furniture_is_removed()
    {
        var html = ArticleSanitizer.Sanitize(
            "<p>2 Min Read</p><p>10/07/2026</p><p>The rover looked back.</p><p>Keep Exploring</p>" +
            "<h2>Discover More Topics From Photojournal</h2>", Base);

        Assert.Equal("<p>The rover looked back.</p>", html);
    }

    [Fact]
    public void Empty_blocks_are_removed_but_image_figures_stay()
    {
        var html = ArticleSanitizer.Sanitize(
            "<p></p><p> &nbsp; </p><div><p><span></span></p></div><figure><img src=\"/a.jpg\" alt=\"a\"></figure><p>Text</p><ul><li></li></ul>", Base);

        Assert.StartsWith("<figure><img src=\"https://science.nasa.gov/a.jpg\"", html);
        Assert.EndsWith("</figure><p>Text</p><ul></ul>", html);
    }

    [Fact]
    public void Headings_followed_by_content_and_sentences_mentioning_reading_stay()
    {
        var html = ArticleSanitizer.Sanitize(
            "<h2>Why it matters</h2><p>A 5 min read can still explain a lot.</p><p>Launch is set for 10/07/2026.</p>", Base);

        Assert.Equal("<h2>Why it matters</h2><p>A 5 min read can still explain a lot.</p><p>Launch is set for 10/07/2026.</p>", html);
    }
}

public class ReaderSummaryTests
{
    private static ContentItem Item(string summary) =>
        new("apod-2026-10-07", ContentKind.Apod, "Pa 30", summary, null, null, "NASA APOD", null);

    [Fact]
    public void Summary_paragraphs_are_html_encoded()
    {
        var readable = ReaderService.FromSummary(Item("First <b>bold</b> & more.\n\nSecond paragraph."), extracted: true);

        Assert.Equal("<p>First &lt;b&gt;bold&lt;/b&gt; &amp; more.</p>\n<p>Second paragraph.</p>", readable.Html);
        Assert.True(readable.Extracted);
    }

    [Fact]
    public void Reading_time_is_at_least_a_minute()
    {
        Assert.Equal(1, ReaderService.FromSummary(Item("Short."), extracted: false).ReadingMinutes);
        Assert.Equal(2, ReaderService.FromSummary(Item(string.Join(' ', Enumerable.Repeat("word", 460))), extracted: false).ReadingMinutes);
    }
}

public class StoredItemTests
{
    [Fact]
    public void Item_details_round_trip_through_storage()
    {
        var item = new ContentItem("sfn-40309", ContentKind.Article, "Title", "Summary", "https://x.org/a.jpg",
            "https://x.org/story", "NASA", new DateTimeOffset(2026, 10, 8, 9, 30, 0, TimeSpan.Zero),
            new TopicTag("moon", "Moon"));

        Assert.Equal(item, ReaderService.DeserializeItem(ReaderService.SerializeItem(item)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("{not json")]
    public void Missing_or_broken_item_details_mean_no_stored_item(string? json) =>
        Assert.Null(ReaderService.DeserializeItem(json));
}
