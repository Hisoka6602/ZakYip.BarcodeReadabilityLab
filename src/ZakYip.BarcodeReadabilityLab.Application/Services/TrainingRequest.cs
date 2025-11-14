namespace ZakYip.BarcodeReadabilityLab.Application.Services;

/// <summary>
/// 训练请求
/// </summary>
public record class TrainingRequest
{
    /// <summary>
    /// 训练数据根目录路径
    /// </summary>
    public required string TrainingRootDirectory { get; init; }

    /// <summary>
    /// 训练输出模型文件存放目录路径
    /// </summary>
    public required string OutputModelDirectory { get; init; }

    /// <summary>
    /// 验证集分割比例（0.0 到 1.0 之间，可选）
    /// </summary>
    public decimal? ValidationSplitRatio { get; init; }

    /// <summary>
    /// 训练任务备注说明（可选）
    /// </summary>
    public string? Remarks { get; init; }
}
