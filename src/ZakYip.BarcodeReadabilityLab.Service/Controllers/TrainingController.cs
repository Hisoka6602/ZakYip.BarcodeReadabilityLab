using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using ZakYip.BarcodeReadabilityLab.Service.Services;
using ZakYip.BarcodeReadabilityLab.Service.Models;

namespace ZakYip.BarcodeReadabilityLab.Service.Controllers;

/// <summary>
/// 训练控制器（传统 API，向后兼容）
/// </summary>
/// <remarks>
/// 提供条码可读性模型训练的传统 REST API 端点。
/// 建议使用 /api/training 端点以获得更完整的功能。
/// </remarks>
[ApiController]
[Route("api/[controller]")]
public class TrainingController : ControllerBase
{
    private readonly ITrainingService _trainingService;
    private readonly ILogger<TrainingController> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="trainingService">训练服务</param>
    /// <param name="logger">日志记录器</param>
    public TrainingController(ITrainingService trainingService, ILogger<TrainingController> logger)
    {
        _trainingService = trainingService;
        _logger = logger;
    }

    /// <summary>
    /// 启动训练任务
    /// </summary>
    /// <param name="request">训练请求参数</param>
    /// <returns>训练任务 ID 和状态消息</returns>
    /// <response code="200">训练任务成功启动</response>
    /// <response code="400">请求参数无效或训练目录不存在</response>
    /// <response code="500">服务器内部错误</response>
    /// <example>
    /// POST /api/training/start
    /// {
    ///   "trainingDataPath": "C:\\BarcodeImages\\Training"
    /// }
    /// </example>
    [HttpPost("start")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> StartTraining([FromBody] StartTrainingRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.TrainingDataPath))
            {
                return BadRequest(new { error = "TrainingDataPath is required" });
            }

            if (!Directory.Exists(request.TrainingDataPath))
            {
                return BadRequest(new { error = "TrainingDataPath does not exist" });
            }

            var taskId = await _trainingService.StartTrainingAsync(request.TrainingDataPath);
            _logger.LogInformation("Training started with task ID: {TaskId}", taskId);

            return Ok(new { taskId, message = "Training started successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting training");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 查询训练任务状态
    /// </summary>
    /// <param name="taskId">训练任务 ID</param>
    /// <returns>训练任务的当前状态信息</returns>
    /// <response code="200">成功返回训练任务状态</response>
    /// <response code="404">训练任务不存在</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet("status/{taskId}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public IActionResult GetStatus(string taskId)
    {
        try
        {
            var status = _trainingService.GetTrainingStatus(taskId);
            
            if (status == null)
            {
                return NotFound(new { error = "Training task not found" });
            }

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting training status");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 取消训练任务
    /// </summary>
    /// <param name="taskId">训练任务 ID</param>
    /// <returns>取消操作结果</returns>
    /// <response code="200">成功请求取消训练任务</response>
    /// <response code="404">训练任务不存在或已完成</response>
    /// <response code="500">服务器内部错误</response>
    [HttpPost("cancel/{taskId}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CancelTraining(string taskId)
    {
        try
        {
            var cancelled = await _trainingService.CancelTrainingAsync(taskId);
            
            if (!cancelled)
            {
                return NotFound(new { error = "Training task not found or already completed" });
            }

            _logger.LogInformation("Training task {TaskId} cancellation requested", taskId);
            return Ok(new { message = "Training cancellation requested successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling training");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

/// <summary>
/// 启动训练请求模型（传统 API）
/// </summary>
public class StartTrainingRequest
{
    /// <summary>
    /// 训练数据目录路径
    /// </summary>
    /// <example>C:\BarcodeImages\Training</example>
    public string TrainingDataPath { get; set; } = string.Empty;
}
