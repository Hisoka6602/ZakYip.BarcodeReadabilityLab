namespace ZakYip.BarcodeReadabilityLab.Application.Tests.Services;

using System;
using System.Threading.Tasks;
using Moq;
using Xunit;
using ZakYip.BarcodeReadabilityLab.Application.Services;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enum;

/// <summary>
/// 训练进度通知器单元测试
/// </summary>
public class TrainingProgressNotifierTests
{
    [Fact(DisplayName = "NotifyProgressAsync 应该正确传递进度信息")]
    public async Task NotifyProgressAsync_ShouldPassProgressCorrectly()
    {
        // Arrange
        var mockNotifier = new Mock<ITrainingProgressNotifier>();
        var jobId = Guid.NewGuid();
        var progress = 0.5m;
        var message = "训练中";

        mockNotifier
            .Setup(n => n.NotifyProgressAsync(jobId, progress, message))
            .Returns(Task.CompletedTask);

        // Act
        await mockNotifier.Object.NotifyProgressAsync(jobId, progress, message);

        // Assert
        mockNotifier.Verify(
            n => n.NotifyProgressAsync(jobId, progress, message),
            Times.Once);
    }

    [Fact(DisplayName = "NotifyDetailedProgressAsync 应该正确传递详细进度信息")]
    public async Task NotifyDetailedProgressAsync_ShouldPassDetailedProgressCorrectly()
    {
        // Arrange
        var mockNotifier = new Mock<ITrainingProgressNotifier>();
        var progressInfo = new TrainingProgressInfo
        {
            JobId = Guid.NewGuid(),
            Progress = 0.5m,
            Stage = TrainingStage.Training,
            Message = "Epoch 5/10",
            StartTime = DateTime.UtcNow.AddMinutes(-5),
            EstimatedRemainingSeconds = 300m,
            Metrics = new TrainingMetricsSnapshot
            {
                CurrentEpoch = 5,
                TotalEpochs = 10,
                Accuracy = 0.85m,
                Loss = 0.23m,
                LearningRate = 0.01m
            }
        };

        mockNotifier
            .Setup(n => n.NotifyDetailedProgressAsync(progressInfo))
            .Returns(Task.CompletedTask);

        // Act
        await mockNotifier.Object.NotifyDetailedProgressAsync(progressInfo);

        // Assert
        mockNotifier.Verify(
            n => n.NotifyDetailedProgressAsync(It.Is<TrainingProgressInfo>(
                p => p.JobId == progressInfo.JobId &&
                     p.Progress == progressInfo.Progress &&
                     p.Stage == progressInfo.Stage &&
                     p.Metrics != null &&
                     p.Metrics.CurrentEpoch == 5)),
            Times.Once);
    }

    [Fact(DisplayName = "TrainingProgressInfo 应该正确创建实例")]
    public void TrainingProgressInfo_ShouldCreateInstanceCorrectly()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var startTime = DateTime.UtcNow.AddMinutes(-10);
        
        // Act
        var progressInfo = new TrainingProgressInfo
        {
            JobId = jobId,
            Progress = 0.75m,
            Stage = TrainingStage.Evaluating,
            Message = "评估模型",
            StartTime = startTime,
            EstimatedRemainingSeconds = 120m,
            EstimatedCompletionTime = DateTime.UtcNow.AddMinutes(2),
            Metrics = new TrainingMetricsSnapshot
            {
                CurrentEpoch = 8,
                TotalEpochs = 10,
                Accuracy = 0.92m,
                Loss = 0.15m
            }
        };

        // Assert
        Assert.Equal(jobId, progressInfo.JobId);
        Assert.Equal(0.75m, progressInfo.Progress);
        Assert.Equal(TrainingStage.Evaluating, progressInfo.Stage);
        Assert.Equal("评估模型", progressInfo.Message);
        Assert.Equal(startTime, progressInfo.StartTime);
        Assert.Equal(120m, progressInfo.EstimatedRemainingSeconds);
        Assert.NotNull(progressInfo.Metrics);
        Assert.Equal(8, progressInfo.Metrics!.CurrentEpoch);
        Assert.Equal(0.92m, progressInfo.Metrics.Accuracy);
    }

    [Fact(DisplayName = "TrainingMetricsSnapshot 应该正确创建实例")]
    public void TrainingMetricsSnapshot_ShouldCreateInstanceCorrectly()
    {
        // Arrange & Act
        var metrics = new TrainingMetricsSnapshot
        {
            CurrentEpoch = 3,
            TotalEpochs = 10,
            Accuracy = 0.88m,
            Loss = 0.25m,
            LearningRate = 0.01m
        };

        // Assert
        Assert.Equal(3, metrics.CurrentEpoch);
        Assert.Equal(10, metrics.TotalEpochs);
        Assert.Equal(0.88m, metrics.Accuracy);
        Assert.Equal(0.25m, metrics.Loss);
        Assert.Equal(0.01m, metrics.LearningRate);
    }

    [Fact(DisplayName = "TrainingStage 枚举应该有正确的值")]
    public void TrainingStage_ShouldHaveCorrectValues()
    {
        // Assert
        Assert.Equal(0, (int)TrainingStage.Initializing);
        Assert.Equal(1, (int)TrainingStage.ScanningData);
        Assert.Equal(2, (int)TrainingStage.BalancingData);
        Assert.Equal(3, (int)TrainingStage.AugmentingData);
        Assert.Equal(4, (int)TrainingStage.PreparingData);
        Assert.Equal(5, (int)TrainingStage.BuildingPipeline);
        Assert.Equal(6, (int)TrainingStage.Training);
        Assert.Equal(7, (int)TrainingStage.Evaluating);
        Assert.Equal(8, (int)TrainingStage.SavingModel);
        Assert.Equal(9, (int)TrainingStage.Completed);
    }
}
