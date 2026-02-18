
using CodeFlow.core.Models.Services;

namespace CodeFlow.Web.Services
{
    public class CacheWarmupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CacheWarmupService> _logger;

        public CacheWarmupService(IServiceProvider serviceProvider, ILogger<CacheWarmupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Cache warmup service started");

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogDebug("Starting periodic cache warmup");

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var hotQuestionCacheService = scope.ServiceProvider.GetRequiredService<IHotQuestionCacheService>();
                        await hotQuestionCacheService.WarmTask();
                    }

                    await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
                    _logger.LogDebug("Periodic cache warmup completed");
                }
                catch (Exception ex) 
                {
                    _logger.LogError(ex, "Error during cache warmup");
                }
            }
        }
    }
}
