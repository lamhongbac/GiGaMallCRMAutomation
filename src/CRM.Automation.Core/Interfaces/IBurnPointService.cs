using CRM.Automation.Core.Models;
using CRM.Automation.Core.Models.Requests;

namespace CRM.Automation.Core.Interfaces;

public interface IBurnPointService
{
    Task<BOProcessResult> ExecuteReminderAsync(BurnPointReminderRequest request, CancellationToken ct = default);
}

public interface IResetPointService
{
    Task<BOProcessResult> ExecuteResetPointAsync(ResetPointRequest request, CancellationToken ct = default);
}

public interface IMemberTierService
{
    Task<BOProcessResult> ExecuteDowngradeAsync(DowngradeMembershipRequest request, CancellationToken ct = default);
}

public interface IFirebasePushService
{
    Task<bool> SendPushAsync(string memId, string title, string message, string screen = "RewardHistoryDetail", string function = "GetRewardHistories", CancellationToken ct = default);
}

public interface ISqlConnectionFactory
{
    System.Data.IDbConnection CreateConnection();
}
