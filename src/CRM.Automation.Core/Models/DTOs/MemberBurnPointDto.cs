namespace CRM.Automation.Core.Models.DTOs;

/// <summary>
/// Khớp 100% với tập kết quả trả về từ Stored Procedure [GetMembersToRemindBurnPoint]
/// (Tương đương MembersNoTransactionUI của dự án cũ)
/// </summary>
public class MemberBurnPointDto
{
    public string MemID { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public DateTime? DateJoined { get; set; }
    public DateTime? LastTransDate { get; set; }
    public decimal AccPoints { get; set; }
    public decimal RedeemPoints { get; set; }
    public decimal RemainingPoints { get; set; }
    public DateTime? ExpectedBurnDate { get; set; }
    public DateTime ExpiredDate { get; set; }
}

/// <summary>
/// Khớp 100% với cấu trúc bảng [dbo].[mssBurnPoint] trong SQL Server
/// </summary>
public class BurnPointRecordDto
{
    public Guid ID { get; set; }
    public string MemID { get; set; } = string.Empty;
    public DateTime? LastTransDate { get; set; }
    public DateTime? ExpectedBurnDate { get; set; }
    public DateTime ExpiredDate { get; set; }
    public decimal Point { get; set; }
    public string BurnType { get; set; } = "NO_TRANSACTION";
    public bool IsReminderSend { get; set; }
    public DateTime? ReminderSendDate { get; set; }
    public string BurnStatus { get; set; } = "Pending";
    public bool IsBurn { get; set; }
    public DateTime? BurnDate { get; set; }
    public bool IsCancel { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Khớp với bảng [dbo].[mssCardTrans] trong SQL Server
/// </summary>
public class CardTransDto
{
    public string MemID { get; set; } = string.Empty;
    public DateTime TransDate { get; set; }
    public decimal Amount { get; set; }
}
