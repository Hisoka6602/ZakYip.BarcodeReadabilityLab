namespace ZakYip.BarcodeReadabilityLab.Core.Domain.Contracts;

using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enum;

/// <summary>
/// 训练任务仓储接口
/// </summary>
public interface ITrainingJobRepository
{
    /// <summary>
    /// 添加新的训练任务
    /// </summary>
    /// <param name="trainingJob">训练任务</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task AddAsync(TrainingJob trainingJob, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新训练任务
    /// </summary>
    /// <param name="trainingJob">训练任务</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UpdateAsync(TrainingJob trainingJob, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据任务 ID 获取训练任务
    /// </summary>
    /// <param name="jobId">任务唯一标识符</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>训练任务，如果不存在则返回 null</returns>
    Task<TrainingJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有训练任务（按开始时间降序）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>训练任务列表</returns>
    Task<IReadOnlyList<TrainingJob>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定状态的训练任务
    /// </summary>
    /// <param name="status">任务状态</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>训练任务列表</returns>
    Task<IReadOnlyList<TrainingJob>> GetByStatusAsync(TrainingJobState status, CancellationToken cancellationToken = default);
}
