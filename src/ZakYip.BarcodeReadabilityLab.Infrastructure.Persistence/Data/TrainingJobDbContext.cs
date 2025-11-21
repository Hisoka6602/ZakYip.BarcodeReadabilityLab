namespace ZakYip.BarcodeReadabilityLab.Infrastructure.Persistence.Data;

using Microsoft.EntityFrameworkCore;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Infrastructure.Persistence.Entities;

/// <summary>
/// 训练任务数据库上下文
/// </summary>
public class TrainingJobDbContext : DbContext
{
    public TrainingJobDbContext(DbContextOptions<TrainingJobDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// 训练任务实体集合
    /// </summary>
    public DbSet<TrainingJobEntity> TrainingJobs => Set<TrainingJobEntity>();

    /// <summary>
    /// 模型版本实体集合
    /// </summary>
    public DbSet<ModelVersionEntity> ModelVersions => Set<ModelVersionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TrainingJobEntity>(entity =>
        {
            entity.HasKey(e => e.JobId);
            
            entity.Property(e => e.JobId)
                .IsRequired();
            
            entity.Property(e => e.TrainingRootDirectory)
                .IsRequired()
                .HasMaxLength(500);
            
            entity.Property(e => e.OutputModelDirectory)
                .IsRequired()
                .HasMaxLength(500);
            
            entity.Property(e => e.ValidationSplitRatio)
                .HasPrecision(18, 6);

            entity.Property(e => e.LearningRate)
                .HasPrecision(18, 6);

            entity.Property(e => e.Epochs)
                .IsRequired();

            entity.Property(e => e.BatchSize)
                .IsRequired();

            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<int>();
            
            entity.Property(e => e.Progress)
                .HasPrecision(18, 6);
            
            // 将 DateTimeOffset 转换为 Unix 时间戳（毫秒）以支持 SQLite ORDER BY
            entity.Property(e => e.StartTime)
                .IsRequired()
                .HasConversion(
                    v => v.ToUnixTimeMilliseconds(),
                    v => DateTimeOffset.FromUnixTimeMilliseconds(v));
            
            // 将可空 DateTimeOffset 转换为可空 Unix 时间戳（毫秒）
            entity.Property(e => e.CompletedTime)
                .HasConversion(
                    v => v.HasValue ? v.Value.ToUnixTimeMilliseconds() : (long?)null,
                    v => v.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(v.Value) : (DateTimeOffset?)null);
            
            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(2000);
            
            entity.Property(e => e.Remarks)
                .HasMaxLength(1000);

            // 评估指标属性配置
            entity.Property(e => e.Accuracy)
                .HasPrecision(18, 6);

            entity.Property(e => e.MacroPrecision)
                .HasPrecision(18, 6);

            entity.Property(e => e.MacroRecall)
                .HasPrecision(18, 6);

            entity.Property(e => e.MacroF1Score)
                .HasPrecision(18, 6);

            entity.Property(e => e.MicroPrecision)
                .HasPrecision(18, 6);

            entity.Property(e => e.MicroRecall)
                .HasPrecision(18, 6);

            entity.Property(e => e.MicroF1Score)
                .HasPrecision(18, 6);

            entity.Property(e => e.LogLoss)
                .HasPrecision(18, 6);

            entity.Property(e => e.ConfusionMatrixJson);

            entity.Property(e => e.PerClassMetricsJson);

            entity.Property(e => e.DataAugmentationOptionsJson);
            entity.Property(e => e.DataBalancingOptionsJson);
            entity.Property(e => e.DataAugmentationImpactJson);

            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.StartTime);
        });

        modelBuilder.Entity<ModelVersionEntity>(entity =>
        {
            entity.HasKey(e => e.VersionId);

            entity.Property(e => e.VersionId)
                .IsRequired();

            entity.Property(e => e.VersionName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.ModelPath)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.DeploymentSlot)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.TrafficPercentage)
                .HasPrecision(18, 6);

            entity.Property(e => e.Notes)
                .HasMaxLength(1000);

            entity.Property(e => e.Accuracy)
                .HasPrecision(18, 6);

            entity.Property(e => e.MacroPrecision)
                .HasPrecision(18, 6);

            entity.Property(e => e.MacroRecall)
                .HasPrecision(18, 6);

            entity.Property(e => e.MacroF1Score)
                .HasPrecision(18, 6);

            entity.Property(e => e.MicroPrecision)
                .HasPrecision(18, 6);

            entity.Property(e => e.MicroRecall)
                .HasPrecision(18, 6);

            entity.Property(e => e.MicroF1Score)
                .HasPrecision(18, 6);

            entity.Property(e => e.LogLoss)
                .HasPrecision(18, 6);

            // 将 DateTimeOffset 转换为 Unix 时间戳（毫秒）以支持 SQLite ORDER BY
            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasConversion(
                    v => v.ToUnixTimeMilliseconds(),
                    v => DateTimeOffset.FromUnixTimeMilliseconds(v));

            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.DeploymentSlot);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}
