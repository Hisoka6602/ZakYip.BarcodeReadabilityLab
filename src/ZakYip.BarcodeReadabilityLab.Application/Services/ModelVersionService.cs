namespace ZakYip.BarcodeReadabilityLab.Application.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Contracts;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enum;
using ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Models;

/// <summary>
/// 模型版本管理服务实现
/// </summary>
public sealed class ModelVersionService : IModelVersionService
{
    private readonly ILogger<ModelVersionService> _logger;
    private readonly IModelVersionRepository _repository;
    private readonly IModelVariantAnalyzer _modelVariantAnalyzer;
    private readonly IOptionsMonitor<BarcodeMlModelOptions> _modelOptionsMonitor;
    private readonly IOptionsMonitorCache<BarcodeMlModelOptions> _optionsCache;

    public ModelVersionService(
        ILogger<ModelVersionService> logger,
        IModelVersionRepository repository,
        IModelVariantAnalyzer modelVariantAnalyzer,
        IOptionsMonitor<BarcodeMlModelOptions> modelOptionsMonitor,
        IOptionsMonitorCache<BarcodeMlModelOptions> optionsCache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _modelVariantAnalyzer = modelVariantAnalyzer ?? throw new ArgumentNullException(nameof(modelVariantAnalyzer));
        _modelOptionsMonitor = modelOptionsMonitor ?? throw new ArgumentNullException(nameof(modelOptionsMonitor));
        _optionsCache = optionsCache ?? throw new ArgumentNullException(nameof(optionsCache));
    }

    /// <inheritdoc />
    public async Task<ModelVersion> RegisterAsync(ModelVersionRegistration registration, CancellationToken cancellationToken = default)
    {
        if (registration is null)
            throw new ArgumentNullException(nameof(registration));

        if (string.IsNullOrWhiteSpace(registration.VersionName))
            throw new ArgumentException("模型版本名称不能为空", nameof(registration));

        if (string.IsNullOrWhiteSpace(registration.ModelPath))
            throw new ArgumentException("模型文件路径不能为空", nameof(registration));

        if (registration.TrafficPercentage is { } traffic && (traffic < 0 || traffic > 1))
            throw new ArgumentOutOfRangeException(nameof(registration.TrafficPercentage), "流量占比必须在 0 与 1 之间");

        var createdAt = registration.CreatedAt ?? DateTimeOffset.UtcNow;
        var version = new ModelVersion
        {
            VersionId = Guid.NewGuid(),
            VersionName = registration.VersionName.Trim(),
            ModelPath = registration.ModelPath.Trim(),
            TrainingJobId = registration.TrainingJobId,
            CreatedAt = createdAt,
            IsActive = registration.SetAsActive,
            DeploymentSlot = string.IsNullOrWhiteSpace(registration.DeploymentSlot) ? "Production" : registration.DeploymentSlot.Trim(),
            TrafficPercentage = registration.TrafficPercentage,
            Notes = registration.Notes,
            EvaluationMetrics = registration.EvaluationMetrics
        };

        await _repository.AddAsync(version, cancellationToken);
        _logger.LogInformation("注册模型版本 => VersionId: {VersionId}, Name: {VersionName}, Slot: {Slot}, Active: {IsActive}", version.VersionId, version.VersionName, version.DeploymentSlot, version.IsActive);

        if (registration.SetAsActive)
        {
            await _repository.SetActiveVersionAsync(version.VersionId, version.DeploymentSlot, cancellationToken);
            UpdateCurrentModelOption(version.ModelPath);
        }

        return version;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ModelVersion>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _repository.GetAllAsync(cancellationToken);

    /// <inheritdoc />
    public Task<ModelVersion?> GetActiveAsync(string deploymentSlot, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deploymentSlot))
            throw new ArgumentException("部署槽位不能为空", nameof(deploymentSlot));

