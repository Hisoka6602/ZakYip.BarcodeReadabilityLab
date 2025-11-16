namespace ZakYip.BarcodeReadabilityLab.Core.Domain.Contracts;

using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 预训练模型管理器接口
/// </summary>
public interface IPretrainedModelManager
{
    /// <summary>
    /// 获取所有可用的预训练模型列表
    /// </summary>
    /// <returns>预训练模型信息列表</returns>
    Task<List<PretrainedModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定类型的预训练模型信息
    /// </summary>
    /// <param name="modelType">模型类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>模型信息</returns>
    Task<PretrainedModelInfo> GetModelInfoAsync(
        PretrainedModelType modelType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 下载预训练模型
    /// </summary>
    /// <param name="modelType">模型类型</param>
    /// <param name="targetDirectory">目标目录</param>
    /// <param name="progressCallback">下载进度回调（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>下载后的模型本地路径</returns>
    Task<string> DownloadModelAsync(
        PretrainedModelType modelType,
        string targetDirectory,
        Action<decimal>? progressCallback = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查预训练模型是否已下载
    /// </summary>
    /// <param name="modelType">模型类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否已下载</returns>
    Task<bool> IsModelDownloadedAsync(
        PretrainedModelType modelType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取预训练模型的本地路径
    /// </summary>
    /// <param name="modelType">模型类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>本地文件路径，如果未下载则返回 null</returns>
    Task<string?> GetModelLocalPathAsync(
        PretrainedModelType modelType,
        CancellationToken cancellationToken = default);
}
