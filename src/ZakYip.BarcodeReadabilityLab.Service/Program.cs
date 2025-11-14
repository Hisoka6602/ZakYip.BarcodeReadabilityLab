using ZakYip.BarcodeReadabilityLab.Service;
using ZakYip.BarcodeReadabilityLab.Service.Configuration;
using ZakYip.BarcodeReadabilityLab.Service.Services;
using ZakYip.BarcodeReadabilityLab.Service.Workers;
using ZakYip.BarcodeReadabilityLab.Application.Extensions;
using ZakYip.BarcodeReadabilityLab.Application.Options;
using ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

// 使用通用主机构建模式
var builder = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "BarcodeReadabilityService";
    })
    .ConfigureServices((hostContext, services) =>
    {
        var configuration = hostContext.Configuration;

        // 配置选项绑定
        services.Configure<BarcodeAnalyzerOptions>(
            configuration.GetSection("BarcodeAnalyzerOptions"));
        services.Configure<TrainingOptions>(
            configuration.GetSection("TrainingOptions"));
        services.Configure<BarcodeReadabilityServiceSettings>(
            configuration.GetSection("BarcodeReadabilityService"));
        services.Configure<ApiSettings>(
            configuration.GetSection("ApiSettings"));

        // 注册 ML.NET 服务 (包括 BarcodeMlModelOptions 配置绑定)
        services.AddMlNetBarcodeAnalyzer(configuration);

        // 注册应用服务（包括 IDirectoryMonitoringService、IUnresolvedImageRouter、ITrainingJobService 和 TrainingWorker）
        services.AddBarcodeAnalyzerServices();

        // 注册 DirectoryMonitoringWorker 后台服务
        services.AddHostedService<DirectoryMonitoringWorker>();

        // 注册传统服务（向后兼容）
        services.AddSingleton<IMLModelService, MLModelService>();
        services.AddSingleton<ITrainingService, TrainingService>();
        services.AddHostedService<ImageMonitoringService>();

        // 配置 HTTP API
        services.AddControllers();
        services.AddEndpointsApiExplorer();
    });

var host = builder.Build();

// 启动 HTTP API
var apiSettings = host.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSettings>>().Value;
var webHost = new WebHostBuilder()
    .UseKestrel()
    .UseUrls(apiSettings.Urls)
    .ConfigureServices(services =>
    {
        services.AddControllers();
        // 传递服务实例到 Web API
        services.AddSingleton(host.Services.GetRequiredService<ITrainingService>());
        services.AddSingleton(host.Services.GetRequiredService<IMLModelService>());
        services.AddSingleton(host.Services.GetRequiredService<ZakYip.BarcodeReadabilityLab.Application.Services.ITrainingJobService>());
    })
    .Configure(app =>
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    })
    .Build();

_ = webHost.RunAsync();

host.Run();
