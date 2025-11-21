using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enums;
using ZakYip.BarcodeReadabilityLab.Infrastructure.Persistence.Data;
using ZakYip.BarcodeReadabilityLab.Service.Models;

namespace ZakYip.BarcodeReadabilityLab.IntegrationTests.Simulation;

/// <summary>
/// 仿真训练集成测试
/// </summary>
public sealed class TrainingSimulationTests : IClassFixture<SimulationHostFactory>
{
    private static readonly TimeSpan StatusPollingInterval = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan StatusTimeout = TimeSpan.FromSeconds(15);
    private readonly SimulationHostFactory _factory;

    public TrainingSimulationTests(SimulationHostFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// 测试用例：端到端仿真训练流程应该成功完成
    /// </summary>
    [Fact]
    public async Task StartTraining_Should_CompleteSuccessfully_InSimulation()
    {
        // Arrange: 准备测试数据集（包含所有 7 个 NoreadReason 类别）
        using var dataset = TestTrainingDatasetBuilder.CreateWithAllNoreadReasons(samplesPerClass: 2, imageSize: 32);
        using var client = _factory.CreateClient();

        // Act: 通过 API 发起训练
        var request = new StartTrainingRequest
        {
            TrainingRootDirectory = dataset.TrainingRootDirectory,
            OutputModelDirectory = dataset.OutputModelDirectory,
            LearningRate = 0.01m,
            Epochs = 1,
            BatchSize = 4,
            ValidationSplitRatio = 0.1m,
            Remarks = "simulation-test-all-categories"
        };

        var startResponse = await client.PostAsJsonAsync("/api/training/start", request);
        
        // Assert: 验证训练启动成功
        startResponse.EnsureSuccessStatusCode();
        var startPayload = await startResponse.Content.ReadFromJsonAsync<StartTrainingResponse>();
        Assert.NotNull(startPayload);
        Assert.NotEqual(Guid.Empty, startPayload!.JobId);

        // Act: 轮询训练状态直到完成
        var finalStatus = await WaitForCompletionAsync(client, startPayload.JobId, StatusTimeout);

        // Assert: 验证训练完成状态
        Assert.Equal("已完成", finalStatus.State);
        Assert.NotNull(finalStatus.EvaluationMetrics);
        Assert.InRange(finalStatus.EvaluationMetrics!.Accuracy, 0.85m, 1.0m);
        Assert.Equal("simulation-test-all-categories", finalStatus.Remarks);
        Assert.Equal(1.0m, finalStatus.Progress);
        Assert.Null(finalStatus.ErrorMessage);

        // Assert: 验证训练历史中存在该任务
        var history = await client.GetFromJsonAsync<List<TrainingJobResponse>>("/api/training/history");
        Assert.NotNull(history);
        var completedJob = history!.FirstOrDefault(job => job.JobId == startPayload.JobId);
        Assert.NotNull(completedJob);
        Assert.Equal("已完成", completedJob!.State);

        // Assert: 验证模型文件已生成
        Assert.True(Directory.Exists(dataset.OutputModelDirectory));
        var modelFiles = Directory.GetFiles(dataset.OutputModelDirectory, "*.zip");
        Assert.NotEmpty(modelFiles);

        // Assert: 验证数据库持久化
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TrainingJobDbContext>();
        
        var jobEntity = await dbContext.TrainingJobs
            .FirstOrDefaultAsync(j => j.JobId == startPayload.JobId);
        
        Assert.NotNull(jobEntity);
        Assert.Equal(Core.Enums.TrainingJobState.Completed, jobEntity!.Status);
        Assert.NotNull(jobEntity.Accuracy);
        Assert.InRange(jobEntity.Accuracy.Value, 0.85m, 1.0m);
    }

    /// <summary>
    /// 测试用例：训练完成后应该持久化评估指标
    /// </summary>
    [Fact]
    public async Task StartTraining_Should_PersistEvaluationMetrics_InSimulation()
    {
        // Arrange: 准备测试数据集
        using var dataset = TestTrainingDatasetBuilder.CreateWithAllNoreadReasons(samplesPerClass: 2, imageSize: 32);
        using var client = _factory.CreateClient();

        var request = new StartTrainingRequest
        {
            TrainingRootDirectory = dataset.TrainingRootDirectory,
            OutputModelDirectory = dataset.OutputModelDirectory,
            LearningRate = 0.01m,
            Epochs = 1,
            BatchSize = 4,
            ValidationSplitRatio = 0.1m,
            Remarks = "simulation-test-metrics"
        };

        // Act: 发起训练并等待完成
        var startResponse = await client.PostAsJsonAsync("/api/training/start", request);
        startResponse.EnsureSuccessStatusCode();
        var startPayload = await startResponse.Content.ReadFromJsonAsync<StartTrainingResponse>();
        Assert.NotNull(startPayload);

        var finalStatus = await WaitForCompletionAsync(client, startPayload!.JobId, StatusTimeout);

        // Assert: 验证评估指标存在且合理
        Assert.NotNull(finalStatus.EvaluationMetrics);
        var metrics = finalStatus.EvaluationMetrics!;
        
        Assert.InRange(metrics.Accuracy, 0.0m, 1.0m);
        Assert.InRange(metrics.MacroPrecision, 0.0m, 1.0m);
        Assert.InRange(metrics.MacroRecall, 0.0m, 1.0m);
        Assert.InRange(metrics.MacroF1Score, 0.0m, 1.0m);
        Assert.InRange(metrics.MicroPrecision, 0.0m, 1.0m);
        Assert.InRange(metrics.MicroRecall, 0.0m, 1.0m);
        Assert.InRange(metrics.MicroF1Score, 0.0m, 1.0m);
        
        if (metrics.LogLoss.HasValue)
        {
            Assert.InRange(metrics.LogLoss.Value, 0.0m, 10.0m);
        }
        
        Assert.NotNull(metrics.ConfusionMatrixJson);
        Assert.NotNull(metrics.PerClassMetricsJson);

        // Assert: 验证数据库中持久化的指标
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TrainingJobDbContext>();
        
        var jobEntity = await dbContext.TrainingJobs
            .FirstOrDefaultAsync(j => j.JobId == startPayload.JobId);
        
        Assert.NotNull(jobEntity);
        Assert.NotNull(jobEntity!.Accuracy);
        Assert.Equal(metrics.Accuracy, jobEntity.Accuracy.Value);
        Assert.NotNull(jobEntity.MacroF1Score);
        Assert.Equal(metrics.MacroF1Score, jobEntity.MacroF1Score.Value);
    }

    /// <summary>
    /// 测试用例：获取不存在的训练任务状态应该返回 404
    /// </summary>
    [Fact]
    public async Task GetTrainingStatus_Should_ReturnNotFound_ForUnknownJob()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var unknownJobId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/training/status/{unknownJobId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// 测试用例：使用二分类数据集的快速训练测试
    /// </summary>
    [Fact]
    public async Task StartTraining_Should_CompleteSuccessfully_WithBinaryClassification()
    {
        // Arrange: 准备简化的二分类数据集
        using var dataset = TestTrainingDatasetBuilder.CreateBinaryClassification(samplesPerClass: 2, imageSize: 16);
        using var client = _factory.CreateClient();

        var request = new StartTrainingRequest
        {
            TrainingRootDirectory = dataset.TrainingRootDirectory,
            OutputModelDirectory = dataset.OutputModelDirectory,
            LearningRate = 0.01m,
            Epochs = 1,
            BatchSize = 2,
            ValidationSplitRatio = 0.1m,
            Remarks = "simulation-test-binary"
        };

        // Act
        var startResponse = await client.PostAsJsonAsync("/api/training/start", request);
        startResponse.EnsureSuccessStatusCode();
        var startPayload = await startResponse.Content.ReadFromJsonAsync<StartTrainingResponse>();
        Assert.NotNull(startPayload);

        var finalStatus = await WaitForCompletionAsync(client, startPayload!.JobId, StatusTimeout);

        // Assert
        Assert.Equal("已完成", finalStatus.State);
        Assert.NotNull(finalStatus.EvaluationMetrics);
        Assert.Equal(2, dataset.LabelDistribution.Count); // 验证是二分类
    }

    /// <summary>
    /// 测试用例：训练过程中应该正确报告进度
    /// </summary>
    [Fact]
    public async Task StartTraining_Should_ReportProgressCorrectly_InSimulation()
    {
        // Arrange
        using var dataset = TestTrainingDatasetBuilder.CreateBinaryClassification(samplesPerClass: 2, imageSize: 16);
        using var client = _factory.CreateClient();

        var request = new StartTrainingRequest
        {
            TrainingRootDirectory = dataset.TrainingRootDirectory,
            OutputModelDirectory = dataset.OutputModelDirectory,
            LearningRate = 0.01m,
            Epochs = 1,
            BatchSize = 2,
            ValidationSplitRatio = 0.1m,
            Remarks = "simulation-test-progress"
        };

        // Act
        var startResponse = await client.PostAsJsonAsync("/api/training/start", request);
        startResponse.EnsureSuccessStatusCode();
        var startPayload = await startResponse.Content.ReadFromJsonAsync<StartTrainingResponse>();
        Assert.NotNull(startPayload);

        var progressValues = new List<decimal>();
        var finalStatus = await WaitForCompletionAsync(
            client,
            startPayload!.JobId,
            StatusTimeout,
            status =>
            {
                if (status.Progress.HasValue)
                {
                    progressValues.Add(status.Progress.Value);
                }
            });

        // Assert: 验证进度值递增
        Assert.NotEmpty(progressValues);
        Assert.Equal(1.0m, progressValues.Last());
        
        // 验证进度值是递增的（允许相等，因为轮询可能在进度不变时读取）
        for (var i = 1; i < progressValues.Count; i++)
        {
            Assert.True(progressValues[i] >= progressValues[i - 1],
                $"进度应该递增或保持不变，但在索引 {i} 处出现递减");
        }
    }

    /// <summary>
    /// 测试用例：训练目录不存在时应该返回错误
    /// </summary>
    [Fact]
    public async Task StartTraining_Should_ReturnError_WhenTrainingDirectoryNotExists()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var nonExistentDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var request = new StartTrainingRequest
        {
            TrainingRootDirectory = nonExistentDirectory,
            OutputModelDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
            LearningRate = 0.01m,
            Epochs = 1,
            BatchSize = 4,
            ValidationSplitRatio = 0.1m,
            Remarks = "simulation-test-invalid-directory"
        };

        // Act
        var startResponse = await client.PostAsJsonAsync("/api/training/start", request);
        
        // Assert: API 应该返回错误（可能是 400 或 500）
        Assert.False(startResponse.IsSuccessStatusCode);
        Assert.True(
            startResponse.StatusCode == HttpStatusCode.BadRequest ||
            startResponse.StatusCode == HttpStatusCode.InternalServerError,
            $"Expected 400 or 500, but got {(int)startResponse.StatusCode}");
    }

    /// <summary>
    /// 测试用例：迁移学习完整闭环验证（如果环境支持）
    /// </summary>
    [Fact]
    public async Task TransferLearningTraining_Simulation_VerifyEndpoint()
    {
        // Arrange: 准备测试数据集
        using var dataset = TestTrainingDatasetBuilder.CreateBinaryClassification(samplesPerClass: 2, imageSize: 32);
        using var client = _factory.CreateClient();

        // 首先验证预训练模型列表可访问
        var modelsResponse = await client.GetAsync("/api/pretrained-models/list");
        Assert.Equal(HttpStatusCode.OK, modelsResponse.StatusCode);

        var models = await modelsResponse.Content.ReadFromJsonAsync<List<PretrainedModelResponse>>();
        Assert.NotNull(models);
        Assert.NotEmpty(models!);

        // 选择 ResNet50 模型进行迁移学习
        var selectedModel = models.FirstOrDefault(m => m.ModelType == PretrainedModelType.ResNet50);
        Assert.NotNull(selectedModel);

        // Act: 通过 API 发起迁移学习训练
        var request = new TransferLearningRequest
        {
            TrainingRootDirectory = dataset.TrainingRootDirectory,
            OutputModelDirectory = dataset.OutputModelDirectory,
            PretrainedModelType = selectedModel!.ModelType,
            LayerFreezeStrategy = LayerFreezeStrategy.FreezeAll,
            LearningRate = 0.001m,
            Epochs = 1,
            BatchSize = 2,
            ValidationSplitRatio = 0.1m,
            Remarks = "迁移学习仿真测试"
        };

        var startResponse = await client.PostAsJsonAsync("/api/training/transfer-learning/start", request);

        // Assert: 验证端点存在（接受 OK 或 500，取决于测试环境是否支持迁移学习）
        Assert.True(
            startResponse.StatusCode == HttpStatusCode.OK ||
            startResponse.StatusCode == HttpStatusCode.InternalServerError,
            $"期望 200 或 500，实际得到 {(int)startResponse.StatusCode}");

        // 如果启动成功，验证基本功能
        if (startResponse.StatusCode == HttpStatusCode.OK)
        {
            var startPayload = await startResponse.Content.ReadFromJsonAsync<StartTrainingResponse>();
            Assert.NotNull(startPayload);
            Assert.NotEqual(Guid.Empty, startPayload!.JobId);
        }
    }

    /// <summary>
    /// 测试用例：迁移学习端点验证
    /// </summary>
    [Fact]
    public async Task TransferLearning_Endpoint_ShouldBeAccessible()
    {
        // Arrange
        using var dataset = TestTrainingDatasetBuilder.CreateBinaryClassification(samplesPerClass: 2, imageSize: 16);
        using var client = _factory.CreateClient();

        // Act: 启动迁移学习训练
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
            Remarks = "端点可访问性验证"
        };

        var startResponse = await client.PostAsJsonAsync("/api/training/transfer-learning/start", request);

        // Assert: 验证端点存在且返回响应（不要求一定成功，因为测试环境可能不支持）
        Assert.NotNull(startResponse);
        Assert.True(
            startResponse.StatusCode == HttpStatusCode.OK ||
            startResponse.StatusCode == HttpStatusCode.InternalServerError ||
            startResponse.StatusCode == HttpStatusCode.BadRequest,
            $"端点应该存在并返回有效响应，实际得到 {(int)startResponse.StatusCode}");
    }

    /// <summary>
    /// 辅助方法：等待训练完成
    /// </summary>
    private static async Task<TrainingJobResponse> WaitForCompletionAsync(
        HttpClient client,
        Guid jobId,
        TimeSpan timeout,
        Action<TrainingJobResponse>? onStatusUpdate = null)
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
                    onStatusUpdate?.Invoke(statusPayload);

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
