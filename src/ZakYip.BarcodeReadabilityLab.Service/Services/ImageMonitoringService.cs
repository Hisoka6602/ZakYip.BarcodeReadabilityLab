using Microsoft.Extensions.Options;
using ZakYip.BarcodeReadabilityLab.Service.Configuration;

namespace ZakYip.BarcodeReadabilityLab.Service.Services;

public class ImageMonitoringService : BackgroundService
{
    private readonly BarcodeReadabilityServiceSettings _settings;
    private readonly IMLModelService _mlModelService;
    private readonly ILogger<ImageMonitoringService> _logger;
    private FileSystemWatcher? _watcher;

    public ImageMonitoringService(
        IOptions<BarcodeReadabilityServiceSettings> settings,
        IMLModelService mlModelService,
        ILogger<ImageMonitoringService> logger)
    {
        _settings = settings.Value;
        _mlModelService = mlModelService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Image Monitoring Service started");

        EnsureDirectoriesExist();
        ProcessExistingFiles();
        StartFileSystemWatcher();

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(_settings.MonitorPath);
        Directory.CreateDirectory(_settings.UnableToAnalyzePath);
        Directory.CreateDirectory(_settings.TrainingDataPath);
        Directory.CreateDirectory(_settings.ModelPath);

        _logger.LogInformation("Directories ensured: Monitor={Monitor}, UnableToAnalyze={UnableToAnalyze}", 
            _settings.MonitorPath, _settings.UnableToAnalyzePath);
    }

    private void ProcessExistingFiles()
    {
        try
        {
            var files = Directory.GetFiles(_settings.MonitorPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f => _settings.SupportedImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));

            foreach (var file in files)
            {
                _ = ProcessImageAsync(file);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing existing files");
        }
    }

    private void StartFileSystemWatcher()
    {
        try
        {
            _watcher = new FileSystemWatcher(_settings.MonitorPath)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                EnableRaisingEvents = true
            };

            foreach (var ext in _settings.SupportedImageExtensions)
            {
                _watcher.Filters.Add($"*{ext}");
            }

            _watcher.Created += OnFileCreated;
            _watcher.Changed += OnFileChanged;

            _logger.LogInformation("File system watcher started for path: {MonitorPath}", _settings.MonitorPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting file system watcher");
        }
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        _logger.LogInformation("New file detected: {FilePath}", e.FullPath);
        _ = ProcessImageAsync(e.FullPath);
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        _logger.LogDebug("File changed: {FilePath}", e.FullPath);
    }

    private async Task ProcessImageAsync(string imagePath)
    {
        try
        {
            await Task.Delay(500);

            if (!File.Exists(imagePath))
            {
                _logger.LogWarning("File no longer exists: {ImagePath}", imagePath);
                return;
            }

            if (!_mlModelService.IsModelLoaded)
            {
                _logger.LogWarning("Model not loaded, moving {ImagePath} to unable to analyze folder", imagePath);
                MoveToUnableToAnalyze(imagePath, "Model not loaded");
                return;
            }

            var prediction = await _mlModelService.PredictAsync(imagePath);

            if (prediction == null)
            {
                _logger.LogWarning("Prediction failed for {ImagePath}, moving to unable to analyze folder", imagePath);
                MoveToUnableToAnalyze(imagePath, "Prediction failed");
                return;
            }

            var maxScore = prediction.Score.Length > 0 ? prediction.Score.Max() : 0;
            
            if (maxScore < _settings.ConfidenceThreshold)
            {
                _logger.LogInformation(
                    "Low confidence ({Confidence:P2}) for {ImagePath}, predicted as {Label}, moving to unable to analyze folder",
                    maxScore, imagePath, prediction.PredictedLabel);
                MoveToUnableToAnalyze(imagePath, $"Low confidence: {maxScore:P2}, Predicted: {prediction.PredictedLabel}");
            }
            else
            {
                _logger.LogInformation(
                    "Successfully analyzed {ImagePath}: {Label} (confidence: {Confidence:P2})",
                    imagePath, prediction.PredictedLabel, maxScore);
                
                File.Delete(imagePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing image {ImagePath}", imagePath);
            try
            {
                MoveToUnableToAnalyze(imagePath, $"Error: {ex.Message}");
            }
            catch (Exception moveEx)
            {
                _logger.LogError(moveEx, "Error moving file to unable to analyze folder");
            }
        }
    }

    private void MoveToUnableToAnalyze(string imagePath, string reason)
    {
        try
        {
            var fileName = Path.GetFileName(imagePath);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var newFileName = $"{timestamp}_{fileName}";
            var destinationPath = Path.Combine(_settings.UnableToAnalyzePath, newFileName);

            var reasonFilePath = Path.ChangeExtension(destinationPath, ".txt");
            File.WriteAllText(reasonFilePath, $"Reason: {reason}\nOriginal Path: {imagePath}\nTimestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            File.Copy(imagePath, destinationPath, overwrite: true);
            File.Delete(imagePath);

            _logger.LogInformation("Moved {ImagePath} to {DestinationPath}", imagePath, destinationPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move file {ImagePath} to unable to analyze folder", imagePath);
            throw;
        }
    }

    public override void Dispose()
    {
        _watcher?.Dispose();
        base.Dispose();
    }
}