        return _repository.GetActiveAsync(deploymentSlot, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ModelVersion?> GetByIdAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        if (versionId == Guid.Empty)
            throw new ArgumentException("模型版本标识不能为空", nameof(versionId));

        return _repository.GetByIdAsync(versionId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ModelVersion>> GetActiveListAsync(CancellationToken cancellationToken = default) =>
        _repository.GetActiveListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<ModelVersion>> GetByDeploymentSlotAsync(string deploymentSlot, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deploymentSlot))
            throw new ArgumentException("部署槽位不能为空", nameof(deploymentSlot));

        return _repository.GetByDeploymentSlotAsync(deploymentSlot, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SetActiveAsync(Guid versionId, string deploymentSlot, CancellationToken cancellationToken = default)
    {
        if (versionId == Guid.Empty)
            throw new ArgumentException("版本标识不能为空", nameof(versionId));

        if (string.IsNullOrWhiteSpace(deploymentSlot))
            throw new ArgumentException("部署槽位不能为空", nameof(deploymentSlot));

        await _repository.SetActiveVersionAsync(versionId, deploymentSlot, cancellationToken);
        var targetVersion = await _repository.GetByIdAsync(versionId, cancellationToken);

        if (targetVersion is null)
            throw new InvalidOperationException($"指定的模型版本不存在: {versionId}");

        UpdateCurrentModelOption(targetVersion.ModelPath);

        _logger.LogInformation("激活模型版本 => VersionId: {VersionId}, Slot: {Slot}", versionId, deploymentSlot);
    }

    /// <inheritdoc />
    public async Task RollbackAsync(Guid targetVersionId, CancellationToken cancellationToken = default)
    {
        if (targetVersionId == Guid.Empty)
            throw new ArgumentException("版本标识不能为空", nameof(targetVersionId));

        var targetVersion = await _repository.GetByIdAsync(targetVersionId, cancellationToken);

        if (targetVersion is null)
            throw new InvalidOperationException($"指定的模型版本不存在: {targetVersionId}");

        await _repository.SetActiveVersionAsync(targetVersion.VersionId, targetVersion.DeploymentSlot, cancellationToken);
        UpdateCurrentModelOption(targetVersion.ModelPath);

        _logger.LogInformation("已回滚到模型版本 => VersionId: {VersionId}, Slot: {Slot}", targetVersion.VersionId, targetVersion.DeploymentSlot);
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<ModelComparisonResult>> CompareAsync(
        BarcodeSample sample,
        IEnumerable<Guid> versionIds,
        CancellationToken cancellationToken = default)
    {
        if (sample is null)
            throw new ArgumentNullException(nameof(sample));

        if (versionIds is null)
            throw new ArgumentNullException(nameof(versionIds));

        if (!File.Exists(sample.FilePath))
            throw new FileNotFoundException($"样本文件不存在：{sample.FilePath}", sample.FilePath);

        var idArray = versionIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        if (idArray.Length == 0)
            return Array.Empty<ModelComparisonResult>();

        var versions = await _repository.GetByIdsAsync(idArray, cancellationToken);

        if (versions.Count == 0)
            return Array.Empty<ModelComparisonResult>();

        return await _modelVariantAnalyzer.AnalyzeAsync(sample, versions, cancellationToken);
    }

    private void UpdateCurrentModelOption(string modelPath)
    {
        if (string.IsNullOrWhiteSpace(modelPath))
            throw new ArgumentException("模型路径不能为空", nameof(modelPath));

        var trimmedPath = modelPath.Trim();
        var updatedOptions = new BarcodeMlModelOptions
        {
            CurrentModelPath = trimmedPath
        };

        // IOptionsMonitorCache 不支持 TryUpdate，需要先移除再添加
        _optionsCache.TryRemove(Options.DefaultName);
        _optionsCache.TryAdd(Options.DefaultName, updatedOptions);

        _logger.LogInformation("当前在线模型已更新 => Path: {ModelPath}", trimmedPath);
    }
}
