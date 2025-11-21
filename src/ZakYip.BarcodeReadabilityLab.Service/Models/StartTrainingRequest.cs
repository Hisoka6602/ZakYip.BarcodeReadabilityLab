namespace ZakYip.BarcodeReadabilityLab.Service.Models;

using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enums;

/// <summary>
/// 启动训练任务的请求模型
/// </summary>
/// <remarks>
/// 所有字段均为可选，如果未提供则使用配置文件中的默认值。
/// 训练数据目录应包含按类别（如 readable、unreadable）组织的子目录结构。
/// </remarks>
/// <example>
/// {
///   "trainingRootDirectory": "C:\\BarcodeImages\\Training",
///   "outputModelDirectory": "C:\\Models\\Output",
///   "validationSplitRatio": 0.2,
///   "learningRate": 0.01,
///   "epochs": 50,
///   "batchSize": 20,
///   "remarks": "第一次训练测试",
///   "dataAugmentation": {
///     "enable": true,
///     "augmentedImagesPerSample": 2,
///     "enableHorizontalFlip": true
///   },
///   "dataBalancing": {
///     "strategy": "OverSample"
///   }
/// }
/// </example>
public record class StartTrainingRequest
{
    /// <summary>
    /// 训练数据根目录路径（可选，为空时使用配置文件中的默认值）
    /// </summary>
    /// <example>C:\BarcodeImages\Training</example>
    public string? TrainingRootDirectory { get; init; }

    /// <summary>
    /// 训练输出模型文件存放目录路径（可选，为空时使用配置文件中的默认值）
    /// </summary>
    /// <example>C:\Models\Output</example>
    public string? OutputModelDirectory { get; init; }

    /// <summary>
    /// 验证集分割比例（0.0 到 1.0 之间，可选）
    /// </summary>
    /// <example>0.2</example>
    public decimal? ValidationSplitRatio { get; init; }

    /// <summary>
    /// 学习率（大于 0 的小数，可选）
    /// </summary>
    /// <example>0.01</example>
    public decimal? LearningRate { get; init; }

    /// <summary>
    /// 训练轮数（Epoch，正整数，可选）
    /// </summary>
    /// <example>50</example>
    public int? Epochs { get; init; }

    /// <summary>
    /// 批大小（Batch Size，正整数，可选）
    /// </summary>
    /// <example>20</example>
    public int? BatchSize { get; init; }

    /// <summary>
    /// 训练任务备注说明（可选）
    /// </summary>
    /// <example>第一次训练测试</example>
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
