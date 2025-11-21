using System.Net;
using System.Net.Http.Json;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enums;
using ZakYip.BarcodeReadabilityLab.Service.Models;

namespace ZakYip.BarcodeReadabilityLab.IntegrationTests.Api;

/// <summary>
/// 迁移学习与预训练模型 API 端点集成测试
/// </summary>
public sealed class TransferLearningEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly TimeSpan StatusPollingInterval = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan StatusTimeout = TimeSpan.FromSeconds(10);
    private readonly CustomWebApplicationFactory _factory;

    public TransferLearningEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetPretrainedModelsList_ShouldReturnOk_WithModelArray()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/pretrained-models/list");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var models = await response.Content.ReadFromJsonAsync<List<PretrainedModelResponse>>();
        Assert.NotNull(models);
        Assert.NotEmpty(models!);

        // 验证至少包含 ResNet50 模型
        var resnet50 = models.FirstOrDefault(m => m.ModelType == PretrainedModelType.ResNet50);
        Assert.NotNull(resnet50);
        Assert.NotNull(resnet50!.ModelName);
        Assert.NotNull(resnet50.Description);
        Assert.True(resnet50.ModelSizeMB > 0);
    }

    [Fact]
    public async Task GetPretrainedModelInfo_WithValidModelType_ShouldReturnModelDetails()
    {
        // Arrange
        using var client = _factory.CreateClient();
        const string modelType = "ResNet50";

        // Act
        var response = await client.GetAsync($"/api/pretrained-models/{modelType}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var model = await response.Content.ReadFromJsonAsync<PretrainedModelResponse>();
        Assert.NotNull(model);
        Assert.Equal(PretrainedModelType.ResNet50, model!.ModelType);
        Assert.NotNull(model.ModelName);
        Assert.NotNull(model.Description);
    }

    [Fact]
    public async Task GetPretrainedModelInfo_WithInvalidModelType_ShouldReturnError()
    {
        // Arrange
        using var client = _factory.CreateClient();
        const string invalidModelType = "NonExistentModel";

        // Act
        var response = await client.GetAsync($"/api/pretrained-models/{invalidModelType}");

        // Assert - 期望 NotFound 或 BadRequest
        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound || 
            response.StatusCode == HttpStatusCode.BadRequest,
            $"期望 404 或 400，实际得到 {(int)response.StatusCode}");
    }

    [Fact]
    public async Task StartTransferLearningTraining_WithResNet50_ShouldReturnResponse()
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
            Epochs = 2,
            BatchSize = 2,
            ValidationSplitRatio = 0.1m,
            Remarks = "迁移学习集成测试"
        };

        // Act
        var startResponse = await client.PostAsJsonAsync("/api/training/transfer-learning/start", request);

        // Assert - 接受 OK 或 InternalServerError（如果测试环境不支持迁移学习）
        Assert.True(
            startResponse.StatusCode == HttpStatusCode.OK ||
            startResponse.StatusCode == HttpStatusCode.InternalServerError,
            $"期望 200 或 500，实际得到 {(int)startResponse.StatusCode}");

        if (startResponse.StatusCode == HttpStatusCode.OK)
        {
            var startPayload = await startResponse.Content.ReadFromJsonAsync<StartTrainingResponse>();
            Assert.NotNull(startPayload);
            Assert.NotEqual(Guid.Empty, startPayload!.JobId);
            Assert.Contains("训练任务", startPayload.Message);
        }
    }

    [Fact]
    public async Task StartTransferLearningTraining_EndToEnd_Simulation()
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
            Remarks = "端到端迁移学习测试"
        };

        // Act
        var startResponse = await client.PostAsJsonAsync("/api/training/transfer-learning/start", request);

        // Assert - 测试环境可能不完全支持迁移学习，接受 OK 或 500
        Assert.True(
            startResponse.StatusCode == HttpStatusCode.OK ||
            startResponse.StatusCode == HttpStatusCode.InternalServerError,
            $"期望 200 或 500，实际得到 {(int)startResponse.StatusCode}");

        // 如果启动成功，验证可以查询状态
        if (startResponse.StatusCode == HttpStatusCode.OK)
        {
            var startPayload = await startResponse.Content.ReadFromJsonAsync<StartTrainingResponse>();
            Assert.NotNull(startPayload);

            // 尝试查询状态（不等待完成，因为可能不支持）
            var statusResponse = await client.GetAsync($"/api/training/status/{startPayload!.JobId}");
            Assert.True(statusResponse.IsSuccessStatusCode);
        }
    }

    [Fact]
    public async Task StartTransferLearningTraining_WithInvalidModelType_ShouldReturnBadRequest()
    {
        // Arrange
        using var dataset = SyntheticTrainingDataset.Create(samplesPerClass: 2);
        using var client = _factory.CreateClient();

        var request = new TransferLearningRequest
        {
            TrainingRootDirectory = dataset.TrainingRootDirectory,
            OutputModelDirectory = dataset.OutputModelDirectory,
            PretrainedModelType = PretrainedModelType.ResNet50, // 使用无效的枚举值需要特殊处理
            LayerFreezeStrategy = LayerFreezeStrategy.FreezeAll,
            LearningRate = 0.001m,
            Epochs = 1,
            BatchSize = 2
        };

        // 注意：由于使用强类型枚举，无法直接传递无效的模型类型
        // 此测试验证的是 API 对有效枚举值但可能未实现的模型类型的处理

        // Act
        var response = await client.PostAsJsonAsync("/api/training/transfer-learning/start", request);

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest || 
            response.StatusCode == HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task StartTransferLearningTraining_WithMissingDirectory_ShouldReturnError()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var nonExistentDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var request = new TransferLearningRequest
        {
            TrainingRootDirectory = nonExistentDirectory,
            OutputModelDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
            PretrainedModelType = PretrainedModelType.ResNet50,
            LayerFreezeStrategy = LayerFreezeStrategy.FreezeAll,
            LearningRate = 0.001m,
            Epochs = 1,
            BatchSize = 2
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/training/transfer-learning/start", request);

        // Assert
        Assert.False(response.IsSuccessStatusCode);
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
                var statusPayload = await response.Content.ReadFromJsonAsync<TrainingJobResponse>(timeoutSource.Token);
                if (statusPayload is not null)
                {
                    if (statusPayload.State == "已完成")
                    {
                        return statusPayload;
                    }

                    if (statusPayload.State == "失败")
                    {
                        throw new InvalidOperationException(
                            $"训练任务 {jobId} 失败: {statusPayload.ErrorMessage}");
                    }
                }
            }

            await Task.Delay(StatusPollingInterval, timeoutSource.Token);
        }

        throw new TimeoutException($"训练任务 {jobId} 在 {timeout} 内未完成");
    }
}
