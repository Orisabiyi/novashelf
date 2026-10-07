using Novashelf.Api.Models;

namespace Novashelf.Api.Tests;

public class TopicTests
{
    [Theory]
    [InlineData("NASA’s Curiosity Rover Catches Stunning Martian Dawn", "mars")]
    [InlineData("Artemis II Crew Moon Photo Annotations", "moon")]
    [InlineData("Crew-12 set to depart the ISS after eight months", "iss")]
    [InlineData("SpaceX launches 24 Starlink satellites", "launches")]
    [InlineData("NASA’s Webb Captures Commotion From Nebula’s Stellar Jets", "telescopes")]
    [InlineData("Brake! Time for ESA’s Hera asteroid mission to go slow", "solar-system")]
    public void Titles_are_tagged_with_their_topic(string title, string expected) =>
        Assert.Equal(expected, Topics.Tag(title)?.Slug);

    [Theory]
    [InlineData("NASA Marshall tests new engine")]          // "mars" inside a word
    [InlineData("Why is NASA bringing contractors back?")]
    [InlineData("Announcing the finalists for the 2026 SpaceNews Icon Awards")]
    public void Unrelated_titles_have_no_topic(string title) =>
        Assert.Null(Topics.Tag(title));

    [Fact]
    public void Tag_carries_the_display_name() =>
        Assert.Equal(new TopicTag("iss", "Space Station"), Topics.Tag("Tiangong crew completes spacewalk"));
}
