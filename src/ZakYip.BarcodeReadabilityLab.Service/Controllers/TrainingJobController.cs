namespace ZakYip.BarcodeReadabilityLab.Service.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using ZakYip.BarcodeReadabilityLab.Application.Services;
using ZakYip.BarcodeReadabilityLab.Core.Enums;

/// <summary>
/// 训练任务管理控制器（传统 MVC API，已废弃）
/// </summary>
/// <remarks>
/// 提供完整的条码可读性模型训练任务管理功能，包括：
/// - 启动训练任务
/// - 查询训练任务状态
/// 
/// ⚠️ 已废弃：此控制器已被 /api/training 下的 Minimal API 端点替代。
/// 建议使用以下端点：
/// - POST /api/training/start - 启动训练任务
/// - GET /api/training/status/{jobId} - 查询训练任务状态
/// - GET /api/training/history - 获取训练历史
/// </remarks>
[ApiController]
[Route("api/training-job")]
[Obsolete("此控制器已废弃，请使用 /api/training 下的 Minimal API 端点")]
public class TrainingJobController : ControllerBase
{
    private readonly ITrainingJobService _trainingJobService;
    private readonly ILogger<TrainingJobController> _logger;

    public TrainingJobController(
        ITrainingJobService trainingJobService,
        ILogger<TrainingJobController> logger)
    {
        _trainingJobService = trainingJobService ?? throw new ArgumentNullException(nameof(trainingJobService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 启动训练任务（已废弃，请使用 POST /api/training/start）
    /// </summary>
    /// <param name="request">训练请求参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>训练任务 ID</returns>
    /// <response code="200">训练任务成功创建并加入队列</response>
    /// <response code="400">请求参数无效或训练目录不存在</response>
    /// <response code="500">服务器内部错误</response>
    [HttpPost("start")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    [Obsolete("请使用 POST /api/training/start 端点")]
    public async Task<IActionResult> StartTrainingAsync(
        [FromBody] TrainingRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("收到训练任务请求，训练目录: {TrainingRootDirectory}",
                request.TrainingRootDirectory);

            var jobId = await _trainingJobService.StartTrainingAsync(request, cancellationToken);

            _logger.LogInformation("训练任务已创建，JobId: {JobId}", jobId);

            return Ok(new
            {
                jobId,
                message = "训练任务已创建并加入队列"
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "训练请求参数无效");
            return BadRequest(new { error = ex.Message });
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.LogWarning(ex, "训练目录不存在");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动训练任务失败");
            return StatusCode(500, new { error = $"启动训练任务失败: {ex.Message}" });
        }
    }

    /// <summary>
    /// 查询训练任务状态（已废弃，请使用 GET /api/training/status/{jobId}）
    /// </summary>
    /// <param name="jobId">训练任务 ID（GUID 格式）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>训练任务状态详情</returns>
    /// <response code="200">成功返回训练任务状态</response>
    /// <response code="404">训练任务不存在</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet("status/{jobId:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [HttpGet("status/{jobId:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    [Obsolete("请使用 GET /api/training/status/{jobId} 端点")]
    public async Task<IActionResult> GetStatusAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var status = await _trainingJobService.GetStatusAsync(jobId, cancellationToken);

            if (status is null)
            {
                _logger.LogWarning("训练任务不存在，JobId: {JobId}", jobId);
                return NotFound(new { error = "训练任务不存在" });
            }

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询训练任务状态失败，JobId: {JobId}", jobId);
            return StatusCode(500, new { error = $"查询训练任务状态失败: {ex.Message}" });
        }
    }
}
