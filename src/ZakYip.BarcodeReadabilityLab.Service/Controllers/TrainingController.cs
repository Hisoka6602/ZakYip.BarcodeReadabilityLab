using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ZakYip.BarcodeReadabilityLab.Application.Options;
using ZakYip.BarcodeReadabilityLab.Application.Services;
using ZakYip.BarcodeReadabilityLab.Core.Enums;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Exceptions;
using ZakYip.BarcodeReadabilityLab.Service.Models;

namespace ZakYip.BarcodeReadabilityLab.Service.Controllers;

/// <summary>
/// 训练控制器（传统 API，向后兼容）
/// </summary>
/// <remarks>
/// 提供条码可读性模型训练的传统 REST API 端点。
/// 建议使用 /api/training 端点以获得更完整的功能。
/// 注意：此控制器已废弃，请使用 /api/training 下的 MinimalAPI 端点。
/// </remarks>
[ApiController]
[Route("api/training-legacy")]
public class TrainingController : ControllerBase
{
    private readonly ITrainingJobService _trainingJobService;
    private readonly IOptions<TrainingOptions> _trainingOptions;
    private readonly ILogger<TrainingController> _logger;

    public TrainingController(
        ITrainingJobService trainingJobService,
        IOptions<TrainingOptions> trainingOptions,
        ILogger<TrainingController> logger)
    {
        _trainingJobService = trainingJobService ?? throw new ArgumentNullException(nameof(trainingJobService));
        _trainingOptions = trainingOptions ?? throw new ArgumentNullException(nameof(trainingOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 启动训练任务
    /// </summary>
    /// <param name="request">训练请求参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>训练任务 ID 和状态消息</returns>
    /// <response code="200">训练任务成功启动</response>
    /// <response code="400">请求参数无效或训练目录不存在</response>
    /// <response code="500">服务器内部错误</response>
    [HttpPost("start")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> StartTraining(
        [FromBody] StartTrainingRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var defaults = _trainingOptions.Value;

            var trainingRequest = new TrainingRequest
            {
                TrainingRootDirectory = string.IsNullOrWhiteSpace(request.TrainingRootDirectory)
                    ? defaults.TrainingRootDirectory
                    : request.TrainingRootDirectory!,
                OutputModelDirectory = string.IsNullOrWhiteSpace(request.OutputModelDirectory)
                    ? defaults.OutputModelDirectory
                    : request.OutputModelDirectory!,
                ValidationSplitRatio = request.ValidationSplitRatio ?? defaults.ValidationSplitRatio,
                LearningRate = request.LearningRate ?? defaults.LearningRate,
                Epochs = request.Epochs ?? defaults.Epochs,
                BatchSize = request.BatchSize ?? defaults.BatchSize,
                Remarks = request.Remarks,
                DataAugmentation = request.DataAugmentation ?? (defaults.DataAugmentation with { }),
                DataBalancing = request.DataBalancing ?? (defaults.DataBalancing with { })
            };

            _logger.LogInformation(
                "收到训练任务请求，训练目录: {TrainingRootDirectory}",
                trainingRequest.TrainingRootDirectory);

            var jobId = await _trainingJobService.StartTrainingAsync(trainingRequest, cancellationToken);

            _logger.LogInformation("训练任务已创建，JobId: {JobId}", jobId);

            return Ok(new StartTrainingResponse
            {
                JobId = jobId,
                Message = "训练任务已创建并加入队列"
            });
        }
        catch (TrainingException ex)
        {
            _logger.LogWarning(ex, "训练请求参数无效");
            return BadRequest(new { error = ex.Message, code = ex.ErrorCode });
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.LogWarning(ex, "训练目录不存在");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动训练任务失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 查询训练任务状态
    /// </summary>
    /// <param name="jobId">训练任务 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>训练任务的当前状态信息</returns>
    /// <response code="200">成功返回训练任务状态</response>
    /// <response code="404">训练任务不存在</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet("status/{jobId:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetStatus(
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

            var response = MapToResponse(status);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询训练任务状态失败，JobId: {JobId}", jobId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 取消训练任务（暂不支持）
    /// </summary>
    /// <param name="jobId">训练任务 ID</param>
    /// <returns>取消操作结果</returns>
    [HttpPost("cancel/{jobId:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status501NotImplemented)]
    public Task<IActionResult> CancelTraining(Guid jobId)
    {
        _logger.LogWarning("取消训练任务尚未实现，JobId: {JobId}", jobId);
        IActionResult result = StatusCode(501, new { error = "当前版本暂不支持取消训练任务" });
        return Task.FromResult(result);
    }

    private static TrainingJobResponse MapToResponse(TrainingJobStatus status)
    {
        var stateDescription = status.Status switch
        {
            TrainingJobState.Queued => "排队中",
            TrainingJobState.Running => "运行中",
            TrainingJobState.Completed => "已完成",
            TrainingJobState.Failed => "失败",
            TrainingJobState.Cancelled => "已取消",
            _ => "未知状态"
        };

        return new TrainingJobResponse
        {
            JobId = status.JobId,
            State = stateDescription,
            Progress = status.Progress,
            LearningRate = status.LearningRate,
            Epochs = status.Epochs,
            BatchSize = status.BatchSize,
            Message = status.Status switch
            {
                TrainingJobState.Queued => "训练任务排队中",
                TrainingJobState.Running => "训练任务正在执行",
                TrainingJobState.Completed => "训练任务已完成",
                TrainingJobState.Failed => $"训练任务失败: {status.ErrorMessage}",
                TrainingJobState.Cancelled => "训练任务已取消",
                _ => "未知状态"
            },
            StartTime = status.StartTime,
            CompletedTime = status.CompletedTime,
            ErrorMessage = status.ErrorMessage,
            Remarks = status.Remarks,
            DataAugmentation = status.DataAugmentation,
            DataBalancing = status.DataBalancing,
            EvaluationMetrics = status.EvaluationMetrics
        };
    }
}
