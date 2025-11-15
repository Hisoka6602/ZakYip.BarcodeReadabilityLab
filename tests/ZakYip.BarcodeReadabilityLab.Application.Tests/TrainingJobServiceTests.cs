using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZakYip.BarcodeReadabilityLab.Application.Options;
using ZakYip.BarcodeReadabilityLab.Application.Services;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Contracts;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Exceptions;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

namespace ZakYip.BarcodeReadabilityLab.Application.Tests;

public class TrainingJobServiceTests
{
    private readonly Mock<ITrainingJobRepository> _repositoryMock = new();
    private readonly Mock<IServiceProvider> _serviceProviderMock = new();
    private readonly Mock<IServiceScope> _serviceScopeMock = new();
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();
    private readonly Mock<ILogger<TrainingJobService>> _loggerMock = new();

    public TrainingJobServiceTests()
    {
        _serviceProviderMock
            .Setup(provider => provider.GetService(typeof(ITrainingJobRepository)))
            .Returns(_repositoryMock.Object);

        _serviceScopeMock
            .Setup(scope => scope.ServiceProvider)
            .Returns(_serviceProviderMock.Object);

        _scopeFactoryMock
            .Setup(factory => factory.CreateScope())
            .Returns(_serviceScopeMock.Object);
    }

