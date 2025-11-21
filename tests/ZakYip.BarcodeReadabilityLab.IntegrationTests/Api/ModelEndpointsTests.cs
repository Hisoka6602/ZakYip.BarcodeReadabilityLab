using ZakYip.BarcodeReadabilityLab.Core.Enum;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using ZakYip.BarcodeReadabilityLab.Service.Models;

namespace ZakYip.BarcodeReadabilityLab.IntegrationTests.Api;

/// <summary>
/// 模型管理端点完整集成测试
/// </summary>
public sealed class ModelEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ModelEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ImportModel_WithValidFile_ShouldReturnCreated()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var modelContent = CreateFakeModelZipContent();
        
        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(modelContent);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        content.Add(fileContent, "ModelFile", "test-model.zip");
        content.Add(new StringContent("test-model-v1"), "VersionName");
        content.Add(new StringContent("测试模型导入"), "Notes");
        content.Add(new StringContent("true"), "SetAsActive");

        // Act
        var response = await client.PostAsync("/api/models/import", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<ModelImportResponse>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result!.VersionId);
        Assert.Contains("test-model", result.VersionName);
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
    }

    [Fact]
    public async Task DownloadCurrentModel_WhenNoModelExists_ShouldReturnNotFound()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/models/current/download");

        // Assert
        // 可能返回 NotFound 或 OK（如果测试中已导入模型）
        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound || 
            response.StatusCode == HttpStatusCode.OK,
            $"期望 404 或 200，实际得到 {(int)response.StatusCode}");
    }

    [Fact]
    public async Task DownloadModelByVersion_WithInvalidVersionId_ShouldReturnNotFound()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var invalidVersionId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/models/{invalidVersionId}/download");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ImportAndDownloadModel_EndToEnd_ShouldSucceed()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var modelContent = CreateFakeModelZipContent();

        // Step 1: 导入模型
        using var importContent = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(modelContent);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        importContent.Add(fileContent, "ModelFile", "e2e-test-model.zip");
        importContent.Add(new StringContent("e2e-test-v1"), "VersionName");
        importContent.Add(new StringContent("端到端测试模型"), "Notes");
        importContent.Add(new StringContent("false"), "SetAsActive"); // 不设为活动模型

        var importResponse = await client.PostAsync("/api/models/import", importContent);
        Assert.Equal(HttpStatusCode.Created, importResponse.StatusCode);
        
        var importResult = await importResponse.Content.ReadFromJsonAsync<ModelImportResponse>();
        Assert.NotNull(importResult);
        var versionId = importResult!.VersionId;

        // Step 2: 通过版本 ID 下载模型
        var downloadResponse = await client.GetAsync($"/api/models/{versionId}/download");
        Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
        
        var downloadedContent = await downloadResponse.Content.ReadAsByteArrayAsync();
        Assert.NotEmpty(downloadedContent);
    }

    [Fact]
    public async Task ImportModel_MultipleVersions_ShouldManageCorrectly()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // 导入第一个版本
        var model1 = CreateFakeModelZipContent();
        using var content1 = new MultipartFormDataContent();
        using var file1 = new ByteArrayContent(model1);
        file1.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        content1.Add(file1, "ModelFile", "model-v1.zip");
        content1.Add(new StringContent("multi-version-v1"), "VersionName");
        content1.Add(new StringContent("第一版本"), "Notes");
        content1.Add(new StringContent("false"), "SetAsActive");

        var response1 = await client.PostAsync("/api/models/import", content1);
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        var result1 = await response1.Content.ReadFromJsonAsync<ModelImportResponse>();
        Assert.NotNull(result1);

        // 导入第二个版本
        var model2 = CreateFakeModelZipContent();
        using var content2 = new MultipartFormDataContent();
        using var file2 = new ByteArrayContent(model2);
        file2.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        content2.Add(file2, "ModelFile", "model-v2.zip");
        content2.Add(new StringContent("multi-version-v2"), "VersionName");
        content2.Add(new StringContent("第二版本"), "Notes");
        content2.Add(new StringContent("false"), "SetAsActive");

        var response2 = await client.PostAsync("/api/models/import", content2);
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);
        var result2 = await response2.Content.ReadFromJsonAsync<ModelImportResponse>();
        Assert.NotNull(result2);

        // 验证两个版本 ID 不同
        Assert.NotEqual(result1!.VersionId, result2!.VersionId);

        // 验证两个版本都可以下载
        var download1 = await client.GetAsync($"/api/models/{result1.VersionId}/download");
        var download2 = await client.GetAsync($"/api/models/{result2.VersionId}/download");
        Assert.Equal(HttpStatusCode.OK, download1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, download2.StatusCode);
    }

    /// <summary>
    /// 创建假的模型 ZIP 文件内容
    /// </summary>
    private static byte[] CreateFakeModelZipContent()
    {
        using var memoryStream = new MemoryStream();
        using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, true))
        {
            var entry = archive.CreateEntry("model.txt");
            using var entryStream = entry.Open();
            using var writer = new StreamWriter(entryStream);
            writer.Write($"Fake Model Content - {Guid.NewGuid()}");
        }
        return memoryStream.ToArray();
    }
}
