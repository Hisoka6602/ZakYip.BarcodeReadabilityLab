namespace ZakYip.BarcodeReadabilityLab.Service.Models;

using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 启动增量训练任务的请求模型
/// </summary>
/// <remarks>
/// 增量训练基于指定的已有模型版本，使用新增样本目录进行继续训练。
/// </remarks>
/// <example>
/// {
///   "baseModelVersionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
///   "trainingRootDirectory": "C:\\BarcodeImages\\IncrementalData\\2025-11-21",
///   "outputModelDirectory": "C:\\BarcodeImages\\Models",
///   "mergeWithHistoricalData": true,
///   "learningRate": 0.0005,
///   "epochs": 5,
///   "batchSize": 16,
///   "remarks": "2025-11-21 每日新增样本增量训练"
/// }
/// </example>
public record class IncrementalTrainingStartRequest
{
    /// <summary>
    /// 基础模型版本 ID（本次增量训练基于的模型版本，必填）
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public required Guid BaseModelVersionId { get; init; }

    /// <summary>
    /// 训练数据根目录路径（通常只包含最新收集/标注的样本，必填）
    /// </summary>
    /// <example>C:\BarcodeImages\IncrementalData\2025-11-21</example>
    public required string TrainingRootDirectory { get; init; }

    /// <summary>
    /// 训练输出模型文件存放目录路径（必填）
    /// </summary>
    /// <example>C:\BarcodeImages\Models</example>
    public required string OutputModelDirectory { get; init; }

    /// <summary>
    /// 是否合并历史训练数据（可选，默认 true）
    /// true: 自动将历史训练数据索引与新数据合并，构造一个"新数据权重更高"的训练集
    /// false: 只使用新数据训练（更偏向"微调新 domain"）
    /// </summary>
    /// <example>true</example>
    public bool MergeWithHistoricalData { get; init; } = true;

    /// <summary>
    /// 验证集分割比例（0.0 到 1.0 之间，可选）
    /// </summary>
    /// <example>0.2</example>
    public decimal? ValidationSplitRatio { get; init; }

    /// <summary>
    /// 学习率（大于 0 的小数，可选）
    /// </summary>
    /// <example>0.0005</example>
    public decimal? LearningRate { get; init; }

    /// <summary>
    /// 训练轮数（Epoch，正整数，可选）
    /// </summary>
    /// <example>5</example>
    public int? Epochs { get; init; }

    /// <summary>
    /// 批大小（Batch Size，正整数，可选）
    /// </summary>
    /// <example>16</example>
    public int? BatchSize { get; init; }

    /// <summary>
    /// 训练任务备注说明（可选）
    /// </summary>
    /// <example>2025-11-21 每日新增样本增量训练</example>
    public string? Remarks { get; init; }

    /// <summary>
    /// 数据增强配置（可选）
    /// </summary>
    public DataAugmentationOptions? DataAugmentation { get; init; }

    /// <summary>
    /// 数据平衡配置（可选）
    /// </summary>
    public DataBalancingOptions? DataBalancing { get; init; }
}
