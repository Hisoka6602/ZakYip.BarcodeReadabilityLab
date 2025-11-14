namespace ZakYip.BarcodeReadabilityLab.Application.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZakYip.BarcodeReadabilityLab.Application.Options;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Contracts;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Exceptions;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 目录监控服务实现
/// </summary>
public sealed class DirectoryMonitoringService : IDirectoryMonitoringService, IDisposable
{
    private readonly ILogger<DirectoryMonitoringService> _logger;
    private readonly IOptions<BarcodeAnalyzerOptions> _options;
    private readonly IBarcodeReadabilityAnalyzer _analyzer;
    private readonly IUnresolvedImageRouter _router;
    private FileSystemWatcher? _watcher;
    private bool _isRunning;
    private bool _isDisposed;
    private readonly object _lock = new();
    private readonly HashSet<string> _processingFiles = new();
    private readonly string[] _imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tif", ".tiff" };

    public DirectoryMonitoringService(
        ILogger<DirectoryMonitoringService> logger,
        IOptions<BarcodeAnalyzerOptions> options,
        IBarcodeReadabilityAnalyzer analyzer,
        IUnresolvedImageRouter router)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
        _router = router ?? throw new ArgumentNullException(nameof(router));
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_isRunning)
            {
                _logger.LogWarning("目录监控服务已在运行中，无需重复启动");
                return Task.CompletedTask;
            }

            var options = _options.Value;
            var watchDirectory = options.WatchDirectory;

            if (string.IsNullOrWhiteSpace(watchDirectory))
            {
                throw new ConfigurationException("监控目录路径未配置", "CONFIG_WATCH_DIR_EMPTY");
            }

            if (!Directory.Exists(watchDirectory))
            {
                _logger.LogWarning("监控目录不存在，正在创建：{WatchDirectory}", watchDirectory);
                try
                {
                    Directory.CreateDirectory(watchDirectory);
                }
                catch (Exception ex)
                {
                    throw new ConfigurationException($"无法创建监控目录：{watchDirectory}", "CONFIG_WATCH_DIR_CREATE_FAILED", ex);
                }
            }

            _logger.LogInformation("开始启动目录监控服务，监控目录: {WatchDirectory}", watchDirectory);

            _watcher = new FileSystemWatcher
            {
                Path = watchDirectory,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                Filter = "*.*",
                IncludeSubdirectories = options.IsRecursive,
                EnableRaisingEvents = false
            };

            _watcher.Created += OnFileCreated;
            _watcher.Changed += OnFileChanged;
            _watcher.Error += OnWatcherError;

            _watcher.EnableRaisingEvents = true;
            _isRunning = true;

            _logger.LogInformation("目录监控服务已启动，配置 => 监控目录: {WatchDirectory}, 递归监控: {IsRecursive}, 置信度阈值: {ConfidenceThreshold}",
                watchDirectory, options.IsRecursive, options.ConfidenceThreshold);

            return Task.CompletedTask;
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!_isRunning)
            {
                _logger.LogWarning("目录监控服务未在运行中，无需停止");
                return Task.CompletedTask;
            }

            _logger.LogInformation("正在停止目录监控服务");

            if (_watcher is not null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Created -= OnFileCreated;
                _watcher.Changed -= OnFileChanged;
                _watcher.Error -= OnWatcherError;
                _watcher.Dispose();
                _watcher = null;
            }

            _isRunning = false;

            _logger.LogInformation("目录监控服务已停止");

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 文件创建事件处理
    /// </summary>
    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        if (!IsImageFile(e.FullPath))
        {
            return;
        }

        _logger.LogDebug("检测到新图片文件：{FilePath}", e.FullPath);
        _ = ProcessImageFileAsync(e.FullPath);
    }

    /// <summary>
    /// 文件变更事件处理
    /// </summary>
    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (!IsImageFile(e.FullPath))
        {
            return;
        }

        _logger.LogDebug("检测到图片文件变更：{FilePath}", e.FullPath);
        _ = ProcessImageFileAsync(e.FullPath);
    }

    /// <summary>
    /// 监控器错误处理
    /// </summary>
    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        var exception = e.GetException();
        _logger.LogError(exception, "目录监控发生错误，错误类型: {ExceptionType}", exception?.GetType().Name);
    }

    /// <summary>
    /// 判断是否为图片文件
    /// </summary>
    private bool IsImageFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return _imageExtensions.Contains(extension);
    }

    /// <summary>
    /// 处理图片文件
    /// </summary>
    private async Task ProcessImageFileAsync(string filePath)
    {
        // 防止重复处理同一文件
        lock (_processingFiles)
        {
            if (_processingFiles.Contains(filePath))
            {
                return;
            }
            _processingFiles.Add(filePath);
        }

        try
        {
            // 等待文件写入完成
            await WaitForFileAvailableAsync(filePath);

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("文件不存在，跳过处理：{FilePath}", filePath);
                return;
            }

            _logger.LogInformation("开始处理图片文件：{FilePath}", filePath);

            // 构造 BarcodeSample
            var sample = new BarcodeSample
            {
                SampleId = Guid.NewGuid(),
                FilePath = filePath,
                CapturedAt = DateTimeOffset.Now
            };

            // 调用分析器
            var result = await _analyzer.AnalyzeAsync(sample);

            // 设置 IsAboveThreshold 字段
            var options = _options.Value;
            var isAboveThreshold = result.Confidence.HasValue 
                && result.Confidence.Value >= options.ConfidenceThreshold;

            var updatedResult = result with { IsAboveThreshold = isAboveThreshold };

            _logger.LogInformation(
                "图片分析完成 => 文件: {FilePath}, 已分析: {IsAnalyzed}, 原因: {Reason}, 置信度: {Confidence:P2}, 达标: {IsAboveThreshold}",
                filePath, updatedResult.IsAnalyzed, updatedResult.Reason, updatedResult.Confidence, updatedResult.IsAboveThreshold);

            // 根据条件决定是否调用路由器
            await _router.RouteAsync(sample, updatedResult);
        }
        catch (AnalysisException ex)
        {
            _logger.LogError(ex, "分析图片文件失败 => 文件: {FilePath}, 错误代码: {ErrorCode}", filePath, ex.ErrorCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理图片文件时发生未预期错误 => 文件: {FilePath}, 错误类型: {ExceptionType}", 
                filePath, ex.GetType().Name);
            // 单个文件出错不影响其他文件处理，仅记录日志
        }
        finally
        {
            lock (_processingFiles)
            {
                _processingFiles.Remove(filePath);
            }
        }
    }

    /// <summary>
    /// 等待文件可用（文件写入完成）
    /// </summary>
    private async Task WaitForFileAvailableAsync(string filePath)
    {
        const int maxRetries = 10;
        const int delayMilliseconds = 500;

        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                // 尝试以独占模式打开文件，检查是否可访问
                await using var stream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.None);
                
                return; // 文件可用
            }
            catch (IOException)
            {
                // 文件仍在被写入，等待后重试
                if (i < maxRetries - 1)
                {
                    await Task.Delay(delayMilliseconds);
                }
            }
        }

        _logger.LogWarning("等待文件可用超时：{FilePath}", filePath);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
            return;

        StopAsync().Wait();
        _isDisposed = true;

        _logger.LogInformation("DirectoryMonitoringService 已释放");
    }
}
