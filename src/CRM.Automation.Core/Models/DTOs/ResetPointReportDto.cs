namespace CRM.Automation.Core.Models.DTOs;

/// <summary>
/// Báo cáo chi tiết các giao dịch trừ điểm hết hạn thực tế của hội viên
/// </summary>
public class ResetPointDetailReportDto
{
    public long OrderNo { get; set; }
    public string MemID { get; set; } = string.Empty;
    public string? CardNo { get; set; }
    public string? FullName { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public decimal BurnedPoints { get; set; }
    public DateTime? BurnDate { get; set; }
    public DateTime ExpiredDate { get; set; }
    public DateTime? LastTransDate { get; set; }
    public DateTime? ReminderSendDate { get; set; }
    public string BurnStatus { get; set; } = "Burned";
    public DateTime ReminderBatchDate { get; set; }
}

/// <summary>
/// Báo cáo tổng hợp số liệu trừ điểm hết hạn
/// </summary>
public class ResetPointSummaryDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int TotalMembersBurned { get; set; }
    public decimal TotalPointsBurned { get; set; }
    public decimal AveragePointsBurned { get; set; }
    public decimal MaxPointsBurned { get; set; }
    public decimal MinPointsBurned { get; set; }
}