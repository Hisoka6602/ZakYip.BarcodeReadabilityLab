namespace ZakYip.BarcodeReadabilityLab.Infrastructure.Persistence.Entities;

using System.Text.Json;
using System.Text.Json.Serialization;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 训练任务实体（数据库表映射）
/// </summary>
public class TrainingJobEntity
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// 训练任务唯一标识符
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// 训练数据根目录路径
    /// </summary>
    public string TrainingRootDirectory { get; set; } = string.Empty;

    /// <summary>
    /// 训练输出模型文件存放目录路径
    /// </summary>
    public string OutputModelDirectory { get; set; } = string.Empty;

    /// <summary>
    /// 验证集分割比例（0.0 到 1.0 之间）
    /// </summary>
    public decimal? ValidationSplitRatio { get; set; }

    /// <summary>
    /// 学习率
    /// </summary>
    public decimal LearningRate { get; set; }

    /// <summary>
    /// 训练轮数（Epoch）
    /// </summary>
    public int Epochs { get; set; }

    /// <summary>
    /// 批大小（Batch Size）
    /// </summary>
    public int BatchSize { get; set; }

    /// <summary>
    /// 训练任务状态
    /// </summary>
    public TrainingJobState Status { get; set; }

    /// <summary>
    /// 训练进度百分比（0.0 到 1.0 之间）
    /// </summary>
    public decimal Progress { get; set; }

    /// <summary>
    /// 训练开始时间
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// 训练完成时间（可选）
    /// </summary>
    public DateTimeOffset? CompletedTime { get; set; }

    /// <summary>
    /// 错误信息（训练失败时可用）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 训练任务备注说明（可选）
    /// </summary>
    public string? Remarks { get; set; }

    /// <summary>
    /// 准确率（训练完成后可用）
    /// </summary>
    public decimal? Accuracy { get; set; }

    /// <summary>
    /// 宏平均精确率（训练完成后可用）
    /// </summary>
    public decimal? MacroPrecision { get; set; }

    /// <summary>
    /// 宏平均召回率（训练完成后可用）
    /// </summary>
    public decimal? MacroRecall { get; set; }

    /// <summary>
    /// 宏平均 F1 分数（训练完成后可用）
    /// </summary>
    public decimal? MacroF1Score { get; set; }

    /// <summary>
    /// 微平均精确率（训练完成后可用）
    /// </summary>
    public decimal? MicroPrecision { get; set; }

    /// <summary>
    /// 微平均召回率（训练完成后可用）
    /// </summary>
    public decimal? MicroRecall { get; set; }

    /// <summary>
    /// 微平均 F1 分数（训练完成后可用）
    /// </summary>
    public decimal? MicroF1Score { get; set; }

    /// <summary>
    /// 对数损失（训练完成后可用）
    /// </summary>
    public decimal? LogLoss { get; set; }

    /// <summary>
    /// 混淆矩阵 JSON（训练完成后可用）
    /// </summary>
    public string? ConfusionMatrixJson { get; set; }

    /// <summary>
    /// 每个类别的评估指标 JSON（训练完成后可用）
    /// </summary>
    public string? PerClassMetricsJson { get; set; }

    /// <summary>
    /// 数据增强配置 JSON
    /// </summary>
    public string? DataAugmentationOptionsJson { get; set; }

    /// <summary>
    /// 数据平衡配置 JSON
    /// </summary>
    public string? DataBalancingOptionsJson { get; set; }

    /// <summary>
    /// 数据增强影响评估 JSON
    /// </summary>
    public string? DataAugmentationImpactJson { get; set; }

    /// <summary>
    /// 转换为领域模型
    /// </summary>
    public TrainingJob ToModel()
    {
        ModelEvaluationMetrics? evaluationMetrics = null;

        // 如果有评估指标数据，构建评估指标对象
        if (Accuracy.HasValue && MacroPrecision.HasValue && MacroRecall.HasValue &&
            MacroF1Score.HasValue && MicroPrecision.HasValue && MicroRecall.HasValue &&
            MicroF1Score.HasValue && !string.IsNullOrWhiteSpace(ConfusionMatrixJson))
        {
            evaluationMetrics = new ModelEvaluationMetrics
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

        var augmentationOptions = !string.IsNullOrWhiteSpace(DataAugmentationOptionsJson)
            ? JsonSerializer.Deserialize<DataAugmentationOptions>(DataAugmentationOptionsJson, JsonOptions) ?? new DataAugmentationOptions()
            : new DataAugmentationOptions();

        var balancingOptions = !string.IsNullOrWhiteSpace(DataBalancingOptionsJson)
            ? JsonSerializer.Deserialize<DataBalancingOptions>(DataBalancingOptionsJson, JsonOptions) ?? new DataBalancingOptions()
            : new DataBalancingOptions();

        return new TrainingJob
        {
            JobId = JobId,
            TrainingRootDirectory = TrainingRootDirectory,
            OutputModelDirectory = OutputModelDirectory,
            ValidationSplitRatio = ValidationSplitRatio,
            LearningRate = LearningRate,
            Epochs = Epochs,
            BatchSize = BatchSize,
            Status = Status,
            Progress = Progress,
            StartTime = StartTime,
            CompletedTime = CompletedTime,
            ErrorMessage = ErrorMessage,
            Remarks = Remarks,
            DataAugmentation = augmentationOptions,
            DataBalancing = balancingOptions,
            EvaluationMetrics = evaluationMetrics
        };
    }

    /// <summary>
    /// 从领域模型创建实体
    /// </summary>
    public static TrainingJobEntity FromModel(TrainingJob model)
    {
        return new TrainingJobEntity
        {
            JobId = model.JobId,
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
}
