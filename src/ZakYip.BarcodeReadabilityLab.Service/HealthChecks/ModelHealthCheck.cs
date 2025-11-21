namespace ZakYip.BarcodeReadabilityLab.Service.HealthChecks;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Models;

/// <summary>
/// 模型健康检查
/// </summary>
public class ModelHealthCheck : IHealthCheck
{
    private readonly BarcodeMlModelOptions _modelOptions;

    public ModelHealthCheck(IOptions<BarcodeMlModelOptions> modelOptions)
    {
        _modelOptions = modelOptions.Value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var modelPath = _modelOptions.CurrentModelPath;

        if (string.IsNullOrWhiteSpace(modelPath))
        {
            return Task.FromResult(
                HealthCheckResult.Degraded(
                    "模型路径未配置",
                    data: new Dictionary<string, object>
                    {
                        ["isModelAvailable"] = false,
                        ["reason"] = "未配置模型路径"
                    }));
        }

        if (!File.Exists(modelPath))
        {
            return Task.FromResult(
                HealthCheckResult.Degraded(
                    $"模型文件不存在：{modelPath}",
                    data: new Dictionary<string, object>
                    {
                        ["isModelAvailable"] = false,
                        ["modelPath"] = modelPath,
                        ["reason"] = "模型文件不存在"
                    }));
        }

        return Task.FromResult(
            HealthCheckResult.Healthy(
                "模型文件可用",
                data: new Dictionary<string, object>
                {
                    ["isModelAvailable"] = true,
                    ["modelPath"] = modelPath
                }));
    }
}
