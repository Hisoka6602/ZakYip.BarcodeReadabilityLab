namespace ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Contracts;

using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enums;
using ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Models;

/// <summary>
/// 图像分类训练器契约
/// </summary>
public interface IImageClassificationTrainer
{
    /// <summary>
    /// 训练图像分类模型
    /// </summary>
    /// <param name="trainingRootDirectory">训练数据根目录（子目录名称为标签）</param>
    /// <param name="outputModelDirectory">输出模型文件目录</param>
    /// <param name="learningRate">学习率（0-1 之间）</param>
    /// <param name="epochs">训练轮数（Epoch）</param>
    /// <param name="batchSize">批大小（Batch Size）</param>
    /// <param name="validationSplitRatio">验证集分割比例（0.0 到 1.0 之间，可选）</param>
    /// <param name="dataAugmentationOptions">数据增强配置（可选）</param>
    /// <param name="dataBalancingOptions">数据平衡配置（可选）</param>
    /// <param name="progressCallback">训练进度回调（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>训练结果，包含模型文件路径和评估指标</returns>
    Task<TrainingResult> TrainAsync(
        string trainingRootDirectory,
        string outputModelDirectory,
        decimal learningRate,
        int epochs,
        int batchSize,
        decimal? validationSplitRatio = null,
        DataAugmentationOptions? dataAugmentationOptions = null,
        DataBalancingOptions? dataBalancingOptions = null,
        ITrainingProgressCallback? progressCallback = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 使用迁移学习训练图像分类模型
    /// </summary>
    /// <param name="trainingRootDirectory">训练数据根目录（子目录名称为标签）</param>
    /// <param name="outputModelDirectory">输出模型文件目录</param>
    /// <param name="learningRate">学习率（0-1 之间）</param>
    /// <param name="epochs">训练轮数（Epoch）</param>
    /// <param name="batchSize">批大小（Batch Size）</param>
    /// <param name="validationSplitRatio">验证集分割比例（0.0 到 1.0 之间，可选）</param>
    /// <param name="transferLearningOptions">迁移学习配置</param>
    /// <param name="dataAugmentationOptions">数据增强配置（可选）</param>
    /// <param name="dataBalancingOptions">数据平衡配置（可选）</param>
    /// <param name="progressCallback">训练进度回调（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>训练结果，包含模型文件路径和评估指标</returns>
    Task<TrainingResult> TrainWithTransferLearningAsync(
        string trainingRootDirectory,
        string outputModelDirectory,
        decimal learningRate,
        int epochs,
        int batchSize,
        decimal? validationSplitRatio = null,
        TransferLearningOptions? transferLearningOptions = null,
        DataAugmentationOptions? dataAugmentationOptions = null,
        DataBalancingOptions? dataBalancingOptions = null,
        ITrainingProgressCallback? progressCallback = null,
        CancellationToken cancellationToken = default);
}
