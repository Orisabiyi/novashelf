using System.Text;
using Novashelf.Api.Learn;

namespace Novashelf.Api.Tests;

public class CurriculumTests
{
    private static readonly Curriculum Real = Curriculum.LoadEmbedded();

    [Fact]
    public void The_shipped_curriculum_is_valid() => Assert.Empty(Real.Validate());

    [Fact]
    public void The_start_here_track_comes_first() =>
        Assert.True(Real.Tracks[0].StartHere);

    [Fact]
    public void Every_lesson_topic_is_a_real_explore_topic()
    {
        var topics = Models.Topics.All.Select(t => t.Slug).ToHashSet();
        var unknown = Real.Tracks.SelectMany(t => t.Lessons).Where(l => l.Topic is not null && !topics.Contains(l.Topic));
        Assert.Empty(unknown);
    }

    [Fact]
    public void Previous_and_next_cross_track_boundaries()
    {
        var firstTrack = Real.Tracks[0];
        var lastOfFirst = firstTrack.Lessons[^1];
        var found = Real.Find(firstTrack.Slug, lastOfFirst.Slug)!.Value;

        Assert.Equal(firstTrack.Lessons.Count, found.Number);
        Assert.Equal(Real.Tracks[1].Slug, found.Next!.Track);
        Assert.Equal(Real.Tracks[1].Lessons[0].Slug, found.Next.Slug);

        var veryFirst = Real.Find(firstTrack.Slug, firstTrack.Lessons[0].Slug)!.Value;
        Assert.Null(veryFirst.Previous);
        Assert.Null(Real.Find(Real.Tracks[^1].Slug, Real.Tracks[^1].Lessons[^1].Slug)!.Value.Next);
    }

    [Fact]
    public void Unknown_lessons_are_not_found() =>
        Assert.Null(Real.Find("stars", "no-such-lesson"));

    [Fact]
    public void Mistakes_in_the_file_are_reported()
    {
        const string json = """
        { "tracks": [
          { "slug": "Bad Slug", "title": "T", "summary": "", "icon": "pi-star", "startHere": false, "lessons": [
            { "slug": "one", "title": "One", "summary": "", "keyIdeas": ["only one"],
              "sources": [
                { "kind": "nasa", "title": "x", "url": "https://evil.example.com/page" },
                { "kind": "wikipedia", "title": "y", "page": "../../etc" },
                { "kind": "blog", "title": "z" }
              ] },
            { "slug": "one", "title": "", "summary": "", "keyIdeas": ["a", "b"], "sources": [] }
          ] }
        ] }
        """;

        var error = Assert.Throws<InvalidDataException>(() => Curriculum.Parse(new MemoryStream(Encoding.UTF8.GetBytes(json))));

        Assert.Contains("exactly one track should have startHere", error.Message);
        Assert.Contains("isn't lowercase-with-dashes", error.Message);
        Assert.Contains("needs 2-5 key ideas", error.Message);
        Assert.Contains("isn't an https NASA page", error.Message);
        Assert.Contains("invalid Wikipedia page name", error.Message);
        Assert.Contains("unknown source kind 'blog'", error.Message);
        Assert.Contains("repeated in track", error.Message);
        Assert.Contains("has no title", error.Message);
        Assert.Contains("has no sources", error.Message);
    }

    [Fact]
    public void Cache_keys_are_stable_and_distinct()
    {
        var a = new LessonSource("wikipedia", "Sun", "Sun", null);
        var b = new LessonSource("nasa", "Sun", null, "https://science.nasa.gov/sun/facts/");
        Assert.Equal(LessonService.CacheKey(a), LessonService.CacheKey(a with { Title = "Different title" }));
        Assert.NotEqual(LessonService.CacheKey(a), LessonService.CacheKey(b));
        Assert.StartsWith("learn-wikipedia-", LessonService.CacheKey(a));
    }
}

