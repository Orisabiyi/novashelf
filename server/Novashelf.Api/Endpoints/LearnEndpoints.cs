using Microsoft.AspNetCore.Http.HttpResults;
using Novashelf.Api.Learn;

namespace Novashelf.Api.Endpoints;

public static class LearnEndpoints
{
    public static IEndpointRouteBuilder MapLearnEndpoints(this IEndpointRouteBuilder app)
    {
        var learn = app.MapGroup("/api/learn");
        learn.MapGet("/", GetTracks);
        learn.MapGet("/{track}/{lesson}", GetLesson);
        return app;
    }

    public sealed record TrackSummary(string Slug, string Title, string Summary, string Icon, bool StartHere, IReadOnlyList<LessonSummary> Lessons);

    public sealed record LessonSummary(string Slug, string Title, string Summary);

    public sealed record LessonResponse(
        TrackRef Track,
        LessonDetail Lesson,
        int Number,
        int LessonsInTrack,
        LessonRef? Previous,
        LessonRef? Next,
        IReadOnlyList<LessonSection> Sections);

    public sealed record TrackRef(string Slug, string Title);

    public sealed record LessonDetail(string Slug, string Title, string Summary, string? Topic, IReadOnlyList<string> KeyIdeas);

    private static Ok<IReadOnlyList<TrackSummary>> GetTracks(Curriculum curriculum) =>
        TypedResults.Ok<IReadOnlyList<TrackSummary>>(curriculum.Tracks
            .Select(t => new TrackSummary(t.Slug, t.Title, t.Summary, t.Icon, t.StartHere,
                t.Lessons.Select(l => new LessonSummary(l.Slug, l.Title, l.Summary)).ToList()))
            .ToList());

    private static async Task<Results<Ok<LessonResponse>, NotFound>> GetLesson(
        string track, string lesson, Curriculum curriculum, LessonService lessons, CancellationToken ct)
    {
        if (curriculum.Find(track, lesson) is not { } found) return TypedResults.NotFound();

        // Have the next lesson ready by the time the reader gets there.
        if (found.Next is { } next && curriculum.Find(next.Track, next.Slug) is { } upcoming)
            lessons.Warm(upcoming.Lesson);

        var sections = await lessons.GetSectionsAsync(found.Lesson, ct);
        return TypedResults.Ok(new LessonResponse(
            new TrackRef(found.Track.Slug, found.Track.Title),
            new LessonDetail(found.Lesson.Slug, found.Lesson.Title, found.Lesson.Summary, found.Lesson.Topic, found.Lesson.KeyIdeas),
            found.Number,
            found.Track.Lessons.Count,
            found.Previous,
            found.Next,
            sections));
    }
}
