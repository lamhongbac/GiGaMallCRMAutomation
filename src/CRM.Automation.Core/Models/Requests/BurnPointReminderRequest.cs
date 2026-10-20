namespace CRM.Automation.Core.Models.Requests;

/// <summary>
/// Request kích hoạt tác vụ Nhắc nhở cháy điểm từ CheckPoint.
/// Chỉ chứa tham số phi nghiệp vụ (ExecutionId, TaskCode, TriggerTime, TriggerBy).
/// </summary>
public class BurnPointReminderRequest : CheckPointTaskRequest
{
}
