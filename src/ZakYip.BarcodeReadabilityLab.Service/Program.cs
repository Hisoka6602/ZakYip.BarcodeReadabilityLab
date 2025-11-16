using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics;
using ZakYip.BarcodeReadabilityLab.Service;
using Microsoft.Extensions.DependencyInjection;
using ZakYip.BarcodeReadabilityLab.Service.Workers;
using ZakYip.BarcodeReadabilityLab.Service.Services;
using ZakYip.BarcodeReadabilityLab.Service.Endpoints;
using ZakYip.BarcodeReadabilityLab.Service.Middleware;
using ZakYip.BarcodeReadabilityLab.Application.Options;
using ZakYip.BarcodeReadabilityLab.Application.Services;
using ZakYip.BarcodeReadabilityLab.Service.Configuration;
using ZakYip.BarcodeReadabilityLab.Application.Extensions;
using ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Extensions;
using ZakYip.BarcodeReadabilityLab.Infrastructure.Persistence.Extensions;

// 创建动态日志级别开关
var levelSwitch = new LoggingLevelSwitch();

// 配置 Serilog 日志
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.ControlledBy(levelSwitch)
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
        .Build())
    .CreateLogger();

try
{
    Log.Information("应用程序启动中...");

    // 使用 WebApplicationBuilder 构建模式，同时支持 Minimal API 和 Windows Service
    var builder = WebApplication.CreateBuilder(args);

    // 使用 Serilog 作为日志提供程序
    builder.Host.UseSerilog();

    // 配置 Windows Service 支持
    builder.Host.UseWindowsService(options =>
    {
        options.ServiceName = "BarcodeReadabilityService";
    });

    // 配置选项绑定
    builder.Services.Configure<BarcodeAnalyzerOptions>(
        builder.Configuration.GetSection("BarcodeAnalyzerOptions"));
    builder.Services.Configure<TrainingOptions>(
        builder.Configuration.GetSection("TrainingOptions"));
    builder.Services.Configure<BarcodeReadabilityServiceSettings>(
        builder.Configuration.GetSection("BarcodeReadabilityService"));
    builder.Services.Configure<ApiSettings>(
        builder.Configuration.GetSection("ApiSettings"));
    builder.Services.Configure<LoggingOptions>(
        builder.Configuration.GetSection("LoggingOptions"));

    // 注册动态日志级别管理服务
    builder.Services.AddSingleton(levelSwitch);
    builder.Services.AddSingleton<ILogLevelManager, LogLevelManager>();

    // 注册 ML.NET 服务 (包括 BarcodeMlModelOptions 配置绑定)
    builder.Services.AddMlNetBarcodeAnalyzer(builder.Configuration);

    // 注册训练任务持久化服务
    builder.Services.AddTrainingJobPersistence();

    // 注册应用服务（包括 IDirectoryMonitoringService、IUnresolvedImageRouter、ITrainingJobService 和 TrainingWorker）
    builder.Services.AddBarcodeAnalyzerServices();

    // 注册训练进度通知服务
    builder.Services.AddSingleton<ITrainingProgressNotifier, SignalRTrainingProgressNotifier>();

    // 注册 SignalR 服务
    builder.Services.AddSignalR()
        .AddJsonProtocol(options =>
        {
            options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.PayloadSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

    // 注册 DirectoryMonitoringWorker 后台服务
    builder.Services.AddHostedService<DirectoryMonitoringWorker>();

    // 配置 HTTP API
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            // 配置 JSON 序列化为小驼峰命名
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });
    builder.Services.AddEndpointsApiExplorer();

    // 配置 Swagger/OpenAPI
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "条码可读性分析 API",
            Version = "v1",
            Description = "提供条码图片可读性分析和模型训练的 API 服务",
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Name = "ZakYip.BarcodeReadabilityLab",
                Url = new Uri("https://github.com/Hisoka6602/ZakYip.BarcodeReadabilityLab")
            }
        });

        // 包含 XML 注释文档
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
        }

        // 配置 Swagger UI 的标签顺序
        options.TagActionsBy(api =>
        {
            if (api.GroupName != null)
            {
                return new[] { api.GroupName };
            }

            if (api.ActionDescriptor is Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor controllerActionDescriptor)
            {
                return new[] { controllerActionDescriptor.ControllerName };
            }

            return new[] { "默认" };
        });

        options.DocInclusionPredicate((name, api) => true);
    });

    var app = builder.Build();

    // 配置监听地址
    var apiSettings = builder.Configuration.GetSection("ApiSettings").Get<ApiSettings>() ?? new ApiSettings();
    app.Urls.Add(apiSettings.Urls);

    // 配置中间件管道
    // 添加异常处理中间件
    app.UseExceptionHandler(exceptionHandlerApp =>
    {
        exceptionHandlerApp.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
            if (exceptionHandlerFeature is not null)
            {
                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(exceptionHandlerFeature.Error, "未处理的异常");

                var errorResponse = new
                {
                    error = "服务器内部错误，请查看日志获取详细信息"
                };

                await context.Response.WriteAsJsonAsync(errorResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
        });
    });

    app.UseRouting();

    // 启用审计日志和性能监控中间件
    app.UseAuditLogging();

    // 启用 Swagger 中间件
    app.UseSwagger(options =>
    {
        options.RouteTemplate = "api-docs/{documentName}/swagger.json";
    });

    // 启用 Swagger UI
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/api-docs/v1/swagger.json", "条码可读性分析 API v1");
        options.RoutePrefix = "api-docs";
        options.DocumentTitle = "条码可读性分析 API 文档";
        options.EnableDeepLinking();
        options.EnableFilter();
        options.EnableTryItOutByDefault();
        options.DisplayRequestDuration();
    });

    // 注册 SignalR Hub 端点
    app.MapHub<ZakYip.BarcodeReadabilityLab.Service.Hubs.TrainingProgressHub>("/hubs/training-progress");

    // 注册 Minimal API 端点
    app.MapTrainingEndpoints();
    app.MapModelEndpoints();
    app.MapLoggingEndpoints();
    app.MapPretrainedModelsEndpoints();

    // 注册传统 MVC 控制器（向后兼容）
    app.MapControllers();

    Log.Information("应用程序已启动，正在监听地址：{Urls}", apiSettings.Urls);
    app.Run();

    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "应用程序启动失败");
    return 1;
}
finally
{
    Log.Information("应用程序正在关闭...");
    Log.CloseAndFlush();
}

/// <summary>
/// Program 类部分定义，用于集成测试
/// </summary>
public partial class Program
{
}
