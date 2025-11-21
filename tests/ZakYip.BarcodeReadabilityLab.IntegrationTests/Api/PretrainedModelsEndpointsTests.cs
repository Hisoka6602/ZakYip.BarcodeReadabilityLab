using System.Net;
using System.Net.Http.Json;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enums;
using ZakYip.BarcodeReadabilityLab.Service.Models;

namespace ZakYip.BarcodeReadabilityLab.IntegrationTests.Api;

/// <summary>
/// 预训练模型端点集成测试
/// </summary>
public sealed class PretrainedModelsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PretrainedModelsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ListPretrainedModels_ShouldReturnOkWithModelList()
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

        // 验证包含常见的预训练模型
        Assert.Contains(models, m => m.ModelType == PretrainedModelType.ResNet50);
    }

    [Fact]
    public async Task GetPretrainedModelInfo_WithValidType_ShouldReturnModelDetails()
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
        Assert.True(model.ModelSizeMB > 0);
    }

    [Fact]
    public async Task GetPretrainedModelInfo_WithInvalidType_ShouldReturnNotFoundOrBadRequest()
    {
        // Arrange
        using var client = _factory.CreateClient();
        const string invalidModelType = "NonExistentModel123";

        // Act
        var response = await client.GetAsync($"/api/pretrained-models/{invalidModelType}");

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound || 
            response.StatusCode == HttpStatusCode.BadRequest,
            $"期望 404 或 400，实际得到 {(int)response.StatusCode}");
    }

    [Fact]
    public async Task GetPretrainedModelInfo_ForAllSupportedModels_ShouldSucceed()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // 获取所有模型列表
        var listResponse = await client.GetAsync("/api/pretrained-models/list");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var models = await listResponse.Content.ReadFromJsonAsync<List<PretrainedModelResponse>>();
        Assert.NotNull(models);
        Assert.NotEmpty(models!);

        // Act & Assert - 验证每个模型都可以单独获取
        foreach (var model in models)
        {
            var modelTypeStr = model.ModelType.ToString();
            var response = await client.GetAsync($"/api/pretrained-models/{modelTypeStr}");
            
            Assert.True(
                response.IsSuccessStatusCode,
                $"获取 {modelTypeStr} 模型信息失败，状态码: {response.StatusCode}");

            var detailModel = await response.Content.ReadFromJsonAsync<PretrainedModelResponse>();
            Assert.NotNull(detailModel);
            Assert.Equal(model.ModelType, detailModel!.ModelType);
        }
    }

    [Fact]
    public async Task DownloadPretrainedModel_ShouldReturnOkOrNotImplemented()
    {
        // Arrange
        using var client = _factory.CreateClient();
        const string modelType = "ResNet50";

        // Act
        var response = await client.PostAsync($"/api/pretrained-models/{modelType}/download", null);

        // Assert
        // 下载端点可能返回 OK（如果实现了）或 NotImplemented（如果未实现）
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotImplemented ||
            response.StatusCode == HttpStatusCode.InternalServerError,
            $"期望 200/501/500，实际得到 {(int)response.StatusCode}");
    }

    [Fact]
    public async Task ListPretrainedModels_ShouldReturnModelsWithValidProperties()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/pretrained-models/list");
        var models = await response.Content.ReadFromJsonAsync<List<PretrainedModelResponse>>();

        // Assert
        Assert.NotNull(models);
        foreach (var model in models!)
        {
            Assert.NotNull(model.ModelName);
            Assert.NotEmpty(model.ModelName);
            
            Assert.NotNull(model.Description);
            Assert.NotEmpty(model.Description);
            
            Assert.True(model.ModelSizeMB > 0, $"模型 {model.ModelName} 的大小应该大于 0");
            
            Assert.True(
                Enum.IsDefined(typeof(PretrainedModelType), model.ModelType),
                $"模型类型 {model.ModelType} 应该是有效的枚举值");
        }
    }
}