public class WikipediaArticlesTests
{
    // A trimmed Parsoid response, the format the Wikipedia REST API returns.
    private const string Parsoid = """
        <html><head><style>.x{}</style></head><body>
        <section data-mw-section-id="0">
          <div class="hatnote">For other uses, see Sun (disambiguation).</div>
          <table class="infobox"><tr><td>Mass: 1.989e30 kg</td></tr></table>
          <p>The <a href="./Star">Sun</a> is a star.<sup class="mw-ref reference"><a href="#cite_note-1">[1]</a></sup></p>
          <figure typeof="mw:File/Thumb"><img src="//upload.wikimedia.org/thumb/a/ab/Sun.jpg/220px-Sun.jpg"
            srcset="//upload.wikimedia.org/thumb/a/ab/Sun.jpg/330px-Sun.jpg 1.5x, //upload.wikimedia.org/thumb/a/ab/Sun.jpg/440px-Sun.jpg 2x" alt="The Sun"><figcaption>The Sun</figcaption></figure>
        </section>
        <section data-mw-section-id="1"><h2 id="Energy">Energy</h2><p>It makes energy by fusion.</p></section>
        <section data-mw-section-id="2"><h2 id="Related_pages">Related pages</h2><ul><li><a href="./Moon">Moon</a></li></ul></section>
        <section data-mw-section-id="3"><h2 id="References">References</h2><ol><li>Some book</li></ol></section>
        </body></html>
        """;

    private static readonly string Cleaned = WikipediaArticles.Clean(Parsoid, "Sun");

    [Fact]
    public void Article_text_and_headings_are_kept()
    {
        Assert.Contains("is a star.", Cleaned);
        Assert.Contains("<h2>Energy</h2>", Cleaned);
        Assert.Contains("It makes energy by fusion.", Cleaned);
    }

    [Fact]
    public void Reference_markers_infoboxes_and_hatnotes_are_removed()
    {
        Assert.DoesNotContain("[1]", Cleaned);
        Assert.DoesNotContain("Mass:", Cleaned);
        Assert.DoesNotContain("For other uses", Cleaned);
        Assert.DoesNotContain(".x{}", Cleaned);
    }

    [Fact]
    public void Reference_and_link_sections_are_removed()
    {
        Assert.DoesNotContain("Related pages", Cleaned);
        Assert.DoesNotContain("References", Cleaned);
        Assert.DoesNotContain("Some book", Cleaned);
    }

    [Fact]
    public void Links_point_to_the_wikipedia_article_and_images_use_the_largest_rendition()
    {
        Assert.Contains("href=\"https://simple.wikipedia.org/wiki/Star\"", Cleaned);
        Assert.Contains("src=\"https://upload.wikimedia.org/thumb/a/ab/Sun.jpg/440px-Sun.jpg\"", Cleaned);
        Assert.Contains("<figcaption>The Sun</figcaption>", Cleaned);
    }
}

public class LessonTextTests
{
    private static string Paragraph(int chars) => $"<p>{new string('x', chars)}</p>";

    [Fact]
    public void Short_sections_are_untouched()
    {
        var html = "<h2>A</h2>" + Paragraph(1000);
        Assert.Equal((html, false), LessonText.Shorten(html, target: 5000));
    }

    [Fact]
    public void Long_sections_stop_before_the_next_heading_when_it_is_close()
    {
        var html = Paragraph(3000) + Paragraph(2500) + Paragraph(500) + "<h2>Next part</h2>" + Paragraph(4000);
        var (shortened, truncated) = LessonText.Shorten(html, target: 5000);

        Assert.True(truncated);
        Assert.DoesNotContain("Next part", shortened);
        Assert.Equal(3, shortened.Split("<p>").Length - 1);
    }

    [Fact]
    public void Without_a_nearby_heading_it_stops_after_the_current_block_and_drops_trailing_headings()
    {
        var html = Paragraph(4000) + "<h2>Mid</h2>" + Paragraph(2000) + Paragraph(4000) + Paragraph(4000) + "<h2>End</h2>";
        var (shortened, truncated) = LessonText.Shorten(html, target: 5000);

        Assert.True(truncated);
        Assert.Contains("Mid", shortened);
        Assert.Equal(2, shortened.Split("<p>").Length - 1);
        Assert.DoesNotContain("End", shortened);
    }

    [Fact]
    public void A_single_huge_block_after_a_decent_read_is_left_out()
    {
        var html = "<h3>Intro</h3>" + Paragraph(2000) + Paragraph(1000) + "<h3>More</h3><figure>" + new string('y', 7000) + "</figure>";
        var (shortened, truncated) = LessonText.Shorten(html, target: 5000);

        Assert.True(truncated);
        Assert.DoesNotContain("<figure>", shortened);
        Assert.DoesNotContain("More", shortened);
        Assert.Contains("Intro", shortened);
    }
}
