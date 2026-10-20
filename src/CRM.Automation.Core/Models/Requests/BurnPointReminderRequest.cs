namespace CRM.Automation.Core.Models.Requests;

public class BurnPointReminderRequest
{
    public int ResetPointYears { get; set; } = 2;
    public int RemindDay { get; set; } = 30;
    public int BatchSize { get; set; } = 500;
}
