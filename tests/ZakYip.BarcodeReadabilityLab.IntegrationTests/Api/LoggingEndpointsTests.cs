using ZakYip.BarcodeReadabilityLab.Core.Enums;
using System.Net;
using System.Net.Http.Json;
using ZakYip.BarcodeReadabilityLab.Service.Endpoints;

namespace ZakYip.BarcodeReadabilityLab.IntegrationTests.Api;

/// <summary>
/// 日志端点集成测试
/// </summary>
public sealed class LoggingEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public LoggingEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetLogLevel_ShouldReturnCurrentLevel()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/logging/level");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<LogLevelResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result!.Level);
    }

    [Fact]
    public async Task SetLogLevel_WithValidLevel_ShouldSucceed()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var request = new SetLogLevelRequest
        {
            Level = "Debug"
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/logging/level", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // 验证日志级别已更改
        var getResponse = await client.GetAsync("/api/logging/level");
        var result = await getResponse.Content.ReadFromJsonAsync<LogLevelResponse>();
        Assert.NotNull(result);
        Assert.Equal("Debug", result!.Level);
    }

    [Fact]
    public async Task SetLogLevel_WithInvalidLevel_ShouldReturnBadRequest()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var request = new SetLogLevelRequest
        {
            Level = "InvalidLevel"
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/logging/level", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SetLogLevel_ToInformation_ThenToWarning_ShouldSucceed()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act - 设置为 Information
        var infoRequest = new SetLogLevelRequest { Level = "Information" };
        var infoResponse = await client.PutAsJsonAsync("/api/logging/level", infoRequest);
        Assert.Equal(HttpStatusCode.OK, infoResponse.StatusCode);

        // Verify Information level
        var getInfo = await client.GetAsync("/api/logging/level");
        var infoResult = await getInfo.Content.ReadFromJsonAsync<LogLevelResponse>();
        Assert.Equal("Information", infoResult!.Level);

        // Act - 设置为 Warning
        var warnRequest = new SetLogLevelRequest { Level = "Warning" };
        var warnResponse = await client.PutAsJsonAsync("/api/logging/level", warnRequest);
        Assert.Equal(HttpStatusCode.OK, warnResponse.StatusCode);

        // Verify Warning level
        var getWarn = await client.GetAsync("/api/logging/level");
        var warnResult = await getWarn.Content.ReadFromJsonAsync<LogLevelResponse>();
        Assert.Equal("Warning", warnResult!.Level);
    }
}
