using CRM.Automation.Core.Interfaces;
using CRM.Automation.Core.Models;
using CRM.Automation.Core.Models.Requests;
using Microsoft.Extensions.Logging;

namespace CRM.Automation.Infrastructure.Services;

public class MemberTierService : IMemberTierService
{
    private readonly ILogger<MemberTierService> _logger;

    public MemberTierService(ILogger<MemberTierService> logger)
    {
        _logger = logger;
    }

    public async Task<BOProcessResult> ExecuteDowngradeAsync(DowngradeMembershipRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation(">>> BẮT ĐẦU CHẠY TASK HẠ HẠNG THẺ HỘI VIÊN: Months={Months} <<<", request.EvaluationPeriodMonths);
        await Task.Delay(100, ct); // Mô phỏng tác vụ

        return BOProcessResult.Success(new
        {
            evaluated = 0,
            downgraded = 0,
            message = "Task đánh giá thăng/hạ hạng thẻ hội viên sẵn sàng kích hoạt."
        }, "Đã hoàn tất đánh giá chu kỳ hội viên.");
    }
}
