namespace ZakYip.BarcodeReadabilityLab.Application.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZakYip.BarcodeReadabilityLab.Application.Options;
using ZakYip.BarcodeReadabilityLab.Infrastructure.Persistence.Data;

/// <summary>
/// 启动配置自检服务实现
/// </summary>
public class StartupSelfCheckService : IStartupSelfCheckService
{
    private readonly ILogger<StartupSelfCheckService> _logger;
    private readonly BarcodeAnalyzerOptions _analyzerOptions;
    private readonly TrainingOptions _trainingOptions;
    private readonly TrainingJobDbContext _dbContext;
    private StartupSelfCheckResult? _lastCheckResult;

    public StartupSelfCheckService(
        ILogger<StartupSelfCheckService> logger,
        IOptions<BarcodeAnalyzerOptions> analyzerOptions,
        IOptions<TrainingOptions> trainingOptions,
        TrainingJobDbContext dbContext)
    {
        _logger = logger;
        _analyzerOptions = analyzerOptions.Value;
        _trainingOptions = trainingOptions.Value;
        _dbContext = dbContext;
    }

    public async Task<StartupSelfCheckResult> PerformSelfCheckAsync()
    {
        _logger.LogInformation("开始执行启动配置自检...");

        var checkResults = new List<SelfCheckResult>();

        // 检查监控目录
        checkResults.Add(CheckDirectory(
            "监控目录",
            _analyzerOptions.WatchDirectory,
            _analyzerOptions.ShouldAutoCreateDirectories,
            isRequired: false));

        // 检查未解决图片目录
        checkResults.Add(CheckDirectory(
            "未解决图片目录",
            _analyzerOptions.UnresolvedDirectory,
            _analyzerOptions.ShouldAutoCreateDirectories,
            isRequired: false));

        // 检查训练数据根目录
        checkResults.Add(CheckDirectory(
            "训练数据根目录",
            _trainingOptions.TrainingRootDirectory,
            _trainingOptions.ShouldAutoCreateDirectories,
            isRequired: false));

        // 检查模型输出目录
        checkResults.Add(CheckDirectory(
            "模型输出目录",
            _trainingOptions.OutputModelDirectory,
            _trainingOptions.ShouldAutoCreateDirectories,
            isRequired: false));

        // 检查数据库连接
        checkResults.Add(await CheckDatabaseConnectionAsync());

        // 判断是否健康
        var criticalChecksFailed = checkResults
            .Where(r => !r.IsHealthy && !r.IsAutoFixed)
            .ToList();

        var isHealthy = criticalChecksFailed.Count == 0;
        var canRun = true; // 始终允许运行，只是某些功能可能不可用

        var result = new StartupSelfCheckResult
        {
            IsHealthy = isHealthy,
            CanRun = canRun,
            CheckResults = checkResults,
            Description = isHealthy
                ? "所有配置检查通过"
                : $"有 {criticalChecksFailed.Count} 项检查未通过，服务以降级模式运行"
        };

        _lastCheckResult = result;

        if (isHealthy)
        {
            _logger.LogInformation("✅ 启动配置自检完成：所有检查通过");
        }
        else
        {
            _logger.LogWarning("⚠️ 启动配置自检完成：有 {Count} 项检查未通过，服务以降级模式运行", criticalChecksFailed.Count);
            foreach (var failed in criticalChecksFailed)
            {
                _logger.LogWarning("  - {CheckName}: {ErrorMessage}", failed.CheckName, failed.ErrorMessage);
            }
        }

        return result;
    }

    public StartupSelfCheckResult? GetLastCheckResult()
    {
        return _lastCheckResult;
    }

    private SelfCheckResult CheckDirectory(
        string checkName,
        string directoryPath,
        bool shouldAutoCreate,
        bool isRequired)
    {
        try
        {
            if (Directory.Exists(directoryPath))
            {
                _logger.LogInformation("✅ {CheckName} 存在：{Path}", checkName, directoryPath);
                return new SelfCheckResult
                {
                    CheckName = checkName,
                    IsHealthy = true,
                    Description = $"目录存在：{directoryPath}"
                };
            }

            if (shouldAutoCreate)
            {
                try
                {
                    Directory.CreateDirectory(directoryPath);
                    _logger.LogWarning("⚠️ {CheckName} 不存在，已自动创建：{Path}", checkName, directoryPath);
                    return new SelfCheckResult
                    {
                        CheckName = checkName,
                        IsHealthy = false,
                        Description = $"目录不存在，已自动创建：{directoryPath}",
                        IsAutoFixed = true
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ {CheckName} 不存在且创建失败：{Path}", checkName, directoryPath);
                    return new SelfCheckResult
                    {
                        CheckName = checkName,
                        IsHealthy = false,
                        ErrorMessage = $"目录不存在且创建失败：{directoryPath}，原因：{ex.Message}"
                    };
                }
            }

            var message = $"目录不存在：{directoryPath}";
            _logger.LogWarning("⚠️ {CheckName} 不存在：{Path}（未配置自动创建）", checkName, directoryPath);

            return new SelfCheckResult
            {
                CheckName = checkName,
                IsHealthy = false,
                ErrorMessage = message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ {CheckName} 检查失败：{Path}", checkName, directoryPath);
            return new SelfCheckResult
            {
                CheckName = checkName,
                IsHealthy = false,
                ErrorMessage = $"检查失败：{ex.Message}"
            };
        }
    }

    private async Task<SelfCheckResult> CheckDatabaseConnectionAsync()
    {
        try
        {
            // 尝试连接数据库
            var canConnect = await _dbContext.Database.CanConnectAsync();

            if (canConnect)
            {
                _logger.LogInformation("✅ 数据库连接正常");
                return new SelfCheckResult
                {
                    CheckName = "数据库连接",
                    IsHealthy = true,
                    Description = "数据库连接正常"
                };
            }

            _logger.LogWarning("⚠️ 数据库连接失败");
            return new SelfCheckResult
            {
                CheckName = "数据库连接",
                IsHealthy = false,
                ErrorMessage = "无法连接到数据库"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 数据库连接检查失败");
            return new SelfCheckResult
            {
                CheckName = "数据库连接",
                IsHealthy = false,
                ErrorMessage = $"数据库连接检查失败：{ex.Message}"
            };
        }
    }
}
