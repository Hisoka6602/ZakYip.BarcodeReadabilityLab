namespace ZakYip.BarcodeReadabilityLab.Service.HealthChecks;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using ZakYip.BarcodeReadabilityLab.Application.Services;

/// <summary>
/// 配置健康检查
/// </summary>
public class ConfigurationHealthCheck : IHealthCheck
{
    private readonly IStartupSelfCheckService _selfCheckService;

    public ConfigurationHealthCheck(IStartupSelfCheckService selfCheckService)
    {
        _selfCheckService = selfCheckService;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var lastCheckResult = _selfCheckService.GetLastCheckResult();

        if (lastCheckResult == null)
        {
            return Task.FromResult(
                HealthCheckResult.Degraded(
                    "配置自检尚未执行",
                    data: new Dictionary<string, object>
                    {
                        ["isConfigValid"] = false,
                        ["message"] = "配置自检尚未执行"
                    }));
        }

        if (lastCheckResult.IsHealthy)
        {
            return Task.FromResult(
                HealthCheckResult.Healthy(
                    "配置检查通过",
                    data: new Dictionary<string, object>
                    {
                        ["isConfigValid"] = true,
                        ["checkedItems"] = lastCheckResult.CheckResults.Count,
                        ["allPassed"] = true
                    }));
        }

        var failedChecks = lastCheckResult.CheckResults
            .Where(r => !r.IsHealthy && !r.IsAutoFixed)
            .Select(r => r.CheckName)
            .ToList();

        if (lastCheckResult.CanRun)
        {
            return Task.FromResult(
                HealthCheckResult.Degraded(
                    $"部分配置检查未通过，服务以降级模式运行",
                    data: new Dictionary<string, object>
                    {
                        ["isConfigValid"] = false,
                        ["canRun"] = true,
                        ["failedChecks"] = failedChecks,
                        ["checkedItems"] = lastCheckResult.CheckResults.Count
                    }));
        }

        return Task.FromResult(
            HealthCheckResult.Unhealthy(
                "配置检查失败，服务不可用",
                data: new Dictionary<string, object>
                {
                    ["isConfigValid"] = false,
                    ["canRun"] = false,
                    ["failedChecks"] = failedChecks
                }));
    }
}
