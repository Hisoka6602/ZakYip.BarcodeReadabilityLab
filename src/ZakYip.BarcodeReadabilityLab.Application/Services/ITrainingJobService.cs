namespace ZakYip.BarcodeReadabilityLab.Application.Services;

/// <summary>
/// 训练任务服务契约
/// </summary>
/// <remarks>
/// 用于通过 API 触发 ML.NET 训练任务。
/// 训练是长时间任务，StartTrainingAsync 只负责排队与启动，不阻塞调用方。
/// </remarks>
public interface ITrainingJobService
{
    /// <summary>
    /// 启动训练任务
    /// </summary>
    /// <param name="request">训练请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>训练任务唯一标识符</returns>
    ValueTask<Guid> StartTrainingAsync(TrainingRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询训练任务状态
    /// </summary>
    /// <param name="jobId">训练任务唯一标识符</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>训练任务状态，如果任务不存在则返回 null</returns>
    ValueTask<TrainingJobStatus?> GetStatusAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有训练任务（按开始时间降序）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>训练任务状态列表</returns>
    Task<IReadOnlyList<TrainingJobStatus>> GetAllAsync(CancellationToken cancellationToken = default);
}
