using System.Text.RegularExpressions;

namespace Novashelf.Api.Models;

/// <param name="Keywords">Matched against article titles (any keyword matches).</param>
public sealed record Topic(string Slug, string Name, IReadOnlyList<string> Keywords)
{
    // Whole words, plus plural/tense endings: "launch" matches "launches" and "launched", but
    // "mars" doesn't match "Marshall". Stricter than the upstream filter, which matches anywhere.
    private readonly Regex _titlePattern = new(
        @"\b(?:" + string.Join('|', Keywords.Select(Regex.Escape)) + @")(?:s|es|ed|ing)?\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public bool MatchesTitle(string title) => _titlePattern.IsMatch(title);
}

/// <summary>A topic label on a content item.</summary>
public sealed record TopicTag(string Slug, string Name);

public static class Topics
{
    public static readonly IReadOnlyList<Topic> All =
    [
        new("mars", "Mars", ["mars", "martian", "perseverance", "curiosity"]),
        new("moon", "Moon", ["moon", "lunar", "artemis"]),
        // No bare "iss": the API matches substrings, so it would hit every "mission".
        new("iss", "Space Station", ["space station", "tiangong", "spacewalk", "crew-"]),
        new("launches", "Launches", ["launch", "rocket", "falcon", "starship"]),
        new("telescopes", "Telescopes", ["telescope", "webb", "hubble", "jwst"]),
        new("exoplanets", "Exoplanets", ["exoplanet", "habitable"]),
        new("earth", "Earth", ["earth", "climate"]),
        new("solar-system", "Solar System", ["jupiter", "saturn", "venus", "mercury", "asteroid", "comet", "solar"]),
    ];

    /// <summary>The first topic (in list order) whose keywords appear in the title, if any.</summary>
    public static TopicTag? Tag(string title) =>
        All.FirstOrDefault(t => t.MatchesTitle(title)) is { } t ? new TopicTag(t.Slug, t.Name) : null;

    public static Topic? Find(string slug) =>
        All.FirstOrDefault(t => string.Equals(t.Slug, slug, StringComparison.OrdinalIgnoreCase));
}
