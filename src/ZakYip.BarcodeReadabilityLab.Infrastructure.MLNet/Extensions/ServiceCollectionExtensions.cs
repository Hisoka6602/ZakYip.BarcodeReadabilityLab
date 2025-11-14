namespace ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Extensions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Contracts;
using ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Models;
using ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Services;

/// <summary>
/// ML.NET 服务注册扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加 ML.NET 条码可读性分析器服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddMlNetBarcodeAnalyzer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        if (configuration is null)
            throw new ArgumentNullException(nameof(configuration));

        // 绑定 BarcodeMlModelOptions 配置
        services.Configure<BarcodeMlModelOptions>(
            configuration.GetSection("BarcodeMlModel"));

        // 注册 IBarcodeReadabilityAnalyzer 实现
        services.AddSingleton<IBarcodeReadabilityAnalyzer, MlNetBarcodeReadabilityAnalyzer>();

        return services;
    }

    /// <summary>
    /// 添加 ML.NET 条码可读性分析器服务（使用委托配置）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项的委托</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddMlNetBarcodeAnalyzer(
        this IServiceCollection services,
        Action<BarcodeMlModelOptions> configureOptions)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        if (configureOptions is null)
            throw new ArgumentNullException(nameof(configureOptions));

        // 绑定 BarcodeMlModelOptions 配置
        services.Configure(configureOptions);

        // 注册 IBarcodeReadabilityAnalyzer 实现
        services.AddSingleton<IBarcodeReadabilityAnalyzer, MlNetBarcodeReadabilityAnalyzer>();

        return services;
    }
}
