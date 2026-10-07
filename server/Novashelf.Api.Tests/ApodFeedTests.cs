using Novashelf.Api.Models;
using Novashelf.Api.Sources;

namespace Novashelf.Api.Tests;

/// <summary>Parses a trimmed copy of https://science.nasa.gov/feed/apod-basic/.</summary>
public class ApodFeedTests
{
    private const string Feed = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0" xmlns:content="http://purl.org/rss/1.0/modules/content/" xmlns:apod="https://science.nasa.gov/apod/">
        <channel>
          <title>APOD</title>
          <item>
            <title>A Complete Auroral Oval from SMILE</title>
            <link>https://science.nasa.gov/image-article/apod-2026-october-6-a-complete-auroral-oval-from-smile/</link>
            <pubDate>Tue, 06 Oct 2026 04:05:00 +0000</pubDate>
            <apod:hdurl>https://assets.science.nasa.gov/dynamicimage/assets/science/cds/apod/apod/2026/october/smile.jpeg?w=512&amp;h=512&amp;fit=clip</apod:hdurl>
            <apod:credit><![CDATA[<a href="https://www.esa.int/">ESA</a>, <a href="https://english.cas.cn/">CAS</a>]]></apod:credit>
            <apod:explanation><![CDATA[<strong>Explanation:</strong> Have you ever seen a complete <a href="x">auroral oval</a>? You can&#039;t see one from the ground.]]></apod:explanation>
          </item>
          <item>
            <title>Supernova Remnant Pa 30 </title>
            <link>https://science.nasa.gov/image-article/apod-2026-october-7-supernova-remnant-pa-30/</link>
            <pubDate>Wed, 07 Oct 2026 04:05:00 +0000</pubDate>
            <apod:hdurl>https://assets.science.nasa.gov/dynamicimage/assets/science/cds/apod/apod/2026/october/noirlab2624a.jpg?w=3233&amp;h=3429&amp;fit=clip</apod:hdurl>
            <apod:credit></apod:credit>
            <apod:explanation><![CDATA[<strong>Explanation: </strong>"Happy New Year!" No, wait, this is not a <a href="y">fireworks display</a>.]]></apod:explanation>
          </item>
          <item>
            <title>A Video Day</title>
            <link>https://science.nasa.gov/image-article/apod-2026-october-4-a-video-day/</link>
            <pubDate>Sun, 04 Oct 2026 04:05:00 +0000</pubDate>
            <apod:hdurl>https://www.youtube.com/embed/abc</apod:hdurl>
            <apod:explanation>Watch this.</apod:explanation>
          </item>
        </channel>
        </rss>
        """;

    [Fact]
    public void Items_are_mapped_newest_first()
    {
        var feed = ApodClient.ParseFeed(Feed);

        Assert.Equal(["apod-2026-10-07", "apod-2026-10-06", "apod-2026-10-04"], feed.Items.Select(i => i.Id));
        Assert.Equal(new DateOnly(2026, 10, 4), feed.OldestDate);
    }

    [Fact]
    public void Item_fields_are_cleaned()
    {
        var item = ApodClient.ParseFeed(Feed).Items.Single(i => i.Id == "apod-2026-10-06");

        Assert.Equal(ContentKind.Apod, item.Kind);
        Assert.Equal("A Complete Auroral Oval from SMILE", item.Title);
        Assert.Equal("Have you ever seen a complete auroral oval? You can't see one from the ground.", item.Summary);
        Assert.Equal("ESA, CAS", item.Publisher);
        Assert.Equal("https://science.nasa.gov/image-article/apod-2026-october-6-a-complete-auroral-oval-from-smile/", item.Url);
        Assert.Equal(
            "https://assets.science.nasa.gov/dynamicimage/assets/science/cds/apod/apod/2026/october/smile.jpeg?w=1600&fit=clip",
            item.ImageUrl);
    }

    [Fact]
    public void Missing_credit_defaults_and_title_is_trimmed()
    {
        var item = ApodClient.ParseFeed(Feed).Items[0];

        Assert.Equal("Supernova Remnant Pa 30", item.Title);
        Assert.Equal("NASA APOD", item.Publisher);
        Assert.Equal("\"Happy New Year!\" No, wait, this is not a fireworks display.", item.Summary);
    }

    [Fact]
    public void Non_nasa_media_has_no_image() =>
        Assert.Null(ApodClient.ParseFeed(Feed).Items.Single(i => i.Id == "apod-2026-10-04").ImageUrl);

    [Fact]
    public void Dtd_is_not_processed()
    {
        const string evil = """
            <?xml version="1.0"?>
            <!DOCTYPE rss [<!ENTITY xxe SYSTEM "file:///etc/passwd">]>
            <rss><channel><item>
              <title>&xxe;</title>
              <link>https://science.nasa.gov/image-article/apod-2026-october-7-x/</link>
            </item></channel></rss>
            """;

        // The entity is never resolved, so the document is rejected instead of leaking the file.
        var ex = Assert.Throws<System.Xml.XmlException>(() => ApodClient.ParseFeed(evil));
        Assert.Contains("undeclared entity", ex.Message);
    }
}
