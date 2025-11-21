namespace ZakYip.BarcodeReadabilityLab.Service.Workers;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.BarcodeReadabilityLab.Application.Services;
using ZakYip.BarcodeReadabilityLab.Core.Enum;

/// <summary>
/// 目录监控后台工作器
/// </summary>
/// <remarks>
/// 负责在后台启动和管理 IDirectoryMonitoringService，监控指定目录中的图片文件。
/// </remarks>
public sealed class DirectoryMonitoringWorker : BackgroundService
{
    private readonly ILogger<DirectoryMonitoringWorker> _logger;
    private readonly IDirectoryMonitoringService _directoryMonitoringService;

    public DirectoryMonitoringWorker(
        ILogger<DirectoryMonitoringWorker> logger,
        IDirectoryMonitoringService directoryMonitoringService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _directoryMonitoringService = directoryMonitoringService ?? throw new ArgumentNullException(nameof(directoryMonitoringService));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("目录监控工作器正在启动");

        try
        {
            // 启动目录监控服务
            await _directoryMonitoringService.StartAsync(stoppingToken);

            _logger.LogInformation("目录监控已成功启动");

            // 保持运行直到取消标记被触发
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("目录监控工作器正在停止");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "目录监控工作器发生异常：{Message}", ex.Message);
            
            // 对于不可恢复的错误，记录日志但不重新抛出，避免服务崩溃
            // 服务将继续运行，等待其他组件或重启
        }
        finally
        {
            try
            {
                // 停止目录监控服务
                await _directoryMonitoringService.StopAsync(CancellationToken.None);
                _logger.LogInformation("目录监控已成功停止");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "停止目录监控时发生异常：{Message}", ex.Message);
            }
        }
    }
}
