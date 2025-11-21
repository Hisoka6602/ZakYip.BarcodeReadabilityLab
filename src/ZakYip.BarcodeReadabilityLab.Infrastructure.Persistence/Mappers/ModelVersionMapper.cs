namespace ZakYip.BarcodeReadabilityLab.Infrastructure.Persistence.Mappers;

using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Infrastructure.Persistence.Entities;

/// <summary>
/// 模型版本领域模型与实体之间的映射器
/// </summary>
internal static class ModelVersionMapper
{
    /// <summary>
    /// 将实体转换为领域模型
    /// </summary>
    /// <param name="entity">模型版本实体</param>
    /// <returns>模型版本领域模型</returns>
    public static ModelVersion ToModel(ModelVersionEntity entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        ModelEvaluationMetrics? metrics = null;

        if (entity.Accuracy.HasValue && entity.MacroPrecision.HasValue && entity.MacroRecall.HasValue && 
            entity.MacroF1Score.HasValue && entity.MicroPrecision.HasValue && entity.MicroRecall.HasValue && 
            entity.MicroF1Score.HasValue && entity.ConfusionMatrixJson is { Length: > 0 })
        {
            metrics = new ModelEvaluationMetrics
            {
                Accuracy = entity.Accuracy.Value,
                MacroPrecision = entity.MacroPrecision.Value,
                MacroRecall = entity.MacroRecall.Value,
                MacroF1Score = entity.MacroF1Score.Value,
                MicroPrecision = entity.MicroPrecision.Value,
                MicroRecall = entity.MicroRecall.Value,
                MicroF1Score = entity.MicroF1Score.Value,
                LogLoss = entity.LogLoss,
                ConfusionMatrixJson = entity.ConfusionMatrixJson,
                PerClassMetricsJson = entity.PerClassMetricsJson,
                DataAugmentationImpactJson = entity.DataAugmentationImpactJson
            };
        }

        return new ModelVersion
        {
            VersionId = entity.VersionId,
            VersionName = entity.VersionName,
            ModelPath = entity.ModelPath,
            TrainingJobId = entity.TrainingJobId,
            ParentModelVersionId = entity.ParentModelVersionId,
            CreatedAt = entity.CreatedAt,
            IsActive = entity.IsActive,
            DeploymentSlot = entity.DeploymentSlot,
            TrafficPercentage = entity.TrafficPercentage,
            Notes = entity.Notes,
            EvaluationMetrics = metrics
        };
    }

    /// <summary>
    /// 从领域模型创建实体
    /// </summary>
    /// <param name="model">模型版本领域模型</param>
    /// <returns>模型版本实体</returns>
    public static ModelVersionEntity ToEntity(ModelVersion model)
    {
        if (model is null)
            throw new ArgumentNullException(nameof(model));

        var entity = new ModelVersionEntity
        {
            VersionId = model.VersionId,
            VersionName = model.VersionName,
            ModelPath = model.ModelPath,
            TrainingJobId = model.TrainingJobId,
            ParentModelVersionId = model.ParentModelVersionId,
            CreatedAt = model.CreatedAt,
            IsActive = model.IsActive,
            DeploymentSlot = model.DeploymentSlot,
            TrafficPercentage = model.TrafficPercentage,
            Notes = model.Notes
        };

        if (model.EvaluationMetrics is { } metrics)
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

    /// <summary>
    /// 更新实体的激活状态字段
    /// </summary>
    /// <param name="entity">要更新的实体</param>
    /// <param name="isActive">新的激活状态</param>
    public static void UpdateEntityActivationStatus(ModelVersionEntity entity, bool isActive)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        entity.IsActive = isActive;
    }

    /// <summary>
    /// 更新实体的流量权重
    /// </summary>
    /// <param name="entity">要更新的实体</param>
    /// <param name="trafficPercentage">新的流量权重</param>
    public static void UpdateEntityTrafficPercentage(ModelVersionEntity entity, decimal? trafficPercentage)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        entity.TrafficPercentage = trafficPercentage;
    }
}
