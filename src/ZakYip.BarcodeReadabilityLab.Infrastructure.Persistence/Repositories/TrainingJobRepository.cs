namespace ZakYip.BarcodeReadabilityLab.Infrastructure.Persistence.Repositories;

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
