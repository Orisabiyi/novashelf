using System.Text.Json;
using Novashelf.Api.Models;
using Novashelf.Api.Sources;

namespace Novashelf.Api.Tests;

/// <summary>Maps trimmed real responses from https://science.nasa.gov/wp-json/wp/v2/apod-basic/{YYMMDD}.</summary>
public class ApodMappingTests
{
    private static readonly JsonSerializerOptions SnakeCase = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private static ContentItem MapJson(string json) =>
        ApodClient.Map(JsonSerializer.Deserialize<ApodClient.ApodBasic>(json, SnakeCase)!);

    [Fact]
    public void Image_entry_maps()
    {
        var item = MapJson("""
            {
              "date": "2020-01-01",
              "post_id": 1237840,
              "title": "Betelgeuse Imagined",
              "permalink": "https://science.nasa.gov/image-article/apod-2020-january-1-betelgeuse-imagined/",
              "media_type": "image",
              "explanation": "<strong>Explanation:</strong> Why is Betelgeuse fading? No one knows. <a href=\"https://www.aavso.org/\">Betelgeuse</a> , one of the brightest stars, is <a href=\"x\">half as bright</a>.",
              "credit": "<b> Illustration Credit: </b> <a href=\"https://www.eso.org/\" target=\"_blank\">ESO</a> , L. Calcada",
              "hdurl": "https://assets.science.nasa.gov/content/dam/science/cds/apod/apod/2020/january/BetelgeuseImagined_EsoCalcada_2662.jpg?w=2662&h=4000&fit=clip"
            }
            """);

        Assert.Equal("apod-2020-01-01", item.Id);
        Assert.Equal(ContentKind.Apod, item.Kind);
        Assert.Equal("Betelgeuse Imagined", item.Title);
        Assert.Equal("Why is Betelgeuse fading? No one knows. Betelgeuse, one of the brightest stars, is half as bright.", item.Summary);
        Assert.Equal("ESO, L. Calcada", item.Publisher);
        Assert.Equal("https://science.nasa.gov/image-article/apod-2020-january-1-betelgeuse-imagined/", item.Url);
        Assert.Equal(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero), item.PublishedAt);
    }

    [Fact]
    public void Video_entry_uses_its_thumbnail_and_strips_credit_label()
    {
        var item = MapJson("""
            {
              "date": "2026-08-23",
              "title": "Cassini Approaches Saturn",
              "media_type": "video",
              "explanation": "<strong>Explanation:</strong> What would it look like?",
              "credit": "<strong>Video Credit &amp; Copyright:</strong> <a href=\"https://ciclops.org/\">Cassini Imaging Team</a>",
              "permalink": "https://science.nasa.gov/image-article/apod/apod-2026-august-23-cassini-approaches-saturn/",
              "hdurl": "https://assets.science.nasa.gov/dynamicimage/assets/science/cds/apod/apod/2026/august/CassiniApproachesSaturn.jpg?w=1280&h=720&fit=clip"
            }
            """);

        Assert.Equal("Cassini Imaging Team", item.Publisher);
        Assert.Equal(
            "https://assets.science.nasa.gov/dynamicimage/assets/science/cds/apod/apod/2026/august/CassiniApproachesSaturn.jpg?w=1600&fit=clip",
            item.ImageUrl);
    }

    [Fact]
    public void Missing_credit_defaults_to_apod()
    {
        var item = MapJson("""{ "date": "1995-06-16", "title": "Neutron Star Earth", "explanation": "e", "credit": "" }""");
        Assert.Equal("NASA APOD", item.Publisher);
    }

    [Theory]
    // /content/dam/ ignores resize parameters; /dynamicimage/assets/ serves the same file resized.
    [InlineData(
        "https://assets.science.nasa.gov/content/dam/science/cds/apod/apod/2020/january/b.jpg?w=2662&h=4000",
        "https://assets.science.nasa.gov/dynamicimage/assets/science/cds/apod/apod/2020/january/b.jpg?w=1600&fit=clip")]
    [InlineData(
        "https://assets.science.nasa.gov/dynamicimage/assets/science/cds/apod/apod/2026/october/n.jpg?w=3233&h=3429&fit=clip",
        "https://assets.science.nasa.gov/dynamicimage/assets/science/cds/apod/apod/2026/october/n.jpg?w=1600&fit=clip")]
    // Generic site thumbnail used by some early entries, not the actual picture.
    [InlineData("https://assets.science.nasa.gov/dynamicimage/assets/science/astro/programs/cosmic-origins/images/misc/news-thumbnail.png?w=594", null)]
    [InlineData("https://www.youtube.com/embed/abc", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Image_urls_are_resized_or_dropped(string? hdUrl, string? expected) =>
        Assert.Equal(expected, ApodClient.ResizedImage(hdUrl));

    [Theory]
    [InlineData("""{ "date": "2026-10-07" }""")]
    [InlineData("""{ "date": "not-a-date", "title": "t" }""")]
    [InlineData("""{ "title": "t" }""")]
    public void Unexpected_shape_is_rejected(string json) =>
        Assert.Throws<JsonException>(() => MapJson(json));
}
