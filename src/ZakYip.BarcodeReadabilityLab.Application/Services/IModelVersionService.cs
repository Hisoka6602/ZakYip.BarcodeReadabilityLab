namespace ZakYip.BarcodeReadabilityLab.Application.Services;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enum;

/// <summary>
/// 模型版本管理服务
/// </summary>
public interface IModelVersionService
{
    /// <summary>
    /// 注册新的模型版本
    /// </summary>
    Task<ModelVersion> RegisterAsync(ModelVersionRegistration registration, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有模型版本
    /// </summary>
    Task<IReadOnlyList<ModelVersion>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定部署槽位的当前激活模型
    /// </summary>
    Task<ModelVersion?> GetActiveAsync(string deploymentSlot, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据版本标识获取模型版本
    /// </summary>
    Task<ModelVersion?> GetByIdAsync(Guid versionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有激活模型版本
    /// </summary>
    Task<IReadOnlyList<ModelVersion>> GetActiveListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定部署槽位下的所有模型版本
    /// </summary>
    Task<IReadOnlyList<ModelVersion>> GetByDeploymentSlotAsync(string deploymentSlot, CancellationToken cancellationToken = default);

    /// <summary>
    /// 将某个模型版本设置为指定部署槽位的激活版本
    /// </summary>
    Task SetActiveAsync(Guid versionId, string deploymentSlot, CancellationToken cancellationToken = default);

    /// <summary>
    /// 回滚到指定的模型版本
    /// </summary>
    Task RollbackAsync(Guid targetVersionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 对多个模型版本执行对比预测
    /// </summary>
    ValueTask<IReadOnlyList<ModelComparisonResult>> CompareAsync(
        BarcodeSample sample,
        IEnumerable<Guid> versionIds,
        CancellationToken cancellationToken = default);
}
