namespace ZakYip.BarcodeReadabilityLab.Service.Models;

/// <summary>
/// 启动训练任务的请求模型
/// </summary>
public record class StartTrainingRequest
{
    /// <summary>
    /// 训练数据根目录路径（可选，为空时使用配置文件中的默认值）
    /// </summary>
    public string? TrainingRootDirectory { get; init; }

    /// <summary>
    /// 训练输出模型文件存放目录路径（可选，为空时使用配置文件中的默认值）
    /// </summary>
    public string? OutputModelDirectory { get; init; }

    /// <summary>
    /// 验证集分割比例（0.0 到 1.0 之间，可选）
    /// </summary>
    public decimal? ValidationSplitRatio { get; init; }

    /// <summary>
    /// 训练任务备注说明（可选）
    /// </summary>
    public string? Remarks { get; init; }
}
