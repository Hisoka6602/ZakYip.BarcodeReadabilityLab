namespace ZakYip.BarcodeReadabilityLab.Application.Tests.TestData.Builders;

using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 模型版本测试数据构造器（Builder 模式）
/// </summary>
public sealed class ModelVersionBuilder
{
    private Guid _versionId = Guid.NewGuid();
    private string _versionName = $"v-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
    private string _modelPath = Path.Combine(Path.GetTempPath(), "model.zip");
    private Guid? _trainingJobId = null;
    private Guid? _parentModelVersionId = null;
    private DateTimeOffset _createdAt = DateTimeOffset.UtcNow;
    private bool _isActive = false;
    private string _deploymentSlot = "Production";
    private decimal? _trafficPercentage = null;
    private string? _notes = null;
    private ModelEvaluationMetrics? _evaluationMetrics = null;

    /// <summary>
    /// 设置版本 ID
    /// </summary>
    public ModelVersionBuilder WithVersionId(Guid versionId)
    {
        _versionId = versionId;
        return this;
    }

    /// <summary>
    /// 设置版本名称
    /// </summary>
    public ModelVersionBuilder WithVersionName(string name)
    {
        _versionName = name;
        return this;
    }

    /// <summary>
    /// 设置模型路径
    /// </summary>
    public ModelVersionBuilder WithModelPath(string path)
    {
        _modelPath = path;
        return this;
    }

    /// <summary>
    /// 设置训练任务 ID
    /// </summary>
    public ModelVersionBuilder WithTrainingJobId(Guid? jobId)
    {
        _trainingJobId = jobId;
        return this;
    }

    /// <summary>
    /// 设置父模型版本 ID
    /// </summary>
    public ModelVersionBuilder WithParentModelVersion(Guid? parentId)
    {
        _parentModelVersionId = parentId;
        return this;
    }

    /// <summary>
    /// 设置创建时间
    /// </summary>
    public ModelVersionBuilder WithCreatedAt(DateTimeOffset createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    /// <summary>
    /// 设置激活状态
    /// </summary>
    public ModelVersionBuilder WithIsActive(bool isActive)
    {
        _isActive = isActive;
        return this;
    }

    /// <summary>
    /// 设置部署槽位
    /// </summary>
    public ModelVersionBuilder WithDeploymentSlot(string slot)
    {
        _deploymentSlot = slot;
        return this;
    }

    /// <summary>
    /// 设置流量权重
    /// </summary>
    public ModelVersionBuilder WithTrafficPercentage(decimal? percentage)
    {
        _trafficPercentage = percentage;
        return this;
    }

    /// <summary>
    /// 设置备注
    /// </summary>
    public ModelVersionBuilder WithNotes(string? notes)
    {
        _notes = notes;
        return this;
    }

    /// <summary>
    /// 设置评估指标
    /// </summary>
    public ModelVersionBuilder WithEvaluationMetrics(ModelEvaluationMetrics? metrics)
    {
        _evaluationMetrics = metrics;
        return this;
    }

    /// <summary>
    /// 创建一个激活的模型版本
    /// </summary>
    public ModelVersionBuilder AsActive()
    {
        _isActive = true;
        return this;
    }

    /// <summary>
    /// 创建一个停用的模型版本
    /// </summary>
    public ModelVersionBuilder AsInactive()
    {
        _isActive = false;
        return this;
    }

    /// <summary>
    /// 创建一个生产环境模型版本
    /// </summary>
    public ModelVersionBuilder AsProduction()
    {
        _deploymentSlot = "Production";
        return this;
    }

    /// <summary>
    /// 创建一个 Canary 环境模型版本
    /// </summary>
    public ModelVersionBuilder AsCanary(decimal trafficPercentage = 0.1m)
    {
        _deploymentSlot = "Canary";
        _trafficPercentage = trafficPercentage;
        return this;
    }

    /// <summary>
    /// 构建模型版本对象
    /// </summary>
    public ModelVersion Build()
    {
        return new ModelVersion
        {
            VersionId = _versionId,
            VersionName = _versionName,
            ModelPath = _modelPath,
            TrainingJobId = _trainingJobId,
            ParentModelVersionId = _parentModelVersionId,
            CreatedAt = _createdAt,
            IsActive = _isActive,
            DeploymentSlot = _deploymentSlot,
            TrafficPercentage = _trafficPercentage,
            Notes = _notes,
            EvaluationMetrics = _evaluationMetrics
        };
    }
}
