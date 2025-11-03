using FAS.Core.Interfaces;

namespace FAS.Api;

public class SyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SyncBackgroundService> _logger;
    private readonly IConfiguration _configuration;

    public SyncBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<SyncBackgroundService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enableAutoSync = _configuration.GetValue<bool>("SyncSettings:EnableAutoSync");

        if (!enableAutoSync)
        {
            _logger.LogInformation("Auto sync is disabled");
            return;
        }

        var databaseSyncInterval = _configuration.GetValue<int>("SyncSettings:DatabaseSyncIntervalMinutes", 60);
        var pdfSyncInterval = _configuration.GetValue<int>("SyncSettings:PdfSyncIntervalMinutes", 30);

        _logger.LogInformation("Background sync service started. Database sync: {DbInterval}min, PDF sync: {PdfInterval}min",
            databaseSyncInterval, pdfSyncInterval);

        var lastDatabaseSync = DateTime.MinValue;
        var lastPdfSync = DateTime.MinValue;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;

                // Database sync
                if ((now - lastDatabaseSync).TotalMinutes >= databaseSyncInterval)
                {
                    _logger.LogInformation("Starting scheduled database sync");

                    using var scope = _serviceProvider.CreateScope();
                    var syncService = scope.ServiceProvider.GetRequiredService<ISyncService>();

                    var results = await syncService.SyncAllAsync();
                    var successCount = results.Count(r => r.IsSuccess);

                    _logger.LogInformation("Scheduled database sync completed: {Success}/{Total} successful",
                        successCount, results.Count);

                    lastDatabaseSync = now;
                }

                // PDF sync (if folder is configured)
                var pdfFolder = _configuration["SyncSettings:PdfFolder"];
                if (!string.IsNullOrWhiteSpace(pdfFolder) &&
                    (now - lastPdfSync).TotalMinutes >= pdfSyncInterval)
                {
                    _logger.LogInformation("Starting scheduled PDF sync from: {Folder}", pdfFolder);

                    using var scope = _serviceProvider.CreateScope();
                    var pdfService = scope.ServiceProvider.GetRequiredService<IPdfService>();

                    var results = await pdfService.SyncPdfFolderAsync(pdfFolder);
                    var successCount = results.Count(r => r.IsSuccess);

                    _logger.LogInformation("Scheduled PDF sync completed: {Success}/{Total} successful",
                        successCount, results.Count);

                    lastPdfSync = now;
                }

                // Wait for 1 minute before checking again
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background sync service");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Background sync service stopped");
    }
}
