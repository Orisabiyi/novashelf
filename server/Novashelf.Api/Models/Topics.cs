namespace Novashelf.Api.Models;

/// <param name="Keywords">Matched against article titles (any keyword matches).</param>
public sealed record Topic(string Slug, string Name, IReadOnlyList<string> Keywords);

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

    public static Topic? Find(string slug) =>
        All.FirstOrDefault(t => string.Equals(t.Slug, slug, StringComparison.OrdinalIgnoreCase));
}
