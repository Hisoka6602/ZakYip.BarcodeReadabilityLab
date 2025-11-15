namespace ZakYip.BarcodeReadabilityLab.Service.Models;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

/// <summary>
/// 模型导入请求
/// </summary>
public sealed class ModelImportRequest
{
    /// <summary>
    /// 上传的模型文件（通常为 .zip）
    /// </summary>
    [Required]
    public IFormFile? ModelFile { get; init; }

    /// <summary>
    /// 自定义模型版本名称
    /// </summary>
    public string? VersionName { get; init; }

    /// <summary>
    /// 部署槽位，默认 Production
    /// </summary>
    public string? DeploymentSlot { get; init; }

    /// <summary>
    /// 该模型在部署槽位中的流量权重
    /// </summary>
    public decimal? TrafficPercentage { get; init; }

    /// <summary>
    /// 模型备注说明
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// 是否将导入模型立即设为激活版本
    /// </summary>
    public bool SetAsActive { get; init; } = true;
}
