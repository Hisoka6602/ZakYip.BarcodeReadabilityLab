namespace ZakYip.BarcodeReadabilityLab.Application.Services;

/// <summary>
/// 仿真数据生成器接口
/// </summary>
public interface ISimulationDataGenerator
{
    /// <summary>
    /// 生成仿真训练数据
    /// </summary>
    /// <param name="outputDirectory">输出目录</param>
    /// <param name="samplesPerClass">每个类别的样本数量</param>
    /// <returns>生成的数据信息</returns>
    Task<SimulationDataResult> GenerateTrainingDataAsync(string outputDirectory, int samplesPerClass = 5);
}

/// <summary>
/// 仿真数据生成结果
/// </summary>
public record class SimulationDataResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// 输出目录路径
    /// </summary>
    public required string OutputDirectory { get; init; }

    /// <summary>
    /// 生成的类别数量
    /// </summary>
    public int ClassCount { get; init; }

    /// <summary>
    /// 生成的样本总数
    /// </summary>
    public int TotalSamples { get; init; }

    /// <summary>
    /// 错误消息（如果失败）
    /// </summary>
    public string? ErrorMessage { get; init; }
}
