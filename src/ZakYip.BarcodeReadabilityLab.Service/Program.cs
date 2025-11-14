using ZakYip.BarcodeReadabilityLab.Service;
using ZakYip.BarcodeReadabilityLab.Service.Configuration;
using ZakYip.BarcodeReadabilityLab.Service.Services;
using ZakYip.BarcodeReadabilityLab.Application.Extensions;
using ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

// Configure settings
builder.Services.Configure<BarcodeReadabilityServiceSettings>(
    builder.Configuration.GetSection("BarcodeReadabilityService"));
builder.Services.Configure<ApiSettings>(
    builder.Configuration.GetSection("ApiSettings"));

// Register ML.NET services
builder.Services.AddMlNetBarcodeAnalyzer(builder.Configuration);

// Register application services (包括训练任务服务)
builder.Services.AddBarcodeAnalyzerServices();

// Register legacy services (向后兼容)
builder.Services.AddSingleton<IMLModelService, MLModelService>();
builder.Services.AddSingleton<ITrainingService, TrainingService>();
builder.Services.AddHostedService<ImageMonitoringService>();

// Configure HTTP API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add Windows Service support
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "BarcodeReadabilityService";
});

var host = builder.Build();

// Start the HTTP API
var apiSettings = builder.Configuration.GetSection("ApiSettings").Get<ApiSettings>() ?? new ApiSettings();
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
