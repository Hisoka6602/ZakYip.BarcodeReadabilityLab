namespace ZakYip.BarcodeReadabilityLab.Core.Domain.Contracts;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 多模型对比分析服务
/// </summary>
public interface IModelVariantAnalyzer
{
    /// <summary>
    /// 使用多个模型版本对样本执行分析
    /// </summary>
    ValueTask<IReadOnlyList<ModelComparisonResult>> AnalyzeAsync(
        BarcodeSample sample,
        IEnumerable<ModelVersion> modelVersions,
        CancellationToken cancellationToken = default);
}
