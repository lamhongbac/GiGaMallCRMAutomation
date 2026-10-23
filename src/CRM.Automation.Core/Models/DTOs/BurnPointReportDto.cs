namespace CRM.Automation.Core.Models.DTOs;

/// <summary>
/// Khớp với dữ liệu trả về từ Stored Procedure [GetBurnPointReport] @FromDate, @ToDate
/// </summary>
public class BurnPointReportDto
{
    public long OrderNo { get; set; }
    public string MemID { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? Gender { get; set; }
    public DateTime? DOB { get; set; }
    public decimal Point { get; set; }
    public DateTime? LastTransDate { get; set; }
    public DateTime? ExpectedBurnDate { get; set; }
    public DateTime ExpiredDate { get; set; }
    public bool IsReminderSend { get; set; }
    public DateTime? ReminderSendDate { get; set; }
    public string BurnStatus { get; set; } = string.Empty;
    public bool IsBurn { get; set; }
    public DateTime? BurnDate { get; set; }
    public bool IsCancel { get; set; }
}

/// <summary>
/// Báo cáo thống kê tổng hợp hiệu quả chiến dịch cứu điểm
/// </summary>
public class BurnPointSummaryDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int TotalRemindedMembers { get; set; }
    public decimal TotalRemindedPoints { get; set; }
    public int TotalSavedMembers { get; set; }
    public decimal TotalSavedPoints { get; set; }
    public int TotalBurnedMembers { get; set; }
    public decimal TotalBurnedPoints { get; set; }
    public decimal PointSavingRatePercent => TotalRemindedPoints > 0 
        ? Math.Round((TotalSavedPoints / TotalRemindedPoints) * 100, 2) 
        : 0;
}
