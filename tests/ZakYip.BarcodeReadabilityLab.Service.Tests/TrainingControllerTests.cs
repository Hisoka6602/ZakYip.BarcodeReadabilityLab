using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZakYip.BarcodeReadabilityLab.Application.Options;
using ZakYip.BarcodeReadabilityLab.Application.Services;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enums;
using ZakYip.BarcodeReadabilityLab.Service.Controllers;
using ZakYip.BarcodeReadabilityLab.Service.Models;

namespace ZakYip.BarcodeReadabilityLab.Service.Tests;

public sealed class TrainingControllerTests
{
    private readonly Mock<ITrainingJobService> _trainingJobService = new();
    private readonly Mock<IOptions<TrainingOptions>> _trainingOptions = new();
    private readonly Mock<ILogger<TrainingController>> _logger = new();
    private readonly TrainingOptions _defaults;

    public TrainingControllerTests()
    {
        _defaults = new TrainingOptions
        {
            TrainingRootDirectory = Path.Combine(Path.GetTempPath(), "train-default"),
            OutputModelDirectory = Path.Combine(Path.GetTempPath(), "model-default"),
            ValidationSplitRatio = 0.25m,
            LearningRate = 0.02m,
            Epochs = 30,
            BatchSize = 12,
            DataAugmentation = new DataAugmentationOptions { Enable = true, AugmentedImagesPerSample = 2 },
            DataBalancing = new DataBalancingOptions { Strategy = DataBalancingStrategy.OverSample }
        };

        _trainingOptions.Setup(options => options.Value).Returns(_defaults);
    }

    [Fact]
    public async Task StartTraining_ShouldApplyDefaultOptions_WhenRequestOmittedValues()
    {
        TrainingRequest? capturedRequest = null;
        var jobId = Guid.NewGuid();

        _trainingJobService
            .Setup(service => service.StartTrainingAsync(It.IsAny<TrainingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jobId)
            .Callback<TrainingRequest, CancellationToken>((request, _) => capturedRequest = request);

        var controller = CreateController();
        var request = new StartTrainingRequest();

        var result = await controller.StartTraining(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<StartTrainingResponse>(okResult.Value);
        Assert.Equal(jobId, response.JobId);

        Assert.NotNull(capturedRequest);
        Assert.Equal(_defaults.TrainingRootDirectory, capturedRequest!.TrainingRootDirectory);
        Assert.Equal(_defaults.OutputModelDirectory, capturedRequest.OutputModelDirectory);
        Assert.Equal(_defaults.ValidationSplitRatio, capturedRequest.ValidationSplitRatio);
        Assert.Equal(_defaults.LearningRate, capturedRequest.LearningRate);
        Assert.Equal(_defaults.Epochs, capturedRequest.Epochs);
        Assert.Equal(_defaults.BatchSize, capturedRequest.BatchSize);
        Assert.Equal(_defaults.DataAugmentation, capturedRequest.DataAugmentation);
        Assert.Equal(_defaults.DataBalancing, capturedRequest.DataBalancing);
    }

    [Fact]
    public async Task GetStatus_ShouldReturnMappedResponse_WhenJobExists()
    {
        var jobId = Guid.NewGuid();
        var status = new TrainingJobStatus
        {
            JobId = jobId,
            Status = TrainingJobState.Running,
            Progress = 0.5m,
            LearningRate = 0.02m,
            Epochs = 30,
            BatchSize = 12,
            StartTime = DateTimeOffset.UtcNow,
            Remarks = "legacy-endpoint",
            DataAugmentation = _defaults.DataAugmentation,
            DataBalancing = _defaults.DataBalancing
        };

        _trainingJobService
            .Setup(service => service.GetStatusAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        var controller = CreateController();

        var result = await controller.GetStatus(jobId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<TrainingJobResponse>(okResult.Value);

        Assert.Equal(jobId, response.JobId);
        Assert.Equal("运行中", response.State);
        Assert.Equal(status.Progress, response.Progress);
        Assert.Equal(status.Remarks, response.Remarks);
        Assert.Equal(status.DataAugmentation, response.DataAugmentation);
        Assert.Equal(status.DataBalancing, response.DataBalancing);
    }

    [Fact]
    public async Task GetStatus_ShouldReturnNotFound_WhenJobMissing()
    {
        var jobId = Guid.NewGuid();

        _trainingJobService
            .Setup(service => service.GetStatusAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TrainingJobStatus?)null);

        var controller = CreateController();

        var result = await controller.GetStatus(jobId, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var payloadType = notFound.Value!.GetType();
        var errorProperty = payloadType.GetProperty("error");
        Assert.NotNull(errorProperty);
        Assert.Equal("训练任务不存在", (string)errorProperty!.GetValue(notFound.Value)!);
    }

    [Fact]
    public async Task CancelTraining_ShouldReturnNotImplemented()
    {
        var controller = CreateController();
        var jobId = Guid.NewGuid();

        var result = await controller.CancelTraining(jobId);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(501, statusResult.StatusCode);
    }

    private TrainingController CreateController()
    {
        return new TrainingController(
            _trainingJobService.Object,
            _trainingOptions.Object,
            _logger.Object);
    }
}
