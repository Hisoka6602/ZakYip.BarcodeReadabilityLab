namespace ZakYip.BarcodeReadabilityLab.Infrastructure.Persistence.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Contracts;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Infrastructure.Persistence.Data;
using ZakYip.BarcodeReadabilityLab.Infrastructure.Persistence.Entities;

/// <summary>
/// 模型版本仓储实现
/// </summary>
public sealed class ModelVersionRepository : IModelVersionRepository
{
    private readonly TrainingJobDbContext _context;

    public ModelVersionRepository(TrainingJobDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task AddAsync(ModelVersion version, CancellationToken cancellationToken = default)
    {
        if (version is null)
            throw new ArgumentNullException(nameof(version));

        var entity = ModelVersionEntity.FromModel(version);
        await _context.ModelVersions.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(ModelVersion version, CancellationToken cancellationToken = default)
    {
        if (version is null)
            throw new ArgumentNullException(nameof(version));

        var entity = await _context.ModelVersions
            .FirstOrDefaultAsync(e => e.VersionId == version.VersionId, cancellationToken);

        if (entity is null)
            throw new InvalidOperationException($"模型版本不存在: {version.VersionId}");

        entity.VersionName = version.VersionName;
        entity.ModelPath = version.ModelPath;
        entity.TrainingJobId = version.TrainingJobId;
        entity.CreatedAt = version.CreatedAt;
        entity.IsActive = version.IsActive;
        entity.DeploymentSlot = version.DeploymentSlot;
        entity.TrafficPercentage = version.TrafficPercentage;
        entity.Notes = version.Notes;

        if (version.EvaluationMetrics is { } metrics)
        {
            entity.Accuracy = metrics.Accuracy;
            entity.MacroPrecision = metrics.MacroPrecision;
            entity.MacroRecall = metrics.MacroRecall;
            entity.MacroF1Score = metrics.MacroF1Score;
            entity.MicroPrecision = metrics.MicroPrecision;
            entity.MicroRecall = metrics.MicroRecall;
            entity.MicroF1Score = metrics.MicroF1Score;
            entity.LogLoss = metrics.LogLoss;
            entity.ConfusionMatrixJson = metrics.ConfusionMatrixJson;
            entity.PerClassMetricsJson = metrics.PerClassMetricsJson;
            entity.DataAugmentationImpactJson = metrics.DataAugmentationImpactJson;
        }
        else
        {
            entity.Accuracy = null;
            entity.MacroPrecision = null;
            entity.MacroRecall = null;
            entity.MacroF1Score = null;
            entity.MicroPrecision = null;
            entity.MicroRecall = null;
            entity.MicroF1Score = null;
            entity.LogLoss = null;
            entity.ConfusionMatrixJson = null;
            entity.PerClassMetricsJson = null;
            entity.DataAugmentationImpactJson = null;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ModelVersion?> GetByIdAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ModelVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.VersionId == versionId, cancellationToken);

        return entity?.ToModel();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ModelVersion>> GetByIdsAsync(IEnumerable<Guid> versionIds, CancellationToken cancellationToken = default)
    {
        if (versionIds is null)
            throw new ArgumentNullException(nameof(versionIds));

        var ids = versionIds.Distinct().ToArray();

        if (ids.Length == 0)
            return Array.Empty<ModelVersion>();

        var entities = await _context.ModelVersions
            .AsNoTracking()
            .Where(e => ids.Contains(e.VersionId))
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        return entities
            .Select(e => e.ToModel())
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ModelVersion>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _context.ModelVersions
            .AsNoTracking()
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        return entities
            .Select(e => e.ToModel())
            .ToList();
    }

    /// <inheritdoc />
    public async Task<ModelVersion?> GetActiveAsync(string deploymentSlot, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deploymentSlot))
            throw new ArgumentException("部署槽位不能为空", nameof(deploymentSlot));

        var entity = await _context.ModelVersions
            .AsNoTracking()
            .Where(e => e.DeploymentSlot == deploymentSlot && e.IsActive)
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return entity?.ToModel();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ModelVersion>> GetActiveListAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _context.ModelVersions
            .AsNoTracking()
            .Where(e => e.IsActive)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        return entities
            .Select(e => e.ToModel())
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ModelVersion>> GetByDeploymentSlotAsync(string deploymentSlot, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deploymentSlot))
            throw new ArgumentException("部署槽位不能为空", nameof(deploymentSlot));

        var entities = await _context.ModelVersions
            .AsNoTracking()
            .Where(e => e.DeploymentSlot == deploymentSlot)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        return entities
            .Select(e => e.ToModel())
            .ToList();
    }

    /// <inheritdoc />
    public async Task SetActiveVersionAsync(Guid versionId, string deploymentSlot, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deploymentSlot))
            throw new ArgumentException("部署槽位不能为空", nameof(deploymentSlot));

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var targetEntity = await _context.ModelVersions
            .FirstOrDefaultAsync(e => e.VersionId == versionId, cancellationToken);

        if (targetEntity is null)
            throw new InvalidOperationException($"模型版本不存在: {versionId}");

        var slotEntities = await _context.ModelVersions
            .Where(e => e.DeploymentSlot == deploymentSlot)
            .ToListAsync(cancellationToken);

        foreach (var entity in slotEntities)
        {
            entity.IsActive = entity.VersionId == versionId;
            entity.DeploymentSlot = deploymentSlot;
        }

        if (slotEntities.All(e => e.VersionId != versionId))
        {
            targetEntity.DeploymentSlot = deploymentSlot;
            targetEntity.IsActive = true;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
