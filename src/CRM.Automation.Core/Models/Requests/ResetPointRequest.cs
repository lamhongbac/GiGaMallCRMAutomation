namespace CRM.Automation.Core.Models.Requests;

/// <summary>
/// Request kích hoạt tác vụ Hủy điểm hết hạn từ CheckPoint.
/// Chỉ chứa tham số phi nghiệp vụ (ExecutionId, TaskCode, TriggerTime, TriggerBy).
/// </summary>
public class ResetPointRequest : CheckPointTaskRequest
{
}
