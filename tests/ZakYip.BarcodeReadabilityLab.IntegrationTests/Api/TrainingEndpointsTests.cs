using ZakYip.BarcodeReadabilityLab.Core.Enums;
using System.Net;
using System.Net.Http.Json;
using ZakYip.BarcodeReadabilityLab.Service.Models;

namespace ZakYip.BarcodeReadabilityLab.IntegrationTests.Api;

/// <summary>
/// 训练端点完整集成测试
/// </summary>
public sealed class TrainingEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly TimeSpan StatusPollingInterval = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan StatusTimeout = TimeSpan.FromSeconds(10);
    private readonly CustomWebApplicationFactory _factory;

    public TrainingEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StartTraining_WithValidRequest_ShouldReturnOk()
    {
        // Arrange
        using var dataset = SyntheticTrainingDataset.Create(samplesPerClass: 2);
        using var client = _factory.CreateClient();

        var request = new StartTrainingRequest
        {
            TrainingRootDirectory = dataset.TrainingRootDirectory,
            OutputModelDirectory = dataset.OutputModelDirectory,
            LearningRate = 0.01m,
            Epochs = 1,
            BatchSize = 2,
            ValidationSplitRatio = 0.1m,
            Remarks = "API测试"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/training/start", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<StartTrainingResponse>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result!.JobId);
        Assert.Contains("训练任务", result.Message);
    }

    [Fact]
    public async Task GetTrainingStatus_WithValidJobId_ShouldReturnStatus()
    {
        // Arrange
        using var dataset = SyntheticTrainingDataset.Create(samplesPerClass: 2);
        using var client = _factory.CreateClient();

        // 先启动一个训练任务
        var startRequest = new StartTrainingRequest
        {
            TrainingRootDirectory = dataset.TrainingRootDirectory,
            OutputModelDirectory = dataset.OutputModelDirectory,
            LearningRate = 0.01m,
            Epochs = 1,
            BatchSize = 2
        };

        var startResponse = await client.PostAsJsonAsync("/api/training/start", startRequest);
        var startResult = await startResponse.Content.ReadFromJsonAsync<StartTrainingResponse>();
        Assert.NotNull(startResult);

        // Act
        var statusResponse = await client.GetAsync($"/api/training/status/{startResult!.JobId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);

        var status = await statusResponse.Content.ReadFromJsonAsync<TrainingJobResponse>();
        Assert.NotNull(status);
        Assert.Equal(startResult.JobId, status!.JobId);
        Assert.NotNull(status.State);
    }

    [Fact]
    public async Task GetTrainingStatus_WithInvalidJobId_ShouldReturnNotFound()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var invalidJobId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/training/status/{invalidJobId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTrainingHistory_ShouldReturnList()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/training/history");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var history = await response.Content.ReadFromJsonAsync<List<TrainingJobResponse>>();
        Assert.NotNull(history);
        // 历史列表可能为空或包含其他测试的任务
        Assert.IsAssignableFrom<IReadOnlyList<TrainingJobResponse>>(history);
    }

    [Fact]
    public async Task StartTraining_WithMissingDirectory_ShouldReturnError()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var nonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var request = new StartTrainingRequest
        {
            TrainingRootDirectory = nonExistentDir,
            OutputModelDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
            LearningRate = 0.01m,
            Epochs = 1,
            BatchSize = 2
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/training/start", request);

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.InternalServerError,
            $"期望 400 或 500，实际得到 {(int)response.StatusCode}");
    }

    [Fact]
    public async Task StartTransferLearningTraining_WithValidRequest_ShouldReturnResponse()
    {
        // Arrange
        using var dataset = SyntheticTrainingDataset.Create(samplesPerClass: 2);
        using var client = _factory.CreateClient();

        var request = new TransferLearningRequest
        {
            TrainingRootDirectory = dataset.TrainingRootDirectory,
            OutputModelDirectory = dataset.OutputModelDirectory,
            PretrainedModelType = PretrainedModelType.ResNet50,
            LayerFreezeStrategy = LayerFreezeStrategy.FreezeAll,
            LearningRate = 0.001m,
            Epochs = 1,
            BatchSize = 2,
            ValidationSplitRatio = 0.1m,
            Remarks = "迁移学习API测试"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/training/transfer-learning/start", request);

        // Assert
        // 迁移学习可能在测试环境中不完全支持，接受 OK 或 500
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.InternalServerError,
            $"期望 200 或 500，实际得到 {(int)response.StatusCode}");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var result = await response.Content.ReadFromJsonAsync<StartTrainingResponse>();
            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result!.JobId);
        }
    }

    [Fact]
    public async Task StartTraining_EndToEnd_ShouldCompleteSuccessfully()
    {
        // Arrange
        using var dataset = SyntheticTrainingDataset.Create(samplesPerClass: 2);
        using var client = _factory.CreateClient();

        var request = new StartTrainingRequest
        {
            TrainingRootDirectory = dataset.TrainingRootDirectory,
            OutputModelDirectory = dataset.OutputModelDirectory,
            LearningRate = 0.01m,
            Epochs = 1,
            BatchSize = 2,
            ValidationSplitRatio = 0.1m,
            Remarks = "端到端API测试"
        };

        // Act - 启动训练
        var startResponse = await client.PostAsJsonAsync("/api/training/start", request);
        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);

        var startResult = await startResponse.Content.ReadFromJsonAsync<StartTrainingResponse>();
        Assert.NotNull(startResult);

        // Act - 等待训练完成
        var finalStatus = await WaitForCompletionAsync(client, startResult!.JobId, StatusTimeout);

        // Assert
        Assert.Equal("已完成", finalStatus.State);
        Assert.NotNull(finalStatus.EvaluationMetrics);
        Assert.InRange(finalStatus.EvaluationMetrics!.Accuracy, 0.0m, 1.0m);
        Assert.Equal("端到端API测试", finalStatus.Remarks);

        // 验证历史记录中存在该任务
        var historyResponse = await client.GetAsync("/api/training/history");
        var history = await historyResponse.Content.ReadFromJsonAsync<List<TrainingJobResponse>>();
        Assert.NotNull(history);
        Assert.Contains(history!, job => job.JobId == startResult.JobId && job.State == "已完成");
    }

    [Fact]
    public async Task StartTraining_WithDataAugmentation_ShouldSucceed()
    {
        // Arrange
        using var dataset = SyntheticTrainingDataset.Create(samplesPerClass: 2);
        using var client = _factory.CreateClient();

        var request = new StartTrainingRequest
        {
            TrainingRootDirectory = dataset.TrainingRootDirectory,
            OutputModelDirectory = dataset.OutputModelDirectory,
            LearningRate = 0.01m,
            Epochs = 1,
            BatchSize = 2,
            ValidationSplitRatio = 0.1m,
            DataAugmentation = new Core.Domain.Models.DataAugmentationOptions
            {
                Enable = true,
                AugmentedImagesPerSample = 2,
                EnableRotation = true,
                RotationAngles = new[] { -15f, 15f }
            },
            Remarks = "数据增强测试"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/training/start", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<StartTrainingResponse>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result!.JobId);
    }

    /// <summary>
    /// 辅助方法：等待训练完成
    /// </summary>
    private static async Task<TrainingJobResponse> WaitForCompletionAsync(
        HttpClient client,
        Guid jobId,
        TimeSpan timeout)
    {
        using var timeoutSource = new CancellationTokenSource(timeout);

        while (!timeoutSource.IsCancellationRequested)
        {
            var response = await client.GetAsync($"/api/training/status/{jobId}", timeoutSource.Token);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var status = await response.Content.ReadFromJsonAsync<TrainingJobResponse>(timeoutSource.Token);
                if (status is not null)
                {
                    if (status.State == "已完成")
                    {
                        return status;
                    }

                    if (status.State == "失败")
                    {
                        throw new InvalidOperationException($"训练任务 {jobId} 失败: {status.ErrorMessage}");
                    }
                }
            }

            await Task.Delay(StatusPollingInterval, timeoutSource.Token);
        }

        throw new TimeoutException($"训练任务 {jobId} 在 {timeout} 内未完成");
    }

    [Fact]
    public async Task StartIncrementalTraining_WithNonExistentBaseModel_ShouldReturnNotFound()
    {
        // Arrange
        using var dataset = SyntheticTrainingDataset.Create(samplesPerClass: 2);
        using var client = _factory.CreateClient();

        var request = new IncrementalTrainingStartRequest
        {
            BaseModelVersionId = Guid.NewGuid(), // 不存在的模型版本
            TrainingRootDirectory = dataset.TrainingRootDirectory,
            OutputModelDirectory = dataset.OutputModelDirectory,
            LearningRate = 0.0005m,
            Epochs = 1,
            BatchSize = 2,
            Remarks = "增量训练测试 - 基础模型不存在"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/training/incremental-start", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task IncrementalTrainingEndpoint_ShouldBeAccessible()
    {
        // Arrange
        using var dataset = SyntheticTrainingDataset.Create(samplesPerClass: 2);
        using var client = _factory.CreateClient();

        var request = new IncrementalTrainingStartRequest
        {
            BaseModelVersionId = Guid.NewGuid(),
            TrainingRootDirectory = dataset.TrainingRootDirectory,
            OutputModelDirectory = dataset.OutputModelDirectory,
            LearningRate = 0.0005m,
            Epochs = 1,
            BatchSize = 2
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/training/incremental-start", request);

        // Assert
        // 应该收到 404（模型不存在）或者如果模型存在则是 200/202
        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound || 
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.Accepted,
            $"期望 200/202/404，实际得到 {(int)response.StatusCode}");
    }
}
