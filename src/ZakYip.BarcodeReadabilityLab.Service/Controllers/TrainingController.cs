using Microsoft.AspNetCore.Mvc;
using ZakYip.BarcodeReadabilityLab.Service.Services;
using ZakYip.BarcodeReadabilityLab.Service.Models;

namespace ZakYip.BarcodeReadabilityLab.Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TrainingController : ControllerBase
{
    private readonly ITrainingService _trainingService;
    private readonly ILogger<TrainingController> _logger;

    public TrainingController(ITrainingService trainingService, ILogger<TrainingController> logger)
    {
        _trainingService = trainingService;
        _logger = logger;
    }

    [HttpPost("start")]
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

    [HttpGet("status/{taskId}")]
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

    [HttpPost("cancel/{taskId}")]
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

public class StartTrainingRequest
{
    public string TrainingDataPath { get; set; } = string.Empty;
}
