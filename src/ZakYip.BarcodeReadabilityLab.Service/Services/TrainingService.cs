using System.Collections.Concurrent;
using ZakYip.BarcodeReadabilityLab.Service.Models;

namespace ZakYip.BarcodeReadabilityLab.Service.Services;

public interface ITrainingService
{
    Task<string> StartTrainingAsync(string trainingDataPath);
    TrainingStatus? GetTrainingStatus(string taskId);
    Task<bool> CancelTrainingAsync(string taskId);
}

public class TrainingService : ITrainingService
{
    private readonly IMLModelService _mlModelService;
    private readonly ILogger<TrainingService> _logger;
    private readonly ConcurrentDictionary<string, TrainingStatus> _trainingTasks = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellationTokens = new();

    public TrainingService(IMLModelService mlModelService, ILogger<TrainingService> logger)
    {
        _mlModelService = mlModelService;
        _logger = logger;
    }

    public async Task<string> StartTrainingAsync(string trainingDataPath)
    {
        var taskId = Guid.NewGuid().ToString();
        var cts = new CancellationTokenSource();
        var status = new TrainingStatus
        {
            TaskId = taskId,
            State = TrainingState.Running,
            Message = "Training started",
            StartTime = DateTime.UtcNow,
            Progress = 0.0
        };

        _trainingTasks[taskId] = status;
        _cancellationTokens[taskId] = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                _logger.LogInformation("Training task {TaskId} started", taskId);
                status.Progress = 0.1;

                await _mlModelService.TrainModelAsync(trainingDataPath, cts.Token);

                status.State = TrainingState.Completed;
                status.Message = "Training completed successfully";
                status.EndTime = DateTime.UtcNow;
                status.Progress = 1.0;
                _logger.LogInformation("Training task {TaskId} completed", taskId);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Training task {TaskId} was cancelled", taskId);
                status.State = TrainingState.Cancelled;
                status.Message = "Training was cancelled by user";
                status.EndTime = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Training task {TaskId} failed", taskId);
                status.State = TrainingState.Failed;
                status.Message = $"Training failed: {ex.Message}";
                status.EndTime = DateTime.UtcNow;
            }
            finally
            {
                _cancellationTokens.TryRemove(taskId, out _);
                cts.Dispose();
            }
        });

        return await Task.FromResult(taskId);
    }

    public TrainingStatus? GetTrainingStatus(string taskId)
    {
        return _trainingTasks.TryGetValue(taskId, out var status) ? status : null;
    }

    public async Task<bool> CancelTrainingAsync(string taskId)
    {
        if (!_cancellationTokens.TryGetValue(taskId, out var cts))
        {
            _logger.LogWarning("Training task {TaskId} not found or already completed", taskId);
            return false;
        }

        try
        {
            _logger.LogInformation("Cancelling training task {TaskId}", taskId);
            cts.Cancel();
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling training task {TaskId}", taskId);
            return false;
        }
    }
}
