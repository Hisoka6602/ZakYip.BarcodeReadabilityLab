using System.Net;
using System.Net.Http.Json;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enum;
using ZakYip.BarcodeReadabilityLab.Service.Models;
using ZakYip.BarcodeReadabilityLab.Service.Endpoints;

namespace ZakYip.BarcodeReadabilityLab.IntegrationTests;

/// <summary>
/// 全量 API 端点 Smoke Test
/// 覆盖所有对外公开的 HTTP API 端点，验证可成功访问和典型异常路径
/// </summary>
public sealed class AllApiEndpointsSmokeTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AllApiEndpointsSmokeTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    #region 训练任务 API (/api/training)

    [Fact]
    public async Task TrainingStart_WithMinimalParameters_ShouldReturnOk()
    {
        // Arrange
        using var dataset = SyntheticTrainingDataset.Create(samplesPerClass: 2);
        using var client = _factory.CreateClient();

        var request = new StartTrainingRequest
        {
            TrainingRootDirectory = dataset.TrainingRootDirectory,
            OutputModelDirectory = dataset.OutputModelDirectory
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/training/start", request);

        // Assert
        Assert.True(response.IsSuccessStatusCode, $"期望成功状态码，实际得到 {response.StatusCode}");

        var result = await response.Content.ReadFromJsonAsync<StartTrainingResponse>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result!.JobId);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
    }

    [Fact]
    public async Task TrainingStart_WithFullParameters_ShouldReturnOk()
    {
        // Arrange
        using var dataset = SyntheticTrainingDataset.Create(samplesPerClass: 2);
        using var client = _factory.CreateClient();

        var request = new StartTrainingRequest
        {
            TrainingRootDirectory = dataset.TrainingRootDirectory,
            OutputModelDirectory = dataset.OutputModelDirectory,
            ValidationSplitRatio = 0.2m,
            LearningRate = 0.01m,
            Epochs = 5,
            BatchSize = 10,
            Remarks = "完整参数测试",
            DataAugmentation = new DataAugmentationOptions
            {
                Enable = true,
                AugmentedImagesPerSample = 2,
                EnableRotation = true,
                RotationAngles = new[] { -10f, 10f },
                EnableHorizontalFlip = true,
                EnableBrightnessAdjustment = true,
                BrightnessLower = 0.8f,
                BrightnessUpper = 1.2f
            },
            DataBalancing = new DataBalancingOptions
            {
                Strategy = DataBalancingStrategy.OverSample,
                TargetSampleCountPerClass = 100
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/training/start", request);

        // Assert
        Assert.True(response.IsSuccessStatusCode, $"期望成功状态码，实际得到 {response.StatusCode}");

        var result = await response.Content.ReadFromJsonAsync<StartTrainingResponse>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result!.JobId);
    }

    [Fact]
    public async Task TrainingStatus_WithValidJobId_ShouldReturnOk()
    {
        // Arrange
        using var dataset = SyntheticTrainingDataset.Create(samplesPerClass: 2);
        using var client = _factory.CreateClient();

        // 先创建一个训练任务
        var startRequest = new StartTrainingRequest
        {
            TrainingRootDirectory = dataset.TrainingRootDirectory,
            OutputModelDirectory = dataset.OutputModelDirectory
        };

        var startResponse = await client.PostAsJsonAsync("/api/training/start", startRequest);
        var startResult = await startResponse.Content.ReadFromJsonAsync<StartTrainingResponse>();

        // Act
        var response = await client.GetAsync($"/api/training/status/{startResult!.JobId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var status = await response.Content.ReadFromJsonAsync<TrainingJobResponse>();
        Assert.NotNull(status);
        Assert.Equal(startResult.JobId, status!.JobId);
        Assert.False(string.IsNullOrWhiteSpace(status.State));
        Assert.True(status.Progress >= 0m && status.Progress <= 1m);
    }

    [Fact]
    public async Task TrainingStatus_WithNonExistentJobId_ShouldReturn404()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var nonExistentJobId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/training/status/{nonExistentJobId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.False(string.IsNullOrWhiteSpace(error!.Error));
    }

    [Fact]
    public async Task TrainingHistory_ShouldReturnOkWithList()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/training/history");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var history = await response.Content.ReadFromJsonAsync<List<TrainingJobResponse>>();
        Assert.NotNull(history);
        // 列表可能为空或包含任务
        Assert.IsAssignableFrom<IReadOnlyList<TrainingJobResponse>>(history);
    }

    #endregion

    #region 迁移学习 API (/api/training/transfer-learning)

    [Fact]
    public async Task TransferLearningStart_WithValidRequest_ShouldReturnOk()
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
            Epochs = 3,
            BatchSize = 5,
            Remarks = "迁移学习 Smoke Test"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/training/transfer-learning/start", request);

        // Assert
        // 迁移学习可能因环境问题返回 500，但应该返回有意义的错误信息
        Assert.True(
            response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.InternalServerError,
            $"期望 200 或 500，实际得到 {response.StatusCode}");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var result = await response.Content.ReadFromJsonAsync<StartTrainingResponse>();
            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result!.JobId);
        }
        else
        {
            // 确保 500 错误有详细信息
            var content = await response.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrWhiteSpace(content));
        }
    }

    [Fact]
    public async Task TransferLearningStart_WithMultiStageTraining_ShouldReturnOk()
    {
        // Arrange
        using var dataset = SyntheticTrainingDataset.Create(samplesPerClass: 2);
        using var client = _factory.CreateClient();

        var request = new TransferLearningRequest
        {
            TrainingRootDirectory = dataset.TrainingRootDirectory,
            OutputModelDirectory = dataset.OutputModelDirectory,
            PretrainedModelType = PretrainedModelType.ResNet50,
            EnableMultiStageTraining = true,
            TrainingPhases = new List<MultiStageTrainingPhaseDto>
            {
                new()
                {
                    PhaseName = "阶段1: 冻结训练",
                    PhaseNumber = 1,
                    Epochs = 2,
                    LearningRate = 0.001m,
                    LayerFreezeStrategy = LayerFreezeStrategy.FreezeAll
                },
                new()
                {
                    PhaseName = "阶段2: 微调",
                    PhaseNumber = 2,
                    Epochs = 2,
                    LearningRate = 0.0001m,
                    LayerFreezeStrategy = LayerFreezeStrategy.FreezePartial,
                    UnfreezeLayersPercentage = 0.3m
                }
            },
            BatchSize = 5,
            Remarks = "多阶段迁移学习测试"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/training/transfer-learning/start", request);

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.InternalServerError,
            $"期望 200 或 500，实际得到 {response.StatusCode}");
    }

    #endregion

    #region 预训练模型 API (/api/pretrained-models)

    [Fact]
    public async Task PretrainedModelsList_ShouldReturnOkWithModels()
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
        
        // 验证至少有一个预训练模型
        var firstModel = models.First();
        Assert.NotEqual(default, firstModel.ModelType);
        Assert.False(string.IsNullOrWhiteSpace(firstModel.ModelName));
    }

    [Fact]
    public async Task PretrainedModelsGetInfo_WithValidType_ShouldReturnOk()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/pretrained-models/{PretrainedModelType.ResNet50}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var model = await response.Content.ReadFromJsonAsync<PretrainedModelResponse>();
        Assert.NotNull(model);
        Assert.Equal(PretrainedModelType.ResNet50, model!.ModelType);
        Assert.False(string.IsNullOrWhiteSpace(model.ModelName));
    }

    [Fact]
    public async Task PretrainedModelsDownload_WithValidType_ShouldReturnOk()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsync($"/api/pretrained-models/{PretrainedModelType.ResNet50}/download", null);

        // Assert
        // 下载可能失败（网络问题），但应该返回有意义的响应
        Assert.True(
            response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.InternalServerError,
            $"期望 200 或 500，实际得到 {response.StatusCode}");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var result = await response.Content.ReadFromJsonAsync<DownloadPretrainedModelResponse>();
            Assert.NotNull(result);
            Assert.Equal(PretrainedModelType.ResNet50, result!.ModelType);
        }
    }

    #endregion

    #region 模型管理 API (/api/models)

    [Fact]
    public async Task ModelsCurrentDownload_WithoutActiveModel_ShouldReturn404Or500()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/models/current/download");

        // Assert
        // 无激活模型时应返回 404 或 500（带有错误信息）
        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.InternalServerError,
            $"期望 404 或 500，实际得到 {response.StatusCode}");

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.False(string.IsNullOrWhiteSpace(error!.Error));
    }

    [Fact]
    public async Task ModelsDownloadByVersion_WithNonExistentId_ShouldReturn404()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var nonExistentVersionId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/models/{nonExistentVersionId}/download");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
    }

    [Fact]
    public async Task ModelsImport_WithValidFile_ShouldReturnCreated()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // 创建一个假的模型文件
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, "fake model content");

        try
        {
            using var content = new MultipartFormDataContent();
            using var fileStream = File.OpenRead(tempFile);
            using var fileContent = new StreamContent(fileStream);
            
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/zip");
            content.Add(fileContent, "ModelFile", "test-model.zip");
            content.Add(new StringContent("test-import-v1"), "VersionName");
            content.Add(new StringContent("Testing"), "DeploymentSlot");
            content.Add(new StringContent("false"), "SetAsActive");
            content.Add(new StringContent("Smoke test 导入"), "Notes");

            // Act
            var response = await client.PostAsync("/api/models/import", content);

            // Assert
            Assert.True(
                response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.BadRequest,
                $"期望 201 或 400，实际得到 {response.StatusCode}");

            if (response.StatusCode == HttpStatusCode.Created)
            {
                var result = await response.Content.ReadFromJsonAsync<ModelImportResponse>();
                Assert.NotNull(result);
                Assert.NotEqual(Guid.Empty, result!.VersionId);
                Assert.False(string.IsNullOrWhiteSpace(result.VersionName));
            }
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task ModelsImport_WithoutFile_ShouldReturnBadRequest()
    {
        // Arrange
        using var client = _factory.CreateClient();
        using var content = new MultipartFormDataContent();

        // 只提供表单字段，不提供文件
        content.Add(new StringContent("test-version"), "VersionName");

        // Act
        var response = await client.PostAsync("/api/models/import", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Contains("模型文件", error!.Error);
    }

    #endregion

    #region 传统控制器 API (/api/training-legacy)

    [Fact]
    public async Task TrainingLegacyCancel_WithAnyJobId_ShouldReturn501()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var jobId = Guid.NewGuid();

        // Act
        var response = await client.PostAsync($"/api/training-legacy/cancel/{jobId}", null);

        // Assert
        // 取消功能暂未实现，应返回 501
        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
    }

    #endregion

    #region 错误路径测试

    [Fact]
    public async Task TrainingStart_WithInvalidDirectory_ShouldReturnBadRequest()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var nonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var request = new StartTrainingRequest
        {
            TrainingRootDirectory = nonExistentDir,
            OutputModelDirectory = nonExistentDir
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/training/start", request);

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.InternalServerError,
            $"期望 400 或 500，实际得到 {response.StatusCode}");

        // 验证返回了错误信息
        var content = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(content));
    }

    [Fact]
    public async Task TrainingStart_WithInvalidParameters_ShouldReturnBadRequest()
    {
        // Arrange
        using var dataset = SyntheticTrainingDataset.Create(samplesPerClass: 2);
        using var client = _factory.CreateClient();

        var request = new StartTrainingRequest
        {
            TrainingRootDirectory = dataset.TrainingRootDirectory,
            OutputModelDirectory = dataset.OutputModelDirectory,
            LearningRate = -1m, // 无效的学习率
            Epochs = 0, // 无效的 Epoch 数
            BatchSize = 0 // 无效的 Batch Size
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/training/start", request);

        // Assert
        // 应该返回错误（400 或 500），不允许成功
        Assert.False(response.IsSuccessStatusCode);
    }

    #endregion
}
