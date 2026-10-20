namespace CRM.Automation.Core.Models.Requests;

/// <summary>
/// Tham số kỹ thuật tĩnh (bất biến) và phi nghiệp vụ (Non-business) do CheckPoint Framework truyền sang.
/// Dùng để định danh, đối soát phiên chạy và ghi log giữa CheckPoint và API.
/// </summary>
public class CheckPointTaskRequest
{
    /// <summary>
    /// Mã định danh phiên chạy duy nhất từ CheckPoint (ExecutionId / TrackingId)
    /// </summary>
    public string? ExecutionId { get; set; }

    /// <summary>
    /// Mã tác vụ cấu hình trên CheckPoint (ví dụ: "TASK_BURN_POINT_REMINDER")
    /// </summary>
    public string? TaskCode { get; set; }

    /// <summary>
    /// Thời điểm CheckPoint kích hoạt tác vụ
    /// </summary>
    public DateTime? TriggerTime { get; set; }

    /// <summary>
    /// Nguồn kích hoạt (Ví dụ: "CheckPointScheduler", "ManualTrigger", "WebApiAsync")
    /// </summary>
    public string? TriggerBy { get; set; } = "CheckPoint";
}
