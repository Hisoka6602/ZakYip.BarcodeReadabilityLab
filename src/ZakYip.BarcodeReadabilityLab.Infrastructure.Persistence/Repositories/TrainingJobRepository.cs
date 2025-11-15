namespace ZakYip.BarcodeReadabilityLab.Infrastructure.Persistence.Repositories;

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Contracts;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Infrastructure.Persistence.Data;
using ZakYip.BarcodeReadabilityLab.Infrastructure.Persistence.Entities;

/// <summary>
/// 训练任务仓储实现
/// </summary>
public class TrainingJobRepository : ITrainingJobRepository
{
    private readonly TrainingJobDbContext _context;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public TrainingJobRepository(TrainingJobDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task AddAsync(TrainingJob trainingJob, CancellationToken cancellationToken = default)
    {
        if (trainingJob is null)
            throw new ArgumentNullException(nameof(trainingJob));

        var entity = TrainingJobEntity.FromModel(trainingJob);
        await _context.TrainingJobs.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(TrainingJob trainingJob, CancellationToken cancellationToken = default)
    {
        if (trainingJob is null)
            throw new ArgumentNullException(nameof(trainingJob));

        var entity = await _context.TrainingJobs.FindAsync(new object[] { trainingJob.JobId }, cancellationToken);
        
        if (entity is null)
            throw new InvalidOperationException($"训练任务不存在: {trainingJob.JobId}");

        // 更新实体属性
        entity.TrainingRootDirectory = trainingJob.TrainingRootDirectory;
        entity.OutputModelDirectory = trainingJob.OutputModelDirectory;
        entity.ValidationSplitRatio = trainingJob.ValidationSplitRatio;
        entity.LearningRate = trainingJob.LearningRate;
        entity.Epochs = trainingJob.Epochs;
        entity.BatchSize = trainingJob.BatchSize;
        entity.Status = trainingJob.Status;
        entity.Progress = trainingJob.Progress;
        entity.StartTime = trainingJob.StartTime;
        entity.CompletedTime = trainingJob.CompletedTime;
        entity.ErrorMessage = trainingJob.ErrorMessage;
        entity.Remarks = trainingJob.Remarks;

        entity.DataAugmentationOptionsJson = JsonSerializer.Serialize(trainingJob.DataAugmentation, JsonOptions);
        entity.DataBalancingOptionsJson = JsonSerializer.Serialize(trainingJob.DataBalancing, JsonOptions);

        if (trainingJob.EvaluationMetrics is { } metrics)
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
    public async Task<TrainingJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.TrainingJobs.FindAsync(new object[] { jobId }, cancellationToken);
        return entity?.ToModel();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TrainingJob>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _context.TrainingJobs
            .OrderByDescending(e => e.StartTime)
            .ToListAsync(cancellationToken);

        return entities.Select(e => e.ToModel()).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TrainingJob>> GetByStatusAsync(
        TrainingJobState status, 
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.TrainingJobs
            .Where(e => e.Status == status)
            .OrderByDescending(e => e.StartTime)
            .ToListAsync(cancellationToken);

        return entities.Select(e => e.ToModel()).ToList();
    }
}
