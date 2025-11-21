namespace ZakYip.BarcodeReadabilityLab.Infrastructure.Persistence.Mappers;

using System.Text.Json;
using System.Text.Json.Serialization;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Infrastructure.Persistence.Entities;

/// <summary>
/// 训练任务领域模型与实体之间的映射器
/// </summary>
internal static class TrainingJobMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// 将实体转换为领域模型
    /// </summary>
    /// <param name="entity">训练任务实体</param>
    /// <returns>训练任务领域模型</returns>
    public static TrainingJob ToModel(TrainingJobEntity entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        ModelEvaluationMetrics? evaluationMetrics = null;

        // 如果有评估指标数据，构建评估指标对象
        if (entity.Accuracy.HasValue && entity.MacroPrecision.HasValue && entity.MacroRecall.HasValue &&
            entity.MacroF1Score.HasValue && entity.MicroPrecision.HasValue && entity.MicroRecall.HasValue &&
            entity.MicroF1Score.HasValue && !string.IsNullOrWhiteSpace(entity.ConfusionMatrixJson))
        {
            evaluationMetrics = new ModelEvaluationMetrics
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

        var augmentationOptions = !string.IsNullOrWhiteSpace(entity.DataAugmentationOptionsJson)
            ? JsonSerializer.Deserialize<DataAugmentationOptions>(entity.DataAugmentationOptionsJson, JsonOptions) ?? new DataAugmentationOptions()
            : new DataAugmentationOptions();

        var balancingOptions = !string.IsNullOrWhiteSpace(entity.DataBalancingOptionsJson)
            ? JsonSerializer.Deserialize<DataBalancingOptions>(entity.DataBalancingOptionsJson, JsonOptions) ?? new DataBalancingOptions()
            : new DataBalancingOptions();

        return new TrainingJob
        {
            JobId = entity.JobId,
            JobType = entity.JobType,
            BaseModelVersionId = entity.BaseModelVersionId,
            ParentTrainingJobId = entity.ParentTrainingJobId,
            TrainingRootDirectory = entity.TrainingRootDirectory,
            OutputModelDirectory = entity.OutputModelDirectory,
            ValidationSplitRatio = entity.ValidationSplitRatio,
            LearningRate = entity.LearningRate,
            Epochs = entity.Epochs,
            BatchSize = entity.BatchSize,
            Status = entity.Status,
            Progress = entity.Progress,
            StartTime = entity.StartTime,
            CompletedTime = entity.CompletedTime,
            ErrorMessage = entity.ErrorMessage,
            Remarks = entity.Remarks,
            DataAugmentation = augmentationOptions,
            DataBalancing = balancingOptions,
            EvaluationMetrics = evaluationMetrics
        };
    }

    /// <summary>
    /// 从领域模型创建实体
    /// </summary>
    /// <param name="model">训练任务领域模型</param>
    /// <returns>训练任务实体</returns>
    public static TrainingJobEntity ToEntity(TrainingJob model)
    {
        if (model is null)
            throw new ArgumentNullException(nameof(model));

        return new TrainingJobEntity
        {
            JobId = model.JobId,
            JobType = model.JobType,
            BaseModelVersionId = model.BaseModelVersionId,
            ParentTrainingJobId = model.ParentTrainingJobId,
            TrainingRootDirectory = model.TrainingRootDirectory,
            OutputModelDirectory = model.OutputModelDirectory,
            ValidationSplitRatio = model.ValidationSplitRatio,
            LearningRate = model.LearningRate,
            Epochs = model.Epochs,
            BatchSize = model.BatchSize,
            Status = model.Status,
            Progress = model.Progress,
            StartTime = model.StartTime,
            CompletedTime = model.CompletedTime,
            ErrorMessage = model.ErrorMessage,
            Remarks = model.Remarks,
            Accuracy = model.EvaluationMetrics?.Accuracy,
            MacroPrecision = model.EvaluationMetrics?.MacroPrecision,
            MacroRecall = model.EvaluationMetrics?.MacroRecall,
            MacroF1Score = model.EvaluationMetrics?.MacroF1Score,
            MicroPrecision = model.EvaluationMetrics?.MicroPrecision,
            MicroRecall = model.EvaluationMetrics?.MicroRecall,
            MicroF1Score = model.EvaluationMetrics?.MicroF1Score,
            LogLoss = model.EvaluationMetrics?.LogLoss,
            ConfusionMatrixJson = model.EvaluationMetrics?.ConfusionMatrixJson,
            PerClassMetricsJson = model.EvaluationMetrics?.PerClassMetricsJson,
            DataAugmentationImpactJson = model.EvaluationMetrics?.DataAugmentationImpactJson,
            DataAugmentationOptionsJson = JsonSerializer.Serialize(model.DataAugmentation, JsonOptions),
            DataBalancingOptionsJson = JsonSerializer.Serialize(model.DataBalancing, JsonOptions)
        };
    }

    /// <summary>
    /// 更新实体的状态和进度字段
    /// </summary>
    /// <param name="entity">要更新的实体</param>
    /// <param name="model">包含最新状态的领域模型</param>
    public static void UpdateEntityStatus(TrainingJobEntity entity, TrainingJob model)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));
        
        if (model is null)
            throw new ArgumentNullException(nameof(model));

        entity.Status = model.Status;
        entity.Progress = model.Progress;
        entity.CompletedTime = model.CompletedTime;
        entity.ErrorMessage = model.ErrorMessage;
        
        // 更新评估指标（如果有）
        if (model.EvaluationMetrics is not null)
        {
            entity.Accuracy = model.EvaluationMetrics.Accuracy;
            entity.MacroPrecision = model.EvaluationMetrics.MacroPrecision;
            entity.MacroRecall = model.EvaluationMetrics.MacroRecall;
            entity.MacroF1Score = model.EvaluationMetrics.MacroF1Score;
            entity.MicroPrecision = model.EvaluationMetrics.MicroPrecision;
            entity.MicroRecall = model.EvaluationMetrics.MicroRecall;
            entity.MicroF1Score = model.EvaluationMetrics.MicroF1Score;
            entity.LogLoss = model.EvaluationMetrics.LogLoss;
            entity.ConfusionMatrixJson = model.EvaluationMetrics.ConfusionMatrixJson;
            entity.PerClassMetricsJson = model.EvaluationMetrics.PerClassMetricsJson;
            entity.DataAugmentationImpactJson = model.EvaluationMetrics.DataAugmentationImpactJson;
        }
    }
}
