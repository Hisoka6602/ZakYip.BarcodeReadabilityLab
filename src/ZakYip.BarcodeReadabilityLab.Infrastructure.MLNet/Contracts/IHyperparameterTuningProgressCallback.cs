namespace ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Contracts;

using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 超参数调优进度回调接口
/// </summary>
public interface IHyperparameterTuningProgressCallback
{
    /// <summary>
    /// 报告调优开始
    /// </summary>
    /// <param name="tuningJobId">调优任务唯一标识</param>
    /// <param name="totalTrials">总试验次数</param>
    void OnTuningStarted(Guid tuningJobId, int totalTrials);

    /// <summary>
    /// 报告试验开始
    /// </summary>
    /// <param name="trialId">试验唯一标识</param>
    /// <param name="trialNumber">试验序号（1-based）</param>
    /// <param name="totalTrials">总试验次数</param>
    /// <param name="configuration">超参数配置</param>
    void OnTrialStarted(Guid trialId, int trialNumber, int totalTrials, HyperparameterConfiguration configuration);

    /// <summary>
    /// 报告试验完成
    /// </summary>
    /// <param name="result">试验结果</param>
    void OnTrialCompleted(HyperparameterTrialResult result);

    /// <summary>
    /// 报告调优完成
    /// </summary>
    /// <param name="result">调优结果</param>
    void OnTuningCompleted(HyperparameterTuningResult result);

    /// <summary>
    /// 报告调优失败
    /// </summary>
    /// <param name="tuningJobId">调优任务唯一标识</param>
    /// <param name="error">错误消息</param>
    void OnTuningFailed(Guid tuningJobId, string error);
}
