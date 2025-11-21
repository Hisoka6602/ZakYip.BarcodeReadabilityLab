namespace ZakYip.BarcodeReadabilityLab.Application.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZakYip.BarcodeReadabilityLab.Application.Options;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enums;

/// <summary>
/// 无法分析图片路由服务实现
/// </summary>
public sealed class UnresolvedImageRouter : IUnresolvedImageRouter
{
    private readonly ILogger<UnresolvedImageRouter> _logger;
    private readonly IOptions<BarcodeAnalyzerOptions> _options;

    public UnresolvedImageRouter(
        ILogger<UnresolvedImageRouter> logger,
        IOptions<BarcodeAnalyzerOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async ValueTask RouteAsync(
        BarcodeSample sample,
        BarcodeAnalysisResult result,
        CancellationToken cancellationToken = default)
    {
        if (sample is null)
            throw new ArgumentNullException(nameof(sample));

        if (result is null)
            throw new ArgumentNullException(nameof(result));

        // 判断是否需要路由到无法分析目录
        if (!ShouldRoute(result, out var reason))
        {
            return;
        }

        try
        {
            var targetPath = await CopyToUnresolvedDirectoryAsync(sample.FilePath, cancellationToken);
            _logger.LogInformation(
                "图片已复制到无法分析目录，原因: {Reason}, 源路径: {SourcePath}, 目标路径: {TargetPath}",
                reason, sample.FilePath, targetPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "复制图片到无法分析目录失败，原因: {Reason}, 源路径: {SourcePath}",
                reason, sample.FilePath);
            throw;
        }
    }

    /// <summary>
    /// 判断是否需要路由到无法分析目录
    /// </summary>
    private bool ShouldRoute(BarcodeAnalysisResult result, out string reason)
    {
        var options = _options.Value;

        // 情况 1: IsAnalyzed 为 false
        if (!result.IsAnalyzed)
        {
            reason = "分析未完成";
            return true;
        }

        // 情况 2: Confidence 为空
        if (!result.Confidence.HasValue)
        {
            reason = "置信度为空";
            return true;
        }

        // 情况 3: Confidence 小于阈值
        if (result.Confidence.Value < options.ConfidenceThreshold)
        {
            reason = $"置信度 {result.Confidence.Value:P2} 低于阈值 {options.ConfidenceThreshold:P2}";
            return true;
        }

        reason = string.Empty;
        return false;
    }

    /// <summary>
    /// 复制图片到无法分析目录
    /// </summary>
    private async Task<string> CopyToUnresolvedDirectoryAsync(
        string sourceFilePath,
        CancellationToken cancellationToken)
    {
        var options = _options.Value;
        var unresolvedDirectory = options.UnresolvedDirectory;

        // 按日期分层创建目录
        var dateFolder = DateTime.Now.ToString("yyyyMMdd");
        var targetDirectory = Path.Combine(unresolvedDirectory, dateFolder);

        // 确保目标目录存在
        Directory.CreateDirectory(targetDirectory);

        // 处理文件名冲突：附加时间戳
        var fileName = Path.GetFileName(sourceFilePath);
        var targetPath = Path.Combine(targetDirectory, fileName);

        if (File.Exists(targetPath))
        {
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            var timestamp = DateTime.Now.ToString("HHmmss_fff");
            fileName = $"{fileNameWithoutExt}_{timestamp}{extension}";
            targetPath = Path.Combine(targetDirectory, fileName);
        }

        // 异步复制文件
        await using var sourceStream = new FileStream(
            sourceFilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true);

        await using var targetStream = new FileStream(
            targetPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            useAsync: true);

        await sourceStream.CopyToAsync(targetStream, cancellationToken);

        return targetPath;
    }
}
