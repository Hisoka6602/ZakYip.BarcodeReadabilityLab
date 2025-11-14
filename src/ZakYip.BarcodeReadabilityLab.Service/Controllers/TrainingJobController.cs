namespace ZakYip.BarcodeReadabilityLab.Service.Controllers;

using Microsoft.AspNetCore.Mvc;
using ZakYip.BarcodeReadabilityLab.Application.Services;

/// <summary>
/// 训练任务管理控制器
/// </summary>
[ApiController]
[Route("api/training-job")]
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
    /// 启动训练任务
    /// </summary>
    /// <param name="request">训练请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>训练任务 ID</returns>
    [HttpPost("start")]
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
    /// 查询训练任务状态
    /// </summary>
    /// <param name="jobId">训练任务 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>训练任务状态</returns>
    [HttpGet("status/{jobId:guid}")]
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
