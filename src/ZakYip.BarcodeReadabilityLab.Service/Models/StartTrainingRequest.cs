namespace ZakYip.BarcodeReadabilityLab.Service.Models;

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
///   "remarks": "第一次训练测试"
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
    /// 训练任务备注说明（可选）
    /// </summary>
    /// <example>第一次训练测试</example>
    public string? Remarks { get; init; }
}
