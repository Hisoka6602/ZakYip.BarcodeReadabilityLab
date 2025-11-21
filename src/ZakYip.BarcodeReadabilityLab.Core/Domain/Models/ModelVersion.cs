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

    /// <summary>
    /// 激活当前模型版本
    /// </summary>
    /// <returns>激活后的模型版本实例</returns>
    public ModelVersion Activate()
    {
        return this with
        {
            IsActive = true
        };
    }

    /// <summary>
    /// 停用当前模型版本
    /// </summary>
    /// <returns>停用后的模型版本实例</returns>
    public ModelVersion Deactivate()
    {
        return this with
        {
            IsActive = false
        };
    }

    /// <summary>
    /// 更新流量权重
    /// </summary>
    /// <param name="trafficPercentage">新的流量权重（0.0 到 1.0 之间）</param>
    /// <returns>更新流量权重后的模型版本实例</returns>
    public ModelVersion UpdateTrafficPercentage(decimal trafficPercentage)
    {
        if (trafficPercentage < 0.0m || trafficPercentage > 1.0m)
            throw new ArgumentOutOfRangeException(nameof(trafficPercentage), "流量权重必须在 0.0 到 1.0 之间");

        return this with
        {
            TrafficPercentage = trafficPercentage
        };
    }

    /// <summary>
    /// 更新备注信息
    /// </summary>
    /// <param name="notes">新的备注信息</param>
    /// <returns>更新备注后的模型版本实例</returns>
    public ModelVersion UpdateNotes(string? notes)
    {
        return this with
        {
            Notes = notes
        };
    }
}
