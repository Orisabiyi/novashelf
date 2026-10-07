using Novashelf.Api.Models;

namespace Novashelf.Api.Tests;

public class ContentIdTests
{
    [Fact]
    public void Article_ids_round_trip()
    {
        Assert.True(ContentId.TryParseArticle(ContentId.ForArticle(40277), out var id));
        Assert.Equal(40277, id);
    }

    [Theory]
    [InlineData("sfn-")]
    [InlineData("sfn-0")]
    [InlineData("sfn--5")]
    [InlineData("sfn-12a")]
    [InlineData("sfn- 12")]
    [InlineData("apod-2026-10-05")]
    [InlineData("40277")]
    public void Malformed_article_ids_are_rejected(string id) =>
        Assert.False(ContentId.TryParseArticle(id, out _));

    [Fact]
    public void Apod_ids_round_trip()
    {
        var date = new DateOnly(2026, 10, 5);
        Assert.Equal("apod-2026-10-05", ContentId.ForApod(date));
        Assert.True(ContentId.TryParseApod("apod-2026-10-05", out var parsed));
        Assert.Equal(date, parsed);
    }

    [Theory]
    [InlineData("apod-2026-13-01")]
    [InlineData("apod-26-10-05")]
    [InlineData("apod-")]
    public void Malformed_apod_ids_are_rejected(string id) =>
        Assert.False(ContentId.TryParseApod(id, out _));

    [Theory]
    [InlineData("img-PIA12345", "PIA12345")]
    [InlineData("img-NHQ201905310037", "NHQ201905310037")]
    [InlineData("img-jsc2010e123_v2.1", "jsc2010e123_v2.1")]
    public void Image_ids_parse(string id, string expected)
    {
        Assert.True(ContentId.TryParseImage(id, out var nasaId));
        Assert.Equal(expected, nasaId);
    }

    [Theory]
    [InlineData("img-")]
    [InlineData("img-../../etc/passwd")]
    [InlineData("img-a b")]
    [InlineData("img-a&q=mars")]
    public void Unsafe_image_ids_are_rejected(string id) =>
        Assert.False(ContentId.TryParseImage(id, out _));
}
