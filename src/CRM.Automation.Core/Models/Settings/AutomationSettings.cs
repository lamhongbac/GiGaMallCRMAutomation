namespace CRM.Automation.Core.Models.Settings;

public class BurnPointSettings
{
    public const string SectionName = "BurnPointSettings";
    public int ResetPointYears { get; set; } = 2;
    public int RemindDay { get; set; } = 30;
    public int BatchSize { get; set; } = 500;
}

public class ResetPointSettings
{
    public const string SectionName = "ResetPointSettings";
    public int BatchSize { get; set; } = 500;
}

public class MemberTierSettings
{
    public const string SectionName = "MemberTierSettings";
    public int EvaluationPeriodMonths { get; set; } = 12;
}
