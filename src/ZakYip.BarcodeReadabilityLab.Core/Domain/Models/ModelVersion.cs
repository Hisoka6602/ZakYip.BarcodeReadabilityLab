using ZakYip.BarcodeReadabilityLab.Core.Enums;

namespace ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

using System;

/// <summary>
/// 已训练模型的版本元数据
/// </summary>
public record class ModelVersion
{
    /// <summary>
    /// 模型版本标识
    /// </summary>
    public required Guid VersionId { get; init; }

    /// <summary>
    /// 模型版本名称或标签
    /// </summary>
    public required string VersionName { get; init; }

    /// <summary>
    /// 模型文件的持久化路径
    /// </summary>
    public required string ModelPath { get; init; }

    /// <summary>
    /// 关联的训练任务编号
    /// </summary>
    public Guid? TrainingJobId { get; init; }

    /// <summary>
    /// 父模型版本 ID（用于构建模型版本树/链）
    /// </summary>
    public Guid? ParentModelVersionId { get; init; }

    /// <summary>
    /// 模型版本创建时间
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// 是否为指定部署槽位的当前激活版本
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// 部署槽位（例如 Production、Canary、ExperimentA 等）
    /// </summary>
    public required string DeploymentSlot { get; init; }

    /// <summary>
    /// 流量权重（用于 A/B 测试流量分配），取值范围 0~1
    /// </summary>
    public decimal? TrafficPercentage { get; init; }

    /// <summary>
    /// 版本备注信息
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// 模型评估指标快照
    /// </summary>
    public ModelEvaluationMetrics? EvaluationMetrics { get; init; }
}
