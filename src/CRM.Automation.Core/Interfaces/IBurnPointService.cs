using CRM.Automation.Core.Models;

namespace CRM.Automation.Core.Interfaces;

public interface IBurnPointService
{
    Task<BOProcessResult> ExecuteReminderAsync(
        string executionId = "",
        int resetPointYears = 2,
        int remindDay = 30,
        int batchSize = 500,
        CancellationToken ct = default);
}

public interface IResetPointService
{
    Task<BOProcessResult> ExecuteResetPointAsync(
        string executionId = "",
        int batchSize = 500,
        CancellationToken ct = default);
}

public interface IMemberTierService
{
    Task<BOProcessResult> ExecuteDowngradeAsync(
        string executionId = "",
        int evaluationPeriodMonths = 12,
        CancellationToken ct = default);
}

public interface IFirebasePushService
{
    Task<bool> SendPushAsync(string memId, string title, string message, string screen = "RewardHistoryDetail", string function = "GetRewardHistories", CancellationToken ct = default);
}

public interface ISqlConnectionFactory
{
    System.Data.IDbConnection CreateConnection();
}
