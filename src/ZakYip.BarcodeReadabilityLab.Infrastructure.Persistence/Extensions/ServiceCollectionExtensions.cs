namespace ZakYip.BarcodeReadabilityLab.Infrastructure.Persistence.Extensions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Contracts;
using ZakYip.BarcodeReadabilityLab.Infrastructure.Persistence.Data;
using ZakYip.BarcodeReadabilityLab.Infrastructure.Persistence.Repositories;

/// <summary>
/// 持久化服务注册扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加训练任务持久化服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="databasePath">SQLite 数据库文件路径（可选，默认为 trainingjobs.db）</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddTrainingJobPersistence(
        this IServiceCollection services,
        string? databasePath = null)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        // 如果未指定数据库路径，使用默认路径
        var dbPath = databasePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "BarcodeReadabilityLab",
            "trainingjobs.db");

        // 确保数据库目录存在
        var dbDirectory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
        {
            Directory.CreateDirectory(dbDirectory);
        }

        // 注册 DbContext
        services.AddDbContext<TrainingJobDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // 注册仓储
        services.AddScoped<ITrainingJobRepository, TrainingJobRepository>();

        // 确保数据库已创建
        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TrainingJobDbContext>();
        context.Database.EnsureCreated();

        return services;
    }
}
