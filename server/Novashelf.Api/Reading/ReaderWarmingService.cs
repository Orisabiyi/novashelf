using Novashelf.Api.Sources;

namespace Novashelf.Api.Reading;

// Publisher feeds only list their latest posts, so an article that can only be read through the feed
// fallback has to be stored before it drops off. Prepare the newest articles regularly; ones already
// stored are a database read, not another fetch.
public sealed class ReaderWarmingService(IServiceScopeFactory scopes, ILogger<ReaderWarmingService> logger)
    : BackgroundService
{
    private const int PagesToWarm = 2;

    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartupDelay, stoppingToken);
            using var timer = new PeriodicTimer(Interval);
            do
            {
                await WarmLatestAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task WarmLatestAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var articles = scope.ServiceProvider.GetRequiredService<SpaceflightNewsClient>();
            var reader = scope.ServiceProvider.GetRequiredService<ReaderService>();

            for (var page = 1; page <= PagesToWarm; page++)
            {
                var feed = await articles.GetArticlesAsync(keywords: null, page, ct);
                foreach (var item in feed.Items) await reader.PrepareAsync(item, ct);
                if (!feed.HasMore) break;
            }
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Reader warming: couldn't warm the latest articles");
        }
    }
}