    [Fact]
    public async Task StartTrainingAsync_ShouldPersistJobAndEnqueue()
    {
        using var trainingDirectory = CreateTempDirectory();
        using var outputDirectory = CreateTempDirectory();

        _repositoryMock
            .Setup(repository => repository.AddAsync(It.IsAny<TrainingJob>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        using var service = CreateService();

        var request = new TrainingRequest
        {
            TrainingRootDirectory = trainingDirectory.FullName,
            OutputModelDirectory = outputDirectory.FullName,
            ValidationSplitRatio = 0.2m,
            LearningRate = 0.01m,
            Epochs = 20,
            BatchSize = 16,
            DataAugmentation = new DataAugmentationOptions(),
            DataBalancing = new DataBalancingOptions()
        };

        var jobId = await service.StartTrainingAsync(request);

        _repositoryMock.Verify(repository => repository.AddAsync(It.Is<TrainingJob>(job =>
                job.JobId == jobId &&
                job.Status == TrainingJobState.Queued &&
                job.TrainingRootDirectory == request.TrainingRootDirectory &&
                job.OutputModelDirectory == request.OutputModelDirectory),
            It.IsAny<CancellationToken>()),
            Times.Once);

        var dequeued = service.TryDequeueJob();

        Assert.NotNull(dequeued);
        Assert.Equal(jobId, dequeued!.Value.jobId);
        Assert.Same(request, dequeued.Value.request);
    }

    [Fact]
    public async Task StartTrainingAsync_InvalidLearningRate_ShouldThrow()
    {
        using var trainingDirectory = CreateTempDirectory();
        using var outputDirectory = CreateTempDirectory();

        using var service = CreateService();

        var request = new TrainingRequest
        {
            TrainingRootDirectory = trainingDirectory.FullName,
            OutputModelDirectory = outputDirectory.FullName,
            ValidationSplitRatio = 0.2m,
            LearningRate = 0m,
            Epochs = 20,
            BatchSize = 16,
            DataAugmentation = new DataAugmentationOptions(),
            DataBalancing = new DataBalancingOptions()
        };

        await Assert.ThrowsAsync<TrainingException>(() => service.StartTrainingAsync(request));

        _repositoryMock.Verify(repository => repository.AddAsync(It.IsAny<TrainingJob>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetStatusAsync_WhenJobExists_ShouldMapToApplicationModel()
    {
        var jobId = Guid.NewGuid();
        var trainingJob = new TrainingJob
        {
            JobId = jobId,
            TrainingRootDirectory = "ignored",
            OutputModelDirectory = "ignored",
            LearningRate = 0.05m,
            Epochs = 10,
            BatchSize = 8,
            Status = TrainingJobState.Running,
            Progress = 0.4m,
            StartTime = DateTimeOffset.UtcNow,
            DataAugmentation = new DataAugmentationOptions { Enable = true },
            DataBalancing = new DataBalancingOptions { Strategy = DataBalancingStrategy.RandomOversampling }
        };

        _repositoryMock
            .Setup(repository => repository.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trainingJob);

        using var service = CreateService();

        var status = await service.GetStatusAsync(jobId);

        Assert.NotNull(status);
        Assert.Equal(TrainingStatus.Running, status!.Status);
        Assert.Equal(trainingJob.Progress, status.Progress);
        Assert.Equal(trainingJob.DataAugmentation, status.DataAugmentation);
        Assert.Equal(trainingJob.DataBalancing, status.DataBalancing);
    }

    [Fact]
    public async Task GetStatusAsync_WhenJobMissing_ShouldReturnNull()
    {
        var jobId = Guid.NewGuid();

        _repositoryMock
            .Setup(repository => repository.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TrainingJob?)null);

        using var service = CreateService();

        var status = await service.GetStatusAsync(jobId);

        Assert.Null(status);
    }

    [Fact]
    public async Task UpdateJobStatus_ShouldApplyActionAndPersist()
    {
        var jobId = Guid.NewGuid();
        var trainingJob = new TrainingJob
        {
            JobId = jobId,
            TrainingRootDirectory = "ignored",
            OutputModelDirectory = "ignored",
            LearningRate = 0.05m,
            Epochs = 10,
            BatchSize = 8,
            Status = TrainingJobState.Queued,
            Progress = 0.2m,
            StartTime = DateTimeOffset.UtcNow
        };

        _repositoryMock
            .Setup(repository => repository.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trainingJob);

        _repositoryMock
            .Setup(repository => repository.UpdateAsync(trainingJob, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        using var service = CreateService();

        await service.UpdateJobStatus(jobId, job => job.Progress = 0.9m);

        Assert.Equal(0.9m, trainingJob.Progress);
        _repositoryMock.Verify();
    }

    [Fact]
    public async Task UpdateJobToCompleted_ShouldSetFinalFields()
    {
        var jobId = Guid.NewGuid();
        var trainingJob = new TrainingJob
        {
            JobId = jobId,
            TrainingRootDirectory = "ignored",
            OutputModelDirectory = "ignored",
            LearningRate = 0.05m,
            Epochs = 10,
            BatchSize = 8,
            Status = TrainingJobState.Running,
            Progress = 0.8m,
            StartTime = DateTimeOffset.UtcNow
        };

        var metrics = new ModelEvaluationMetrics
        {
            Accuracy = 0.9m,
            MacroPrecision = 0.88m,
            MacroRecall = 0.87m,
            MacroF1Score = 0.89m,
            MicroPrecision = 0.9m,
            MicroRecall = 0.9m,
            MicroF1Score = 0.9m,
            ConfusionMatrixJson = "{}"
        };

        _repositoryMock
            .Setup(repository => repository.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trainingJob);

        TrainingJob? updatedJob = null;

        _repositoryMock
            .Setup(repository => repository.UpdateAsync(It.IsAny<TrainingJob>(), It.IsAny<CancellationToken>()))
            .Callback<TrainingJob, CancellationToken>((job, _) => updatedJob = job)
            .Returns(Task.CompletedTask);

        using var service = CreateService();

        await service.UpdateJobToCompleted(jobId, metrics);

        Assert.NotNull(updatedJob);
        Assert.Equal(TrainingJobState.Completed, updatedJob!.Status);
        Assert.Equal(1m, updatedJob.Progress);
        Assert.NotNull(updatedJob.CompletedTime);
        Assert.Equal(metrics, updatedJob.EvaluationMetrics);
    }

    [Fact]
    public async Task WaitAndReleaseTrainingSlot_ShouldRespectConcurrencyLimits()
    {
        using var service = CreateService(maxConcurrentJobs: 1);

        var initialSlots = service.GetAvailableTrainingSlots();
        Assert.Equal(1, initialSlots);

        await service.WaitForTrainingSlotAsync();

        var availableAfterWait = service.GetAvailableTrainingSlots();
        Assert.Equal(0, availableAfterWait);

        service.ReleaseTrainingSlot();

        var availableAfterRelease = service.GetAvailableTrainingSlots();
        Assert.Equal(1, availableAfterRelease);
    }

    private TrainingJobService CreateService(int maxConcurrentJobs = 2)
    {
        var options = Options.Create(new TrainingOptions
        {
            TrainingRootDirectory = "ignored",
            OutputModelDirectory = "ignored",
            MaxConcurrentTrainingJobs = maxConcurrentJobs
        });

        return new TrainingJobService(
            _loggerMock.Object,
            _scopeFactoryMock.Object,
            options);
    }

    private static TempDirectory CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var info = Directory.CreateDirectory(path);
        return new TempDirectory(info);
    }

    private sealed class TempDirectory : IAsyncDisposable, IDisposable
    {
        private readonly DirectoryInfo _directoryInfo;
        private bool _disposed;

        public TempDirectory(DirectoryInfo directoryInfo)
        {
            _directoryInfo = directoryInfo;
        }

        public string FullName => _directoryInfo.FullName;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_directoryInfo.Exists)
            {
                _directoryInfo.Delete(recursive: true);
            }

            _disposed = true;
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
