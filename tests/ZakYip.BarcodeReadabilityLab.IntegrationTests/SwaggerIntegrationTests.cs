namespace ZakYip.BarcodeReadabilityLab.IntegrationTests;

using System.Net;

/// <summary>
/// Swagger/OpenAPI 集成测试
/// </summary>
public sealed class SwaggerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SwaggerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    /// <summary>
    /// 测试 Swagger JSON 端点是否可访问
    /// </summary>
    [Fact]
    public async Task SwaggerJson_ShouldBeAccessible()
    {
        // Act
        var response = await _client.GetAsync("/api-docs/v1/swagger.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
        Assert.Contains("\"openapi\":", content);
        Assert.Contains("条码可读性分析 API", content);
    }

    /// <summary>
    /// 测试 Swagger UI 端点是否可访问
    /// </summary>
    [Fact]
    public async Task SwaggerUI_ShouldBeAccessible()
    {
        // Act
        var response = await _client.GetAsync("/api-docs");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
        Assert.Contains("swagger-ui", content);
    }

    /// <summary>
    /// 测试 Swagger 配置包含所有期望的端点
    /// </summary>
    [Fact]
    public async Task SwaggerJson_ShouldContainAllEndpoints()
    {
        // Act
        var response = await _client.GetAsync("/api-docs/v1/swagger.json");
        var content = await response.Content.ReadAsStringAsync();

        // Assert - 验证训练端点
        Assert.Contains("/api/training/start", content);
        Assert.Contains("/api/training/status/{jobId}", content);
        Assert.Contains("/api/training/history", content);

        // Assert - 验证模型端点
        Assert.Contains("/api/models/current/download", content);
        Assert.Contains("/api/models/{versionId}/download", content);
        Assert.Contains("/api/models/import", content);
    }

    /// <summary>
    /// 测试 Swagger 配置包含 XML 注释
    /// </summary>
    [Fact]
    public async Task SwaggerJson_ShouldContainXmlComments()
    {
        // Act
        var response = await _client.GetAsync("/api-docs/v1/swagger.json");
        var content = await response.Content.ReadAsStringAsync();

        // Assert - 验证 XML 注释存在
        Assert.Contains("启动训练任务", content);
        Assert.Contains("查询训练任务状态", content);
        Assert.Contains("获取训练任务历史", content);
    }
}
