namespace CRM.Automation.Core.Models.Requests;

/// <summary>
/// Request kích hoạt tác vụ Hạ hạng hội viên từ CheckPoint.
/// Chỉ chứa tham số phi nghiệp vụ (ExecutionId, TaskCode, TriggerTime, TriggerBy).
/// </summary>
public class DowngradeMembershipRequest : CheckPointTaskRequest
{
}
