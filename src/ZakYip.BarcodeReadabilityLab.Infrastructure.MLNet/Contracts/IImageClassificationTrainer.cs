namespace ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Contracts;

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
    /// <param name="validationSplitRatio">验证集分割比例（0.0 到 1.0 之间，可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>训练完成后的模型文件路径</returns>
    Task<string> TrainAsync(
        string trainingRootDirectory,
        string outputModelDirectory,
        decimal? validationSplitRatio = null,
        CancellationToken cancellationToken = default);
}
