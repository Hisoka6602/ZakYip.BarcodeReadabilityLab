using ZakYip.BarcodeReadabilityLab.Application.Options;
using ZakYip.BarcodeReadabilityLab.Application.Services;

namespace ZakYip.BarcodeReadabilityLab.Application.Tests;

/// <summary>
/// 应用程序选项和服务类测试
/// </summary>
public sealed class ApplicationOptionsTests
{
    [Fact]
    public void BarcodeAnalyzerOptions_ShouldCreateInstance_WithRequiredProperties()
    {
        // Arrange
        var watchDir = "/path/to/watch";
        var unresolvedDir = "/path/to/unresolved";

        // Act
        var options = new BarcodeAnalyzerOptions
        {
            WatchDirectory = watchDir,
            UnresolvedDirectory = unresolvedDir
        };

        // Assert
        Assert.Equal(watchDir, options.WatchDirectory);
        Assert.Equal(unresolvedDir, options.UnresolvedDirectory);
        Assert.Equal(0.90m, options.ConfidenceThreshold);
        Assert.False(options.IsRecursive);
    }

    [Fact]
    public void BarcodeAnalyzerOptions_ShouldCreateInstance_WithAllProperties()
    {
        // Arrange
        var watchDir = "/path/to/watch";
        var unresolvedDir = "/path/to/unresolved";
        var threshold = 0.85m;

        // Act
        var options = new BarcodeAnalyzerOptions
        {
            WatchDirectory = watchDir,
            UnresolvedDirectory = unresolvedDir,
            ConfidenceThreshold = threshold,
            IsRecursive = true
        };

        // Assert
        Assert.Equal(watchDir, options.WatchDirectory);
        Assert.Equal(unresolvedDir, options.UnresolvedDirectory);
        Assert.Equal(threshold, options.ConfidenceThreshold);
        Assert.True(options.IsRecursive);
    }

    [Fact]
    public void BarcodeAnalyzerOptions_RecordEquality_ShouldWork_ForSameValues()
    {
        // Arrange
        var options1 = new BarcodeAnalyzerOptions
        {
            WatchDirectory = "/path/to/watch",
            UnresolvedDirectory = "/path/to/unresolved",
            ConfidenceThreshold = 0.85m,
            IsRecursive = true
        };

        var options2 = new BarcodeAnalyzerOptions
        {
            WatchDirectory = "/path/to/watch",
            UnresolvedDirectory = "/path/to/unresolved",
            ConfidenceThreshold = 0.85m,
            IsRecursive = true
        };

        // Act & Assert
        Assert.Equal(options1, options2);
    }

    [Fact]
    public void ResourceUsageSnapshot_ShouldCreateInstance()
    {
        // Arrange
        var cpuUsage = 45.5m;
        var usedMemory = 2_000_000_000L;
        var totalMemory = 8_000_000_000L;
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var snapshot = new ResourceUsageSnapshot
        {
            CpuUsagePercent = cpuUsage,
            UsedMemoryBytes = usedMemory,
            TotalMemoryBytes = totalMemory,
            Timestamp = timestamp
        };

        // Assert
        Assert.Equal(cpuUsage, snapshot.CpuUsagePercent);
        Assert.Equal(usedMemory, snapshot.UsedMemoryBytes);
        Assert.Equal(totalMemory, snapshot.TotalMemoryBytes);
        Assert.Equal(timestamp, snapshot.Timestamp);
    }

