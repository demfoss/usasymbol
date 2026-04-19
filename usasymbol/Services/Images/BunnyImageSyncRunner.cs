namespace USASymbol.Services.Images;

public sealed class BunnyImageSyncRunner
{
    private readonly BunnyImageSyncService _syncService;
    private readonly ILogger<BunnyImageSyncRunner> _logger;

    public BunnyImageSyncRunner(
        BunnyImageSyncService syncService,
        ILogger<BunnyImageSyncRunner> logger)
    {
        _syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting Bunny image sync runner.");
            var result = await _syncService.SyncAsync(cancellationToken);

            _logger.LogInformation(
                "Bunny image sync completed. Uploaded: {Uploaded}, Skipped: {Skipped}, Failed: {Failed}",
                result.Uploaded,
                result.Skipped,
                result.Failed);

            return result.Failed > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bunny image sync failed.");
            return 1;
        }
    }
}
