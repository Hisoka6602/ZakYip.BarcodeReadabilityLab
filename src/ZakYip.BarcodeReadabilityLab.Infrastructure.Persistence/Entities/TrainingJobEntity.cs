namespace ZakYip.BarcodeReadabilityLab.Infrastructure.Persistence.Entities;

using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 训练任务实体（数据库表映射）
/// </summary>
public class TrainingJobEntity
{
    /// <summary>
    /// 训练任务唯一标识符
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// 训练数据根目录路径
    /// </summary>
    public string TrainingRootDirectory { get; set; } = string.Empty;

    /// <summary>
    /// 训练输出模型文件存放目录路径
    /// </summary>
    public string OutputModelDirectory { get; set; } = string.Empty;

    /// <summary>
    /// 验证集分割比例（0.0 到 1.0 之间）
    /// </summary>
    public decimal? ValidationSplitRatio { get; set; }

    /// <summary>
    /// 训练任务状态
    /// </summary>
    public TrainingJobState Status { get; set; }

    /// <summary>
    /// 训练进度百分比（0.0 到 1.0 之间）
    /// </summary>
    public decimal Progress { get; set; }

    /// <summary>
    /// 训练开始时间
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// 训练完成时间（可选）
    /// </summary>
    public DateTimeOffset? CompletedTime { get; set; }

    /// <summary>
    /// 错误信息（训练失败时可用）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 训练任务备注说明（可选）
    /// </summary>
    public string? Remarks { get; set; }

    /// <summary>
    /// 转换为领域模型
    /// </summary>
    public TrainingJob ToModel()
    {
        return new TrainingJob
        {
            JobId = JobId,
            TrainingRootDirectory = TrainingRootDirectory,
            OutputModelDirectory = OutputModelDirectory,
            ValidationSplitRatio = ValidationSplitRatio,
            Status = Status,
            Progress = Progress,
            StartTime = StartTime,
            CompletedTime = CompletedTime,
            ErrorMessage = ErrorMessage,
            Remarks = Remarks
        };
    }

    /// <summary>
    /// 从领域模型创建实体
    /// </summary>
    public static TrainingJobEntity FromModel(TrainingJob model)
    {
        return new TrainingJobEntity
        {
            JobId = model.JobId,
            TrainingRootDirectory = model.TrainingRootDirectory,
            OutputModelDirectory = model.OutputModelDirectory,
            ValidationSplitRatio = model.ValidationSplitRatio,
            Status = model.Status,
            Progress = model.Progress,
            StartTime = model.StartTime,
            CompletedTime = model.CompletedTime,
            ErrorMessage = model.ErrorMessage,
            Remarks = model.Remarks
        };
    }
}
