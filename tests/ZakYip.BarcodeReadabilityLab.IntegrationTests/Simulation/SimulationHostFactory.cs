using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Contracts;
using ZakYip.BarcodeReadabilityLab.Infrastructure.Persistence.Data;
using ZakYip.BarcodeReadabilityLab.Service.Workers;

namespace ZakYip.BarcodeReadabilityLab.IntegrationTests.Simulation;

/// <summary>
/// 仿真测试宿主工厂，用于端到端集成测试
/// </summary>
public sealed class SimulationHostFactory : WebApplicationFactory<Program>
{
    private readonly string _sandboxRoot;

    public SimulationHostFactory()
    {
        // 为每个测试实例创建独立的沙箱目录
        _sandboxRoot = Path.Combine(
            Path.GetTempPath(),
            "barcode-lab-simulation",
            Guid.NewGuid().ToString("N"));
    }

    /// <summary>
    /// 获取沙箱根目录路径
    /// </summary>
    public string SandboxRoot => _sandboxRoot;

    /// <summary>
    /// 获取监控目录路径
    /// </summary>
    public string MonitorPath => Path.Combine(_sandboxRoot, "monitor");

    /// <summary>
    /// 获取无法分析目录路径
    /// </summary>
    public string UnresolvedPath => Path.Combine(_sandboxRoot, "unresolved");

    /// <summary>
    /// 获取训练数据目录路径
    /// </summary>
    public string TrainingDataPath => Path.Combine(_sandboxRoot, "training");

    /// <summary>
    /// 获取模型目录路径
    /// </summary>
    public string ModelPath => Path.Combine(_sandboxRoot, "models");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // 创建测试用的目录结构
        Directory.CreateDirectory(MonitorPath);
        Directory.CreateDirectory(UnresolvedPath);
        Directory.CreateDirectory(TrainingDataPath);
        Directory.CreateDirectory(ModelPath);

        builder.UseEnvironment("SimulationTest");

        builder.ConfigureAppConfiguration((context, configurationBuilder) =>
        {
            // 注入仿真用配置
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BarcodeReadabilityService:MonitorPath"] = MonitorPath,
                ["BarcodeReadabilityService:UnableToAnalyzePath"] = UnresolvedPath,
                ["BarcodeReadabilityService:TrainingDataPath"] = TrainingDataPath,
                ["BarcodeReadabilityService:ModelPath"] = ModelPath,
                ["ApiSettings:Urls"] = "http://127.0.0.1:0",
                ["TrainingOptions:EnableResourceMonitoring"] = "false",
                ["TrainingOptions:MaxConcurrentTrainingJobs"] = "1",
                ["TrainingOptions:QueuePollingIntervalSeconds"] = "1"
            });
        });

        builder.ConfigureServices(services =>
        {
            // 移除 DirectoryMonitoringWorker，避免在测试中启动文件监控
            var hostedServiceDescriptors = services
                .Where(descriptor => descriptor.ServiceType == typeof(IHostedService) &&
                                   descriptor.ImplementationType == typeof(DirectoryMonitoringWorker))
                .ToList();

            foreach (var descriptor in hostedServiceDescriptors)
            {
                services.Remove(descriptor);
            }

            // 替换 DbContext 为 InMemory 数据库
            var dbContextDescriptor = services.SingleOrDefault(
                descriptor => descriptor.ServiceType == typeof(DbContextOptions<TrainingJobDbContext>));
            if (dbContextDescriptor is not null)
            {
                services.Remove(dbContextDescriptor);
            }

            // 使用独立的 InMemory 数据库实例
            var databaseName = $"SimulationTest-{Guid.NewGuid():N}";
            services.AddDbContext<TrainingJobDbContext>(options =>
            {
                options.UseInMemoryDatabase(databaseName);
                options.ConfigureWarnings(warnings =>
                {
                    warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning);
                });
            });

            // 替换 IImageClassificationTrainer 为仿真实现
            services.RemoveAll<IImageClassificationTrainer>();
            services.AddSingleton<IImageClassificationTrainer>(new FakeImageClassificationTrainer(simulationDelayMs: 200));

            // 确保数据库已创建
            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TrainingJobDbContext>();
            dbContext.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // 清理沙箱目录
            if (Directory.Exists(_sandboxRoot))
            {
                try
                {
                    Directory.Delete(_sandboxRoot, recursive: true);
                }
                catch
                {
                    // 忽略清理错误
                }
            }
        }

        base.Dispose(disposing);
    }
}
