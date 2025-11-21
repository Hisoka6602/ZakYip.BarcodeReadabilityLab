namespace ZakYip.BarcodeReadabilityLab.Application.Tests.TestData.Builders;

using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enums;

/// <summary>
/// 训练任务测试数据构造器（Builder 模式）
/// </summary>
public sealed class TrainingJobBuilder
{
    private Guid _jobId = Guid.NewGuid();
    private string _trainingRootDirectory = Path.Combine(Path.GetTempPath(), "training");
    private string _outputModelDirectory = Path.Combine(Path.GetTempPath(), "output");
    private decimal? _validationSplitRatio = 0.2m;
    private decimal _learningRate = 0.01m;
    private int _epochs = 10;
    private int _batchSize = 8;
    private TrainingJobType _jobType = TrainingJobType.Full;
    private Guid? _baseModelVersionId = null;
    private Guid? _parentTrainingJobId = null;
    private TrainingJobState _status = TrainingJobState.Queued;
    private decimal _progress = 0.0m;
    private DateTimeOffset _startTime = DateTimeOffset.UtcNow;
    private DateTimeOffset? _completedTime = null;
    private string? _errorMessage = null;
    private string? _remarks = null;
    private DataAugmentationOptions _dataAugmentation = new();
    private DataBalancingOptions _dataBalancing = new();
    private ModelEvaluationMetrics? _evaluationMetrics = null;

    /// <summary>
    /// 设置任务 ID
    /// </summary>
    public TrainingJobBuilder WithJobId(Guid jobId)
    {
        _jobId = jobId;
        return this;
    }

    /// <summary>
    /// 设置训练根目录
    /// </summary>
    public TrainingJobBuilder WithTrainingDirectory(string directory)
    {
        _trainingRootDirectory = directory;
        return this;
    }

    /// <summary>
    /// 设置输出目录
    /// </summary>
    public TrainingJobBuilder WithOutputDirectory(string directory)
    {
        _outputModelDirectory = directory;
        return this;
    }

    /// <summary>
    /// 设置验证分割比例
    /// </summary>
    public TrainingJobBuilder WithValidationSplit(decimal? ratio)
    {
        _validationSplitRatio = ratio;
        return this;
    }

    /// <summary>
    /// 设置学习率
    /// </summary>
    public TrainingJobBuilder WithLearningRate(decimal rate)
    {
        _learningRate = rate;
        return this;
    }

    /// <summary>
    /// 设置训练轮数
    /// </summary>
    public TrainingJobBuilder WithEpochs(int epochs)
    {
        _epochs = epochs;
        return this;
    }

    /// <summary>
    /// 设置批大小
    /// </summary>
    public TrainingJobBuilder WithBatchSize(int batchSize)
    {
        _batchSize = batchSize;
        return this;
    }

    /// <summary>
    /// 设置任务类型
    /// </summary>
    public TrainingJobBuilder WithJobType(TrainingJobType jobType)
    {
        _jobType = jobType;
        return this;
    }

    /// <summary>
    /// 设置基础模型版本 ID（用于增量训练）
    /// </summary>
    public TrainingJobBuilder WithBaseModelVersion(Guid? baseModelVersionId)
    {
        _baseModelVersionId = baseModelVersionId;
        return this;
    }

    /// <summary>
    /// 设置任务状态
    /// </summary>
    public TrainingJobBuilder WithStatus(TrainingJobState status)
    {
        _status = status;
        return this;
    }

    /// <summary>
    /// 设置进度
    /// </summary>
    public TrainingJobBuilder WithProgress(decimal progress)
    {
        _progress = progress;
        return this;
    }

    /// <summary>
    /// 设置开始时间
    /// </summary>
    public TrainingJobBuilder WithStartTime(DateTimeOffset startTime)
    {
        _startTime = startTime;
        return this;
    }

    /// <summary>
    /// 设置完成时间
    /// </summary>
    public TrainingJobBuilder WithCompletedTime(DateTimeOffset? completedTime)
    {
        _completedTime = completedTime;
        return this;
    }

    /// <summary>
    /// 设置错误信息
    /// </summary>
    public TrainingJobBuilder WithErrorMessage(string? errorMessage)
    {
        _errorMessage = errorMessage;
        return this;
    }

    /// <summary>
    /// 设置备注
    /// </summary>
    public TrainingJobBuilder WithRemarks(string? remarks)
    {
        _remarks = remarks;
        return this;
    }

    /// <summary>
    /// 设置数据增强选项
    /// </summary>
    public TrainingJobBuilder WithDataAugmentation(DataAugmentationOptions options)
    {
        _dataAugmentation = options;
        return this;
    }

    /// <summary>
    /// 设置数据平衡选项
    /// </summary>
    public TrainingJobBuilder WithDataBalancing(DataBalancingOptions options)
    {
        _dataBalancing = options;
        return this;
    }

    /// <summary>
    /// 设置评估指标
    /// </summary>
    public TrainingJobBuilder WithEvaluationMetrics(ModelEvaluationMetrics? metrics)
    {
        _evaluationMetrics = metrics;
        return this;
    }

    /// <summary>
    /// 创建一个已完成的训练任务
    /// </summary>
    public TrainingJobBuilder AsCompleted()
    {
        _status = TrainingJobState.Completed;
        _progress = 1.0m;
        _completedTime = DateTimeOffset.UtcNow;
        _errorMessage = null;
        return this;
    }

    /// <summary>
    /// 创建一个运行中的训练任务
    /// </summary>
    public TrainingJobBuilder AsRunning(decimal progress = 0.5m)
    {
        _status = TrainingJobState.Running;
        _progress = progress;
        _completedTime = null;
        _errorMessage = null;
        return this;
    }

    /// <summary>
    /// 创建一个失败的训练任务
    /// </summary>
    public TrainingJobBuilder AsFailed(string errorMessage = "训练失败")
    {
        _status = TrainingJobState.Failed;
        _completedTime = DateTimeOffset.UtcNow;
        _errorMessage = errorMessage;
        return this;
    }

    /// <summary>
    /// 构建训练任务对象
    /// </summary>
    public TrainingJob Build()
    {
        return new TrainingJob
        {
            JobId = _jobId,
            TrainingRootDirectory = _trainingRootDirectory,
            OutputModelDirectory = _outputModelDirectory,
            ValidationSplitRatio = _validationSplitRatio,
            LearningRate = _learningRate,
            Epochs = _epochs,
            BatchSize = _batchSize,
            JobType = _jobType,
            BaseModelVersionId = _baseModelVersionId,
            ParentTrainingJobId = _parentTrainingJobId,
            Status = _status,
            Progress = _progress,
            StartTime = _startTime,
            CompletedTime = _completedTime,
            ErrorMessage = _errorMessage,
            Remarks = _remarks,
            DataAugmentation = _dataAugmentation,
            DataBalancing = _dataBalancing,
            EvaluationMetrics = _evaluationMetrics
        };
    }
}
