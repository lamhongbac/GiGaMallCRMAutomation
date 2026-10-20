namespace CRM.Automation.Core.Models.DTOs;

public class MemberBurnPointDto
{
    public string MemID { get; set; } = string.Empty;
    public DateTime? LastTransDate { get; set; }
    public DateTime? ExpectedBurnDate { get; set; }
    public DateTime ExpiredDate { get; set; }
    public decimal RemainingPoints { get; set; }
}

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
}

public class CardTransDto
{
    public string MemID { get; set; } = string.Empty;
    public DateTime TransDate { get; set; }
    public decimal Amount { get; set; }
}
