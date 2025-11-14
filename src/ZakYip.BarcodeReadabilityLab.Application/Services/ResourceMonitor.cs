namespace ZakYip.BarcodeReadabilityLab.Application.Services;

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

/// <summary>
/// 系统资源监控服务实现
/// </summary>
public sealed class ResourceMonitor : IResourceMonitor
{
    private readonly ILogger<ResourceMonitor> _logger;
    private readonly Process _currentProcess;
    private DateTimeOffset _lastCpuCheck;
    private TimeSpan _lastCpuTime;

    public ResourceMonitor(ILogger<ResourceMonitor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _currentProcess = Process.GetCurrentProcess();
        _lastCpuCheck = DateTimeOffset.UtcNow;
        _lastCpuTime = _currentProcess.TotalProcessorTime;
    }

    /// <inheritdoc />
    public ResourceUsageSnapshot GetCurrentUsage()
    {
        try
        {
            var cpuUsage = CalculateCpuUsage();
            var (usedMemory, totalMemory) = GetMemoryUsage();

            return new ResourceUsageSnapshot
            {
                CpuUsagePercent = cpuUsage,
                UsedMemoryBytes = usedMemory,
                TotalMemoryBytes = totalMemory,
                Timestamp = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取系统资源使用情况时发生错误");
            
            // 返回默认快照
            return new ResourceUsageSnapshot
            {
                CpuUsagePercent = 0m,
                UsedMemoryBytes = 0,
                TotalMemoryBytes = 0,
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }

    /// <inheritdoc />
    public Task<ResourceUsageSnapshot> GetCurrentUsageAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() => GetCurrentUsage(), cancellationToken);
    }

    /// <summary>
    /// 计算 CPU 使用率
    /// </summary>
    private decimal CalculateCpuUsage()
    {
        var currentTime = DateTimeOffset.UtcNow;
        var currentCpuTime = _currentProcess.TotalProcessorTime;

        var elapsedTime = (currentTime - _lastCpuCheck).TotalMilliseconds;
        var elapsedCpuTime = (currentCpuTime - _lastCpuTime).TotalMilliseconds;

        // 更新上次检查时间
        _lastCpuCheck = currentTime;
        _lastCpuTime = currentCpuTime;

        if (elapsedTime <= 0)
        {
            return 0m;
        }

        // CPU 使用率 = (CPU 时间增量 / 实际时间增量) / 处理器数量 * 100
        var cpuUsage = (decimal)(elapsedCpuTime / elapsedTime / Environment.ProcessorCount * 100);

        // 限制在 0-100 范围内
        return Math.Max(0m, Math.Min(100m, cpuUsage));
    }

    /// <summary>
    /// 获取内存使用情况
    /// </summary>
    private (long usedMemory, long totalMemory) GetMemoryUsage()
    {
        // 刷新进程信息
        _currentProcess.Refresh();

        // 获取当前进程使用的内存
        var usedMemory = _currentProcess.WorkingSet64;

        // 获取系统总内存
        long totalMemory;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            totalMemory = GetTotalPhysicalMemoryWindows();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            totalMemory = GetTotalPhysicalMemoryLinux();
        }
        else
        {
            // 其他平台使用默认值
            totalMemory = 0;
        }

        return (usedMemory, totalMemory);
    }

    /// <summary>
    /// 获取 Windows 系统总物理内存
    /// </summary>
    private static long GetTotalPhysicalMemoryWindows()
    {
        try
        {
            // 使用 GC 获取总内存
            var totalBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
            return totalBytes > 0 ? totalBytes : 0;
        }
        catch
        {
            // 如果失败，返回默认值
            return 0;
        }
    }

    /// <summary>
    /// 获取 Linux 系统总物理内存
    /// </summary>
    private static long GetTotalPhysicalMemoryLinux()
    {
        try
        {
            // 在 Linux 上读取 /proc/meminfo
            var memInfo = File.ReadAllLines("/proc/meminfo");
            var memTotalLine = memInfo.FirstOrDefault(line => line.StartsWith("MemTotal:"));
            
            if (memTotalLine is not null)
            {
                // MemTotal: 16384000 kB
                var parts = memTotalLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && long.TryParse(parts[1], out var memKb))
                {
                    return memKb * 1024; // 转换为字节
                }
            }
        }
        catch
        {
            // 如果失败，使用 GC 信息
        }

        // 使用 GC 获取总内存
        var totalBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        return totalBytes;
    }
}
