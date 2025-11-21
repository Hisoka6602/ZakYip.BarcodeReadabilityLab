namespace ZakYip.BarcodeReadabilityLab.Service.Models;

using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enums;

/// <summary>
/// 预训练模型响应
/// </summary>
public record class PretrainedModelResponse
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
    /// 模型大小（MB）
    /// </summary>
    public decimal ModelSizeMB { get; init; }

    /// <summary>
    /// 是否已下载
    /// </summary>
    public bool IsDownloaded { get; init; }

    /// <summary>
    /// 本地文件路径（如果已下载）
    /// </summary>
    public string? LocalPath { get; init; }

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
