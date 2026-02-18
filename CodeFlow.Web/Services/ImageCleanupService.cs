
namespace CodeFlow.Web.Services
{
    public class ImageCleanupService : BackgroundService
    {
        private readonly ILogger<ImageCleanupService> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ImageCleanupService(IWebHostEnvironment webHostEnvironment, ILogger<ImageCleanupService> logger)
        {
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Image clean up service started.");

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    CleanUpImages();
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during image clean up job");
                }
            }
        }

        public void CleanUpImages()
        {
            try
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Uploads", "images");

                var fileList = Directory.GetFiles(uploadsFolder, "*.png");

                foreach (var file in fileList)
                {
                    if (File.Exists(file))
                    {
                        var accessTime = File.GetLastAccessTimeUtc(file);
                        if(accessTime < DateTime.UtcNow.AddDays(-120))
                        {
                            File.Delete(file);
                            _logger.LogInformation("Removed unused file from {file}", file);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during image clean up job");
            }
        }
    }
}
