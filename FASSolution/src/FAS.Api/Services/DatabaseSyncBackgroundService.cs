namespace FAS.Api.Services;

public class DatabaseSyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseSyncBackgroundService> _logger;
    private readonly IConfiguration _configuration;
    private readonly bool _autoSyncEnabled;
    private readonly int _syncIntervalMinutes;

    public DatabaseSyncBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseSyncBackgroundService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;

        var syncSettings = _configuration.GetSection("SyncSettings");
        _autoSyncEnabled = syncSettings.GetValue<bool>("AutoSyncEnabled", true);
        _syncIntervalMinutes = syncSettings.GetValue<int>("SyncIntervalMinutes", 30);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Database Sync Background Service started. Auto-sync: {Enabled}, Interval: {Minutes} minutes",
            _autoSyncEnabled, _syncIntervalMinutes);

        if (!_autoSyncEnabled)
        {
            _logger.LogInformation("Auto-sync is disabled. Service will not run.");
            return;
        }

        // Đợi 30 giây sau khi khởi động để các service khác khởi động xong
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Starting scheduled database sync");

                using var scope = _serviceProvider.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<IDatabaseSyncService>();

                await syncService.SyncAllTablesAsync();

                _logger.LogInformation("Scheduled database sync completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during scheduled database sync");
            }

            // Đợi đến lần sync tiếp theo
            await Task.Delay(TimeSpan.FromMinutes(_syncIntervalMinutes), stoppingToken);
        }

        _logger.LogInformation("Database Sync Background Service stopped");
    }
}
