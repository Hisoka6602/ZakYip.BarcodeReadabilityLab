namespace ZakYip.BarcodeReadabilityLab.Application.Services;

using System;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enums;

/// <summary>
/// 模型版本注册请求
/// </summary>
public sealed record class ModelVersionRegistration
{
    /// <summary>
    /// 模型版本名称
    /// </summary>
    public required string VersionName { get; init; }

    /// <summary>
    /// 模型文件路径
    /// </summary>
    public required string ModelPath { get; init; }

    /// <summary>
    /// 关联训练任务
    /// </summary>
    public Guid? TrainingJobId { get; init; }

    /// <summary>
    /// 父模型版本 ID（用于增量训练时建立模型版本谱系）
    /// </summary>
    public Guid? ParentModelVersionId { get; init; }

    /// <summary>
    /// 指定部署槽位（默认 Production）
    /// </summary>
    public string DeploymentSlot { get; init; } = "Production";

    /// <summary>
    /// A/B 测试流量占比（0~1）
    /// </summary>
    public decimal? TrafficPercentage { get; init; }

    /// <summary>
    /// 版本备注
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// 评估指标快照
    /// </summary>
    public ModelEvaluationMetrics? EvaluationMetrics { get; init; }

    /// <summary>
    /// 注册时间（默认当前时间）
    /// </summary>
    public DateTimeOffset? CreatedAt { get; init; }

    /// <summary>
    /// 是否立即设为激活版本
    /// </summary>
    public bool SetAsActive { get; init; }
}
