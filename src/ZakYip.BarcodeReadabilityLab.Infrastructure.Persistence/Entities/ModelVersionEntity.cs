namespace ZakYip.BarcodeReadabilityLab.Infrastructure.Persistence.Entities;

using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Infrastructure.Persistence.Mappers;

/// <summary>
/// 模型版本持久化实体
/// </summary>
public sealed class ModelVersionEntity
{
    public Guid VersionId { get; set; }

    public string VersionName { get; set; } = string.Empty;

    public string ModelPath { get; set; } = string.Empty;

    public Guid? TrainingJobId { get; set; }

    public Guid? ParentModelVersionId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public bool IsActive { get; set; }

    public string DeploymentSlot { get; set; } = "Production";

    public decimal? TrafficPercentage { get; set; }

    public string? Notes { get; set; }

    public decimal? Accuracy { get; set; }

    public decimal? MacroPrecision { get; set; }

    public decimal? MacroRecall { get; set; }

    public decimal? MacroF1Score { get; set; }

    public decimal? MicroPrecision { get; set; }

    public decimal? MicroRecall { get; set; }

    public decimal? MicroF1Score { get; set; }

    public decimal? LogLoss { get; set; }

    public string? ConfusionMatrixJson { get; set; }

    public string? PerClassMetricsJson { get; set; }

    public string? DataAugmentationImpactJson { get; set; }

    /// <summary>
    /// 从领域模型创建实体（委托给 ModelVersionMapper）
    /// </summary>
    public static ModelVersionEntity FromModel(ModelVersion version) => ModelVersionMapper.ToEntity(version);

    /// <summary>
    /// 转换为领域模型（委托给 ModelVersionMapper）
    /// </summary>
    public ModelVersion ToModel() => ModelVersionMapper.ToModel(this);
}
