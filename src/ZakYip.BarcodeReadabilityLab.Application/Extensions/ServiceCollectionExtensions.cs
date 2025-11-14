namespace ZakYip.BarcodeReadabilityLab.Application.Extensions;

using Microsoft.Extensions.DependencyInjection;
using ZakYip.BarcodeReadabilityLab.Application.Services;
using ZakYip.BarcodeReadabilityLab.Application.Workers;

/// <summary>
/// 应用服务注册扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加条码分析应用服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddBarcodeAnalyzerServices(
        this IServiceCollection services)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        // 注册 IUnresolvedImageRouter 实现
        services.AddSingleton<IUnresolvedImageRouter, UnresolvedImageRouter>();

        // 注册 IDirectoryMonitoringService 实现
        services.AddSingleton<IDirectoryMonitoringService, DirectoryMonitoringService>();

        // 注册系统资源监控服务
        services.AddSingleton<IResourceMonitor, ResourceMonitor>();

        // 注册 ITrainingJobService 实现
        services.AddSingleton<TrainingJobService>();
        services.AddSingleton<ITrainingJobService>(sp => sp.GetRequiredService<TrainingJobService>());

        // 注册训练任务恢复服务（在其他服务启动前执行）
        services.AddHostedService<TrainingJobRecoveryService>();

        // 注册训练任务后台工作器
        services.AddHostedService<TrainingWorker>();

        return services;
    }
}
