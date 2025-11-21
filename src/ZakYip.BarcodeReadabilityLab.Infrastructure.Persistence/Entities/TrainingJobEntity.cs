namespace ZakYip.BarcodeReadabilityLab.Infrastructure.Persistence.Entities;

using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enums;
using ZakYip.BarcodeReadabilityLab.Infrastructure.Persistence.Mappers;

/// <summary>
/// 训练任务实体（数据库表映射）
/// </summary>
public class TrainingJobEntity
{
    /// <summary>
    /// 训练任务唯一标识符
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// 训练任务类型
    /// </summary>
    public TrainingJobType JobType { get; set; }

    /// <summary>
    /// 使用的训练档位类型
    /// </summary>
    public TrainingProfileType? ProfileType { get; set; }

    /// <summary>
    /// 训练超参数快照（JSON 格式）
    /// </summary>
    public string? HyperparametersSnapshot { get; set; }

    /// <summary>
    /// 是否触发了早停
    /// </summary>
    public bool? TriggeredEarlyStopping { get; set; }

    /// <summary>
    /// 实际训练轮数（如果早停则小于配置的 Epochs）
    /// </summary>
    public int? ActualEpochs { get; set; }

    /// <summary>
    /// 基础模型版本 ID（增量训练时必填）
    /// </summary>
    public Guid? BaseModelVersionId { get; set; }

    /// <summary>
    /// 父训练任务 ID（用于串联训练任务谱系）
    /// </summary>
    public Guid? ParentTrainingJobId { get; set; }

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
    /// 转换为领域模型（委托给 TrainingJobMapper）
    /// </summary>
    public TrainingJob ToModel() => TrainingJobMapper.ToModel(this);

    /// <summary>
    /// 从领域模型创建实体（委托给 TrainingJobMapper）
    /// </summary>
    public static TrainingJobEntity FromModel(TrainingJob model) => TrainingJobMapper.ToEntity(model);
}
