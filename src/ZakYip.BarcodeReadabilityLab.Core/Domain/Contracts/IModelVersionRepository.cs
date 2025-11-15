namespace ZakYip.BarcodeReadabilityLab.Core.Domain.Contracts;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 模型版本持久化仓储
/// </summary>
public interface IModelVersionRepository
{
    /// <summary>
    /// 新增模型版本
    /// </summary>
    Task AddAsync(ModelVersion version, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新模型版本元数据
    /// </summary>
    Task UpdateAsync(ModelVersion version, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据标识获取模型版本
    /// </summary>
    Task<ModelVersion?> GetByIdAsync(Guid versionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据多个标识批量获取模型版本
    /// </summary>
    Task<IReadOnlyList<ModelVersion>> GetByIdsAsync(IEnumerable<Guid> versionIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有模型版本
    /// </summary>
    Task<IReadOnlyList<ModelVersion>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定部署槽位的当前激活版本
    /// </summary>
    Task<ModelVersion?> GetActiveAsync(string deploymentSlot, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前所有激活版本列表
    /// </summary>
    Task<IReadOnlyList<ModelVersion>> GetActiveListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定部署槽位下的所有版本
    /// </summary>
    Task<IReadOnlyList<ModelVersion>> GetByDeploymentSlotAsync(string deploymentSlot, CancellationToken cancellationToken = default);

    /// <summary>
    /// 将指定版本设置为部署槽位的激活版本
    /// </summary>
    Task SetActiveVersionAsync(Guid versionId, string deploymentSlot, CancellationToken cancellationToken = default);
}
