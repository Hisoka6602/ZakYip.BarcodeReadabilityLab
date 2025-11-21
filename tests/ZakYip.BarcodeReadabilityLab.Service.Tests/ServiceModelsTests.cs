using ZakYip.BarcodeReadabilityLab.Service.Models;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enum;

namespace ZakYip.BarcodeReadabilityLab.Service.Tests;

/// <summary>
/// 服务模型测试
/// </summary>
public sealed class ServiceModelsTests
{
    [Fact]
    public void ErrorResponse_ShouldCreateInstance()
    {
        // Arrange
        var errorMessage = "发生错误";

        // Act
        var response = new ErrorResponse { Error = errorMessage };

        // Assert
        Assert.Equal(errorMessage, response.Error);
    }

    [Fact]
    public void StartTrainingResponse_ShouldCreateInstance_WithRequiredProperties()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var message = "训练任务已创建";

        // Act
        var response = new StartTrainingResponse
        {
            JobId = jobId,
            Message = message
        };

        // Assert
        Assert.Equal(jobId, response.JobId);
        Assert.Equal(message, response.Message);
    }

    [Fact]
    public void ModelImportResponse_ShouldCreateInstance()
    {
        // Arrange
        var versionId = Guid.NewGuid();
        var versionName = "v1.0.0";
        var modelPath = "/path/to/model.zip";
        var isActive = true;

        // Act
        var response = new ModelImportResponse
        {
            VersionId = versionId,
            VersionName = versionName,
            ModelPath = modelPath,
            IsActive = isActive
        };

        // Assert
        Assert.Equal(versionId, response.VersionId);
        Assert.Equal(versionName, response.VersionName);
        Assert.Equal(modelPath, response.ModelPath);
        Assert.True(response.IsActive);
    }

    [Fact]
    public void TrainingJobResponse_ShouldCreateInstance_WithMinimalProperties()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var state = "运行中";
        var progress = 0.5m;
        var message = "正在训练";

        // Act
        var response = new TrainingJobResponse
        {
            JobId = jobId,
            State = state,
            Progress = progress,
            Message = message
        };

        // Assert
        Assert.Equal(jobId, response.JobId);
        Assert.Equal(state, response.State);
        Assert.Equal(progress, response.Progress);
        Assert.Equal(message, response.Message);
    }

    [Fact]
    public void TrainingJobResponse_ShouldCreateInstance_WithAllProperties()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var state = "已完成";
        var progress = 1.0m;
        var message = "训练完成";
        var startTime = DateTimeOffset.UtcNow.AddHours(-1);
        var completedTime = DateTimeOffset.UtcNow;
        var learningRate = 0.01m;
        var epochs = 50;
        var batchSize = 20;
        var remarks = "测试训练";

        var metrics = new ModelEvaluationMetrics
        {
            Accuracy = 0.95m,
            MacroPrecision = 0.94m,
            MacroRecall = 0.93m,
            MacroF1Score = 0.935m,
            MicroPrecision = 0.95m,
            MicroRecall = 0.95m,
            MicroF1Score = 0.95m,
            ConfusionMatrixJson = "[[10, 1], [0, 12]]"
        };

        var dataAugmentation = new DataAugmentationOptions
        {
            Enable = true,
            AugmentedImagesPerSample = 2
        };

        var dataBalancing = new DataBalancingOptions
        {
            Strategy = DataBalancingStrategy.OverSample
        };

        // Act
        var response = new TrainingJobResponse
        {
            JobId = jobId,
            State = state,
            Progress = progress,
            Message = message,
            StartTime = startTime,
            CompletedTime = completedTime,
            LearningRate = learningRate,
            Epochs = epochs,
            BatchSize = batchSize,
            Remarks = remarks,
            EvaluationMetrics = metrics,
            DataAugmentation = dataAugmentation,
            DataBalancing = dataBalancing
        };

        // Assert
        Assert.Equal(jobId, response.JobId);
        Assert.Equal(state, response.State);
        Assert.Equal(progress, response.Progress);
        Assert.Equal(message, response.Message);
        Assert.Equal(startTime, response.StartTime);
        Assert.Equal(completedTime, response.CompletedTime);
        Assert.Equal(learningRate, response.LearningRate);
        Assert.Equal(epochs, response.Epochs);
        Assert.Equal(batchSize, response.BatchSize);
        Assert.Equal(remarks, response.Remarks);
        Assert.NotNull(response.EvaluationMetrics);
        Assert.Equal(0.95m, response.EvaluationMetrics!.Accuracy);
        Assert.NotNull(response.DataAugmentation);
        Assert.True(response.DataAugmentation!.Enable);
        Assert.NotNull(response.DataBalancing);
        Assert.Equal(DataBalancingStrategy.OverSample, response.DataBalancing!.Strategy);
    }

    [Fact]
    public void StartTrainingRequest_ShouldCreateInstance_WithDefaults()
    {
        // Act
        var request = new StartTrainingRequest();

        // Assert
        Assert.Null(request.TrainingRootDirectory);
        Assert.Null(request.OutputModelDirectory);
        Assert.Null(request.ValidationSplitRatio);
        Assert.Null(request.LearningRate);
        Assert.Null(request.Epochs);
        Assert.Null(request.BatchSize);
        Assert.Null(request.Remarks);
        Assert.Null(request.DataAugmentation);
        Assert.Null(request.DataBalancing);
    }

    [Fact]
    public void StartTrainingRequest_ShouldCreateInstance_WithAllProperties()
    {
        // Arrange
        var trainingDir = "/path/to/training";
        var outputDir = "/path/to/output";
        var validationRatio = 0.2m;
        var learningRate = 0.01m;
        var epochs = 50;
        var batchSize = 20;
        var remarks = "测试训练";

        var dataAugmentation = new DataAugmentationOptions
        {
            Enable = true,
            AugmentedImagesPerSample = 2
        };

        var dataBalancing = new DataBalancingOptions
        {
            Strategy = DataBalancingStrategy.UnderSample
        };

        // Act
        var request = new StartTrainingRequest
        {
            TrainingRootDirectory = trainingDir,
            OutputModelDirectory = outputDir,
            ValidationSplitRatio = validationRatio,
            LearningRate = learningRate,
            Epochs = epochs,
            BatchSize = batchSize,
            Remarks = remarks,
            DataAugmentation = dataAugmentation,
            DataBalancing = dataBalancing
        };

        // Assert
        Assert.Equal(trainingDir, request.TrainingRootDirectory);
        Assert.Equal(outputDir, request.OutputModelDirectory);
        Assert.Equal(validationRatio, request.ValidationSplitRatio);
        Assert.Equal(learningRate, request.LearningRate);
        Assert.Equal(epochs, request.Epochs);
        Assert.Equal(batchSize, request.BatchSize);
        Assert.Equal(remarks, request.Remarks);
        Assert.NotNull(request.DataAugmentation);
        Assert.True(request.DataAugmentation!.Enable);
        Assert.NotNull(request.DataBalancing);
        Assert.Equal(DataBalancingStrategy.UnderSample, request.DataBalancing!.Strategy);
    }
}
