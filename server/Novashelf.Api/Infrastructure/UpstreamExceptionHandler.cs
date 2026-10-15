using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Polly;

namespace Novashelf.Api.Infrastructure;

public sealed class UpstreamExceptionHandler(IProblemDetailsService problemDetails, ILogger<UpstreamExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
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
