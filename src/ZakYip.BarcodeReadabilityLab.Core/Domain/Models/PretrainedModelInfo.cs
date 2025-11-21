using ZakYip.BarcodeReadabilityLab.Core.Enum;

namespace ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 预训练模型信息
/// </summary>
public record class PretrainedModelInfo
{
    /// <summary>
    /// 模型类型
    /// </summary>
    public required PretrainedModelType ModelType { get; init; }

    /// <summary>
    /// 模型名称
    /// </summary>
    public required string ModelName { get; init; }

    /// <summary>
    /// 模型描述
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// 模型大小（字节）
    /// </summary>
    public long ModelSizeBytes { get; init; }

    /// <summary>
    /// 是否已下载
    /// </summary>
    public bool IsDownloaded { get; init; }

    /// <summary>
    /// 本地文件路径（如果已下载）
    /// </summary>
    public string? LocalPath { get; init; }

    /// <summary>
    /// 下载地址（如果需要下载）
    /// </summary>
    public string? DownloadUrl { get; init; }

    /// <summary>
    /// 推荐使用场景
    /// </summary>
    public string? RecommendedUseCase { get; init; }

    /// <summary>
    /// 参数数量（百万）
    /// </summary>
    public decimal ParameterCountMillions { get; init; }

    /// <summary>
    /// 训练数据集
    /// </summary>
    public string? TrainedOn { get; init; }
}
