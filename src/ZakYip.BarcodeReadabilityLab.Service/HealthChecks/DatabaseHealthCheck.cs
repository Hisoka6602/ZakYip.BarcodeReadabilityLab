namespace ZakYip.BarcodeReadabilityLab.Service.HealthChecks;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using ZakYip.BarcodeReadabilityLab.Infrastructure.Persistence.Data;

/// <summary>
/// 数据库健康检查
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly TrainingJobDbContext _dbContext;

    public DatabaseHealthCheck(TrainingJobDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

            if (canConnect)
            {
                return HealthCheckResult.Healthy(
                    "数据库连接正常",
                    data: new Dictionary<string, object>
                    {
                        ["isDatabaseOk"] = true
                    });
            }

            return HealthCheckResult.Unhealthy(
                "无法连接到数据库",
                data: new Dictionary<string, object>
                {
                    ["isDatabaseOk"] = false
                });
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"数据库检查失败：{ex.Message}",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    ["isDatabaseOk"] = false,
                    ["error"] = ex.Message
                });
        }
    }
}