    [Fact]
    public void ResourceUsageSnapshot_MemoryUsagePercent_ShouldCalculateCorrectly()
    {
        // Arrange
        var usedMemory = 2_000_000_000L;
        var totalMemory = 8_000_000_000L;
        var expectedPercent = 25m;

        // Act
        var snapshot = new ResourceUsageSnapshot
        {
            CpuUsagePercent = 50m,
            UsedMemoryBytes = usedMemory,
            TotalMemoryBytes = totalMemory,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Assert
        Assert.Equal(expectedPercent, snapshot.MemoryUsagePercent);
    }

    [Fact]
    public void ResourceUsageSnapshot_MemoryUsagePercent_ShouldReturnZero_WhenTotalIsZero()
    {
        // Arrange & Act
        var snapshot = new ResourceUsageSnapshot
        {
            CpuUsagePercent = 50m,
            UsedMemoryBytes = 1000L,
            TotalMemoryBytes = 0L,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Assert
        Assert.Equal(0m, snapshot.MemoryUsagePercent);
    }

    [Fact]
    public void ResourceUsageSnapshot_RecordEquality_ShouldWork_ForSameValues()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);

        var snapshot1 = new ResourceUsageSnapshot
        {
            CpuUsagePercent = 45.5m,
            UsedMemoryBytes = 2_000_000_000L,
            TotalMemoryBytes = 8_000_000_000L,
            Timestamp = timestamp
        };

        var snapshot2 = new ResourceUsageSnapshot
        {
            CpuUsagePercent = 45.5m,
            UsedMemoryBytes = 2_000_000_000L,
            TotalMemoryBytes = 8_000_000_000L,
            Timestamp = timestamp
        };

        // Act & Assert
        Assert.Equal(snapshot1, snapshot2);
    }

    [Fact]
    public void ResourceUsageSnapshot_RecordEquality_ShouldNotWork_ForDifferentValues()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;

        var snapshot1 = new ResourceUsageSnapshot
        {
            CpuUsagePercent = 45.5m,
            UsedMemoryBytes = 2_000_000_000L,
            TotalMemoryBytes = 8_000_000_000L,
            Timestamp = timestamp
        };

        var snapshot2 = new ResourceUsageSnapshot
        {
            CpuUsagePercent = 50.0m,
            UsedMemoryBytes = 2_000_000_000L,
            TotalMemoryBytes = 8_000_000_000L,
            Timestamp = timestamp
        };

        // Act & Assert
        Assert.NotEqual(snapshot1, snapshot2);
    }

    [Fact]
    public void ResourceUsageSnapshot_MemoryUsagePercent_ShouldHandle100Percent()
    {
        // Arrange
        var totalMemory = 8_000_000_000L;

        // Act
        var snapshot = new ResourceUsageSnapshot
        {
            CpuUsagePercent = 100m,
            UsedMemoryBytes = totalMemory,
            TotalMemoryBytes = totalMemory,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Assert
        Assert.Equal(100m, snapshot.MemoryUsagePercent);
    }

    [Fact]
    public void TrainingJobStatus_ShouldCreateInstance_WithRequiredProperties()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var status = TrainingStatus.Running;
        var progress = 0.5m;
        var learningRate = 0.01m;
        var epochs = 50;
        var batchSize = 20;

        // Act
        var jobStatus = new TrainingJobStatus
        {
            JobId = jobId,
            Status = status,
            Progress = progress,
            LearningRate = learningRate,
            Epochs = epochs,
            BatchSize = batchSize
        };

        // Assert
        Assert.Equal(jobId, jobStatus.JobId);
        Assert.Equal(status, jobStatus.Status);
        Assert.Equal(progress, jobStatus.Progress);
        Assert.Equal(learningRate, jobStatus.LearningRate);
        Assert.Equal(epochs, jobStatus.Epochs);
        Assert.Equal(batchSize, jobStatus.BatchSize);
    }

    [Fact]
    public void TrainingRequest_ShouldCreateInstance_WithRequiredProperties()
    {
        // Arrange
        var trainingDir = "/path/to/training";
        var outputDir = "/path/to/output";
        var learningRate = 0.01m;
        var epochs = 50;
        var batchSize = 20;

        // Act
        var request = new TrainingRequest
        {
            TrainingRootDirectory = trainingDir,
            OutputModelDirectory = outputDir,
            LearningRate = learningRate,
            Epochs = epochs,
            BatchSize = batchSize
        };

        // Assert
        Assert.Equal(trainingDir, request.TrainingRootDirectory);
        Assert.Equal(outputDir, request.OutputModelDirectory);
        Assert.Equal(learningRate, request.LearningRate);
        Assert.Equal(epochs, request.Epochs);
        Assert.Equal(batchSize, request.BatchSize);
    }
}
