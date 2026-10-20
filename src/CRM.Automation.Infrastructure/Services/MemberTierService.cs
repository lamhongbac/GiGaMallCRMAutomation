using CRM.Automation.Core.Interfaces;
using CRM.Automation.Core.Models;
using Microsoft.Extensions.Logging;

namespace CRM.Automation.Infrastructure.Services;

public class MemberTierService : IMemberTierService
{
    private readonly ILogger<MemberTierService> _logger;

    public MemberTierService(ILogger<MemberTierService> logger)
    {
        _logger = logger;
    }

    public async Task<BOProcessResult> ExecuteDowngradeAsync(
        string executionId = "", 
        int evaluationPeriodMonths = 12, 
        CancellationToken ct = default)
    {
        _logger.LogInformation(">>> BẮT ĐẦU CHẠY TASK HẠ HẠNG THẺ HỘI VIÊN [{ExecutionId}]: Months={Months} <<<", 
            executionId, evaluationPeriodMonths);
        
        await Task.Delay(100, ct); // Mô phỏng tác vụ

        return BOProcessResult.Success(new
        {
            executionId,
            evaluated = 0,
            downgraded = 0,
            evaluationMonths = evaluationPeriodMonths,
            message = "Task đánh giá thăng/hạ hạng thẻ hội viên sẵn sàng kích hoạt."
        }, "Đã hoàn tất đánh giá chu kỳ hội viên.");
    }
}
