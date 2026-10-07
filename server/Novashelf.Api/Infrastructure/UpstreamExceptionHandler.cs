using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Polly;

namespace Novashelf.Api.Infrastructure;

/// <summary>
/// Turns failures talking to NASA / Spaceflight News into a 502 instead of a generic 500,
/// so the front end can show "the source is unavailable" rather than "something broke".
/// </summary>
public sealed class UpstreamExceptionHandler(IProblemDetailsService problemDetails, ILogger<UpstreamExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        // ExecutionRejectedException covers resilience-pipeline timeouts and open circuit breakers.
        var isUpstream = exception is HttpRequestException or JsonException or ExecutionRejectedException
            || (exception is TaskCanceledException && !context.RequestAborted.IsCancellationRequested);
        if (!isUpstream) return false;

        logger.LogWarning(exception, "Upstream content source failed for {Path}", context.Request.Path);

        context.Response.StatusCode = StatusCodes.Status502BadGateway;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails =
            {
                Title = "Content source unavailable",
                Detail = "A content source didn't respond. Please try again shortly.",
                Status = StatusCodes.Status502BadGateway,
            },
        });
    }
}
