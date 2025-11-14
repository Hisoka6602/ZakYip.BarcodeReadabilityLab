namespace ZakYip.BarcodeReadabilityLab.Service.Models;

public class TrainingStatus
{
    public string TaskId { get; set; } = string.Empty;
    public TrainingState State { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public double Progress { get; set; }
}

public enum TrainingState
{
    NotStarted,
    Running,
    Completed,
    Failed,
    Cancelled
}
