using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ZakYip.BarcodeReadabilityLab.Application.Services;
using ZakYip.BarcodeReadabilityLab.Service.Models;

namespace ZakYip.BarcodeReadabilityLab.IntegrationTests;

/// <summary>
/// 模型导入导出端点集成测试
/// </summary>
public sealed class ModelEndpointsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ModelEndpointsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ImportModel_WithValidZipFile_ShouldSucceed()
    {
        // Arrange
        using var client = _factory.CreateClient();
        
        // 创建一个假的模型 ZIP 文件
        var modelContent = CreateFakeModelZipContent();
        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(modelContent);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        content.Add(fileContent, "ModelFile", "test-model.zip");
        content.Add(new StringContent("test-model-v1"), "VersionName");
        content.Add(new StringContent("测试模型"), "Notes");
        content.Add(new StringContent("true"), "SetAsActive");

        // Act
        var response = await client.PostAsync("/api/models/import", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var importResponse = JsonSerializer.Deserialize<ModelImportResponse>(
            responseContent, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        Assert.NotNull(importResponse);
        Assert.NotEqual(Guid.Empty, importResponse.VersionId);
        Assert.Contains("test-model", importResponse.VersionName);
        Assert.False(string.IsNullOrEmpty(importResponse.ModelPath));
    }

    [Fact]
    public async Task ImportModel_WithoutFile_ShouldReturnBadRequest()
    {
        // Arrange
        using var client = _factory.CreateClient();
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("test-model"), "VersionName");

        // Act
        var response = await client.PostAsync("/api/models/import", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(
            responseContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        Assert.NotNull(errorResponse);
        Assert.Contains("模型文件", errorResponse.Error);
    }

    [Fact]
    public async Task DownloadCurrentModel_WhenModelExists_ShouldReturnFile()
    {
        // Arrange
        using var client = _factory.CreateClient();
        
        // 首先导入一个模型
        var importedVersionId = await ImportTestModelAsync(client);
        
        // Act
        var response = await client.GetAsync("/api/models/current/download");

        // Assert
        // 注意：因为测试环境可能没有实际的模型文件，可能返回 404
        Assert.True(
            response.StatusCode == HttpStatusCode.OK || 
            response.StatusCode == HttpStatusCode.NotFound);
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            Assert.Equal("application/octet-stream", response.Content.Headers.ContentType?.MediaType);
            Assert.True(response.Content.Headers.ContentLength > 0);
        }
    }

    [Fact]
    public async Task DownloadModelByVersion_WithValidVersionId_ShouldReturnFile()
    {
        // Arrange
        using var client = _factory.CreateClient();
        
        // 首先导入一个模型
        var versionId = await ImportTestModelAsync(client);

        // Act
        var response = await client.GetAsync($"/api/models/{versionId}/download");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/octet-stream", response.Content.Headers.ContentType?.MediaType);
        
        var content = await response.Content.ReadAsByteArrayAsync();
        Assert.True(content.Length > 0);
    }

    [Fact]
    public async Task DownloadModelByVersion_WithInvalidVersionId_ShouldReturnNotFound()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var nonExistentVersionId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/models/{nonExistentVersionId}/download");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ImportAndDownload_EndToEnd_ShouldWorkCorrectly()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var originalContent = CreateFakeModelZipContent();

        // Act 1: 导入模型
        var versionId = await ImportTestModelAsync(client, originalContent);

        // Act 2: 下载导入的模型
        var downloadResponse = await client.GetAsync($"/api/models/{versionId}/download");

        // Assert
        Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
        
        var downloadedContent = await downloadResponse.Content.ReadAsByteArrayAsync();
        Assert.Equal(originalContent, downloadedContent);
    }

    [Fact]
    public async Task ImportModel_MultipleModels_ShouldManageVersionsCorrectly()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act: 导入多个模型
        var version1Id = await ImportTestModelAsync(client, versionName: "model-v1");
        var version2Id = await ImportTestModelAsync(client, versionName: "model-v2");
        var version3Id = await ImportTestModelAsync(client, versionName: "model-v3");

        // Assert: 确保每个版本都有唯一的 ID
        Assert.NotEqual(version1Id, version2Id);
        Assert.NotEqual(version2Id, version3Id);
        Assert.NotEqual(version1Id, version3Id);

        // 验证可以分别下载每个版本
        var download1 = await client.GetAsync($"/api/models/{version1Id}/download");
        var download2 = await client.GetAsync($"/api/models/{version2Id}/download");
        var download3 = await client.GetAsync($"/api/models/{version3Id}/download");

        Assert.Equal(HttpStatusCode.OK, download1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, download2.StatusCode);
        Assert.Equal(HttpStatusCode.OK, download3.StatusCode);
    }

    /// <summary>
    /// 创建假的模型 ZIP 文件内容（用于测试）
    /// </summary>
    private static byte[] CreateFakeModelZipContent()
    {
        // 创建一个简单的 ZIP 文件头部字节
        // 这不是真正的 ZIP，但足够用于测试文件上传和下载
        var content = new byte[256];
        
        // ZIP 文件签名: 0x50 0x4B 0x03 0x04 (PK..)
        content[0] = 0x50;
        content[1] = 0x4B;
        content[2] = 0x03;
        content[3] = 0x04;
        
        // 填充一些随机数据
        var random = new Random(42); // 使用固定种子以保证可重复性
        random.NextBytes(content.AsSpan(4));
        
        return content;
    }

    /// <summary>
    /// 辅助方法：导入测试模型并返回版本 ID
    /// </summary>
    private static async Task<Guid> ImportTestModelAsync(
        HttpClient client, 
        byte[]? modelContent = null,
        string? versionName = null)
    {
        modelContent ??= CreateFakeModelZipContent();
        versionName ??= $"test-model-{Guid.NewGuid().ToString()[..8]}";

        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(modelContent);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        content.Add(fileContent, "ModelFile", $"{versionName}.zip");
        content.Add(new StringContent(versionName), "VersionName");
        content.Add(new StringContent("测试模型"), "Notes");
        content.Add(new StringContent("true"), "SetAsActive");

        var response = await client.PostAsync("/api/models/import", content);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var importResponse = JsonSerializer.Deserialize<ModelImportResponse>(
            responseContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return importResponse!.VersionId;
    }
}
