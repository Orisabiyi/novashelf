using System.Text.Json;
using Novashelf.Api.Models;
using Novashelf.Api.Sources;

namespace Novashelf.Api.Tests;

/// <summary>Maps trimmed real responses from each API to <see cref="ContentItem"/>.</summary>
public class SourceMappingTests
{
    private static readonly JsonSerializerOptions SnakeCase = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    [Fact]
    public void Spaceflight_news_article_maps()
    {
        const string json = """
        {
          "id": 40277,
          "title": "Brake! Time for ESA's Hera asteroid mission to go slow ",
          "authors": [{ "name": "ESA", "socials": null }],
          "url": "https://www.esa.int/Space_Safety/Hera/Brake",
          "image_url": "https://www.esa.int/hera.gif",
          "news_site": "ESA",
          "summary": "Launched two years ago today...",
          "published_at": "2026-10-07T12:33:00Z",
          "featured": false
        }
        """;

        var item = SpaceflightNewsClient.Map(JsonSerializer.Deserialize<SpaceflightNewsClient.SfnArticle>(json, SnakeCase)!);

        Assert.Equal("sfn-40277", item.Id);
        Assert.Equal(ContentKind.Article, item.Kind);
        Assert.Equal("Brake! Time for ESA's Hera asteroid mission to go slow", item.Title);
        Assert.Equal("https://www.esa.int/hera.gif", item.ImageUrl);
        Assert.Equal("ESA", item.Publisher);
        Assert.Equal(new DateTimeOffset(2026, 10, 7, 12, 33, 0, TimeSpan.Zero), item.PublishedAt);
    }

    [Fact]
    public void Spaceflight_news_article_without_image_has_null_image()
    {
        const string json = """{ "id": 1, "title": "t", "url": "https://x", "image_url": "", "summary": "s" }""";
        var item = SpaceflightNewsClient.Map(JsonSerializer.Deserialize<SpaceflightNewsClient.SfnArticle>(json, SnakeCase)!);
        Assert.Null(item.ImageUrl);
    }



    [Fact]
    public void Nasa_image_maps_and_prefers_medium_rendition()
    {
        const string json = """
        {
          "data": [{
            "center": "HQ",
            "date_created": "2019-05-31T00:00:00Z",
            "description": "The Mars celebration.",
            "media_type": "image",
            "nasa_id": "NHQ201905310037",
            "photographer": "NASA/Bill Ingalls",
            "title": "Mars Celebration"
          }],
          "links": [
            { "href": "https://images-assets.nasa.gov/image/NHQ201905310037/NHQ201905310037~small.jpg", "rel": "alternate" },
            { "href": "https://images-assets.nasa.gov/image/NHQ201905310037/NHQ201905310037~thumb.jpg", "rel": "preview" },
            { "href": "https://images-assets.nasa.gov/image/NHQ201905310037/NHQ201905310037~medium.jpg", "rel": "alternate" }
          ]
        }
        """;

        var item = NasaImagesClient.Map(JsonSerializer.Deserialize<NasaImagesClient.Item>(json, SnakeCase)!);

        Assert.NotNull(item);
        Assert.Equal("img-NHQ201905310037", item.Id);
        Assert.Equal(ContentKind.Image, item.Kind);
        Assert.EndsWith("~medium.jpg", item.ImageUrl);
        Assert.Equal("NASA/Bill Ingalls", item.Publisher);
        Assert.Equal("https://images.nasa.gov/details/NHQ201905310037", item.Url);
    }

    [Fact]
    public void Nasa_image_without_data_is_skipped()
    {
        var item = NasaImagesClient.Map(new NasaImagesClient.Item(Data: null, Links: null));
        Assert.Null(item);
    }

    [Theory]
    [InlineData("Meet the finalists.\nThe post Announcing the finalists appeared first on SpaceNews.", "Meet the finalists.")]
    [InlineData("No footer here.", "No footer here.")]
    public void Spaceflight_news_summary_drops_wordpress_footer(string raw, string expected) =>
        Assert.Equal(expected, SpaceflightNewsClient.CleanSummary(raw));

}
