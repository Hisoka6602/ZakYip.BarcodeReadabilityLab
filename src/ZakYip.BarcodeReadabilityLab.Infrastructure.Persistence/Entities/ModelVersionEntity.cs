namespace ZakYip.BarcodeReadabilityLab.Infrastructure.Persistence.Entities;

using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enums;

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

    public static ModelVersionEntity FromModel(ModelVersion version)
    {
        if (version is null)
            throw new ArgumentNullException(nameof(version));

        var entity = new ModelVersionEntity
        {
            VersionId = version.VersionId,
            VersionName = version.VersionName,
            ModelPath = version.ModelPath,
            TrainingJobId = version.TrainingJobId,
            ParentModelVersionId = version.ParentModelVersionId,
            CreatedAt = version.CreatedAt,
            IsActive = version.IsActive,
            DeploymentSlot = version.DeploymentSlot,
            TrafficPercentage = version.TrafficPercentage,
            Notes = version.Notes
        };

        if (version.EvaluationMetrics is { } metrics)
        {
            entity.Accuracy = metrics.Accuracy;
            entity.MacroPrecision = metrics.MacroPrecision;
            entity.MacroRecall = metrics.MacroRecall;
            entity.MacroF1Score = metrics.MacroF1Score;
            entity.MicroPrecision = metrics.MicroPrecision;
            entity.MicroRecall = metrics.MicroRecall;
            entity.MicroF1Score = metrics.MicroF1Score;
            entity.LogLoss = metrics.LogLoss;
            entity.ConfusionMatrixJson = metrics.ConfusionMatrixJson;
            entity.PerClassMetricsJson = metrics.PerClassMetricsJson;
            entity.DataAugmentationImpactJson = metrics.DataAugmentationImpactJson;
        }
        else
        {
            entity.Accuracy = null;
            entity.MacroPrecision = null;
            entity.MacroRecall = null;
            entity.MacroF1Score = null;
            entity.MicroPrecision = null;
            entity.MicroRecall = null;
            entity.MicroF1Score = null;
            entity.LogLoss = null;
            entity.ConfusionMatrixJson = null;
            entity.PerClassMetricsJson = null;
            entity.DataAugmentationImpactJson = null;
        }

        return entity;
    }

    public ModelVersion ToModel()
    {
        ModelEvaluationMetrics? metrics = null;

        if (Accuracy.HasValue && MacroPrecision.HasValue && MacroRecall.HasValue && MacroF1Score.HasValue &&
            MicroPrecision.HasValue && MicroRecall.HasValue && MicroF1Score.HasValue && ConfusionMatrixJson is { Length: > 0 })
        {
            metrics = new ModelEvaluationMetrics
            {
                Accuracy = Accuracy.Value,
                MacroPrecision = MacroPrecision.Value,
                MacroRecall = MacroRecall.Value,
                MacroF1Score = MacroF1Score.Value,
                MicroPrecision = MicroPrecision.Value,
                MicroRecall = MicroRecall.Value,
                MicroF1Score = MicroF1Score.Value,
                LogLoss = LogLoss,
                ConfusionMatrixJson = ConfusionMatrixJson,
                PerClassMetricsJson = PerClassMetricsJson,
                DataAugmentationImpactJson = DataAugmentationImpactJson
            };
        }

        return new ModelVersion
        {
            VersionId = VersionId,
            VersionName = VersionName,
            ModelPath = ModelPath,
            TrainingJobId = TrainingJobId,
            ParentModelVersionId = ParentModelVersionId,
            CreatedAt = CreatedAt,
            IsActive = IsActive,
            DeploymentSlot = DeploymentSlot,
            TrafficPercentage = TrafficPercentage,
            Notes = Notes,
            EvaluationMetrics = metrics
        };
    }
}
