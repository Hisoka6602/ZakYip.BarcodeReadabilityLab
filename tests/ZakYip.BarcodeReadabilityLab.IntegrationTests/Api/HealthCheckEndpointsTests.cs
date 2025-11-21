namespace ZakYip.BarcodeReadabilityLab.IntegrationTests.Api;

using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.Json;
using Xunit;

/// <summary>
/// 健康检查端点集成测试
/// </summary>
public class HealthCheckEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthCheckEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact(DisplayName = "GET /health 应返回健康状态")]
    public async Task HealthEndpoint_ShouldReturnHealthStatus()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.ServiceUnavailable, 
            "健康检查端点应返回成功或服务不可用状态");
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(content), "响应内容不应为空");

        // 验证返回的是有效的 JSON
        var json = JsonSerializer.Deserialize<JsonElement>(content);
        Assert.True(json.TryGetProperty("status", out _), "响应应包含 status 字段");
    }

    [Fact(DisplayName = "GET /ready 应返回就绪状态")]
    public async Task ReadyEndpoint_ShouldReturnReadinessStatus()
    {
        // Act
        var response = await _client.GetAsync("/ready");

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.ServiceUnavailable,
            "就绪检查端点应返回成功或服务不可用状态");
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(content), "响应内容不应为空");

        // 验证返回的是有效的 JSON
        var json = JsonSerializer.Deserialize<JsonElement>(content);
        Assert.True(json.TryGetProperty("status", out _), "响应应包含 status 字段");
        Assert.True(json.TryGetProperty("checks", out _), "响应应包含 checks 字段");
    }

    [Fact(DisplayName = "GET /live 应返回存活状态")]
    public async Task LiveEndpoint_ShouldReturnLivenessStatus()
    {
        // Act
        var response = await _client.GetAsync("/live");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(content), "响应内容不应为空");

        // 验证返回的是有效的 JSON
        var json = JsonSerializer.Deserialize<JsonElement>(content);
        Assert.True(json.TryGetProperty("status", out var status), "响应应包含 status 字段");
        Assert.Equal("Healthy", status.GetString());
    }

    [Fact(DisplayName = "健康检查端点应返回有效的 JSON 格式")]
    public async Task HealthEndpoints_ShouldReturnValidJsonFormat()
    {
        // Arrange
        var endpoints = new[] { "/health", "/ready", "/live" };

        foreach (var endpoint in endpoints)
        {
            // Act
            var response = await _client.GetAsync(endpoint);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            var exception = Record.Exception(() => JsonSerializer.Deserialize<JsonElement>(content));
            Assert.Null(exception);
        }
    }

    [Fact(DisplayName = "/health 应包含配置、数据库和模型检查")]
    public async Task HealthEndpoint_ShouldIncludeAllChecks()
    {
        // Act
        var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content);

        // Assert
        if (json.TryGetProperty("checks", out var checks))
        {
            var checkNames = new List<string>();
            foreach (var check in checks.EnumerateArray())
            {
                if (check.TryGetProperty("name", out var name))
                {
                    checkNames.Add(name.GetString() ?? "");
                }
            }

            // 至少应该有配置、数据库和模型三个检查项
            Assert.Contains("configuration", checkNames);
            Assert.Contains("database", checkNames);
            Assert.Contains("model", checkNames);
        }
    }
}
