using System.Text.Json;
using System.Text.RegularExpressions;

namespace Novashelf.Api.Learn;

/// <summary>
/// The beginner learning paths: tracks of lessons in order. Edited by hand in curriculum.json and
/// compiled into the API, so the server only ever fetches the sources listed there.
/// </summary>
public sealed partial class Curriculum
{
    public static readonly IReadOnlySet<string> NasaHosts = new HashSet<string> { "science.nasa.gov", "www.nasa.gov" };

    public required IReadOnlyList<Track> Tracks { get; init; }

    /// <summary>The lesson with its track, and the lessons before and after it across all tracks.</summary>
    public (Track Track, Lesson Lesson, int Number, LessonRef? Previous, LessonRef? Next)? Find(string trackSlug, string lessonSlug)
    {
        var all = Tracks.SelectMany(t => t.Lessons.Select(l => (Track: t, Lesson: l))).ToList();
        var index = all.FindIndex(x => x.Track.Slug == trackSlug && x.Lesson.Slug == lessonSlug);
        if (index < 0) return null;

        var (track, lesson) = all[index];
        LessonRef? Ref(int i) => i >= 0 && i < all.Count ? new LessonRef(all[i].Track.Slug, all[i].Lesson.Slug, all[i].Lesson.Title, all[i].Track.Title) : null;
        return (track, lesson, track.Lessons.ToList().IndexOf(lesson) + 1, Ref(index - 1), Ref(index + 1));
    }

    public static Curriculum LoadEmbedded()
    {
        using var stream = typeof(Curriculum).Assembly.GetManifestResourceStream("Novashelf.Api.Learn.curriculum.json")
            ?? throw new InvalidOperationException("curriculum.json is not embedded in the assembly.");
        return Parse(stream);
    }

    internal static Curriculum Parse(Stream json)
    {
        var curriculum = JsonSerializer.Deserialize<Curriculum>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidDataException("curriculum.json is empty.");
        var problems = curriculum.Validate();
        if (problems.Count > 0)
            throw new InvalidDataException("curriculum.json is invalid:\n- " + string.Join("\n- ", problems));
        return curriculum;
    }

    /// <summary>Everything that could go wrong when hand-editing the file.</summary>
    internal List<string> Validate()
    {
        var problems = new List<string>();
        if (Tracks is not { Count: > 0 }) return ["there are no tracks"];
        if (Tracks.Count(t => t.StartHere) != 1) problems.Add("exactly one track should have startHere: true");

        foreach (var duplicate in Tracks.GroupBy(t => t.Slug).Where(g => g.Count() > 1))
            problems.Add($"track slug '{duplicate.Key}' is used more than once");

        foreach (var track in Tracks)
        {
            if (!Slug().IsMatch(track.Slug ?? "")) problems.Add($"track slug '{track.Slug}' isn't lowercase-with-dashes");
            if (string.IsNullOrWhiteSpace(track.Title)) problems.Add($"track '{track.Slug}' has no title");
            if (track.Lessons is not { Count: > 0 }) problems.Add($"track '{track.Slug}' has no lessons");

            foreach (var duplicate in (track.Lessons ?? []).GroupBy(l => l.Slug).Where(g => g.Count() > 1))
                problems.Add($"lesson slug '{duplicate.Key}' is repeated in track '{track.Slug}'");

            foreach (var lesson in track.Lessons ?? [])
            {
                var where = $"{track.Slug}/{lesson.Slug}";
                if (!Slug().IsMatch(lesson.Slug ?? "")) problems.Add($"lesson slug '{where}' isn't lowercase-with-dashes");
                if (string.IsNullOrWhiteSpace(lesson.Title)) problems.Add($"lesson '{where}' has no title");
                if (lesson.KeyIdeas is not { Count: >= 2 and <= 5 }) problems.Add($"lesson '{where}' needs 2-5 key ideas");
                if (lesson.Sources is not { Count: >= 1 }) problems.Add($"lesson '{where}' has no sources");

                foreach (var source in lesson.Sources ?? [])
                {
                    switch (source.Kind)
                    {
                        case LessonSource.Wikipedia when !WikipediaPage().IsMatch(source.Page ?? ""):
                            problems.Add($"lesson '{where}' has an invalid Wikipedia page name '{source.Page}'");
                            break;
                        case LessonSource.Nasa when !Uri.TryCreate(source.Url, UriKind.Absolute, out var uri)
                                                    || uri.Scheme != "https" || !NasaHosts.Contains(uri.Host):
                            problems.Add($"lesson '{where}' has a NASA source that isn't an https NASA page: '{source.Url}'");
                            break;
                        case not (LessonSource.Wikipedia or LessonSource.Nasa):
                            problems.Add($"lesson '{where}' has an unknown source kind '{source.Kind}'");
                            break;
                    }
                }
            }
        }
        return problems;
    }

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex Slug();

    // Article titles as they appear in Wikipedia URLs, e.g. "Rover_(space_exploration)".
    [GeneratedRegex(@"^[A-Za-z0-9_()\-.,']{1,120}$")]
    private static partial Regex WikipediaPage();
}

public sealed record Track(string Slug, string Title, string Summary, string Icon, bool StartHere, IReadOnlyList<Lesson> Lessons);

/// <param name="Topic">An Explore topic slug for the lesson's "In the news" articles, if any.</param>
public sealed record Lesson(
    string Slug,
    string Title,
    string Summary,
    string? Topic,
    IReadOnlyList<string> KeyIdeas,
    IReadOnlyList<LessonSource> Sources);

/// <param name="Kind">"wikipedia" (Simple English Wikipedia article name in Page) or "nasa" (page Url).</param>
public sealed record LessonSource(string Kind, string Title, string? Page, string? Url)
{
    public const string Wikipedia = "wikipedia";
    public const string Nasa = "nasa";
}

public sealed record LessonRef(string Track, string Slug, string Title, string TrackTitle);
