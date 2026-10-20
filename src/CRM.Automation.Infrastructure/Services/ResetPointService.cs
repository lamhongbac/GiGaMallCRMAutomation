using System.Diagnostics;
using CRM.Automation.Core.Interfaces;
using CRM.Automation.Core.Models;
using CRM.Automation.Core.Models.Requests;
using CRM.Automation.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace CRM.Automation.Infrastructure.Services;

public class ResetPointService : IResetPointService
{
    private readonly BurnPointRepository _repository;
    private readonly IFirebasePushService _pushService;
    private readonly ILogger<ResetPointService> _logger;

    public ResetPointService(BurnPointRepository repository, IFirebasePushService pushService, ILogger<ResetPointService> logger)
    {
        _repository = repository;
        _pushService = pushService;
        _logger = logger;
    }

    public async Task<BOProcessResult> ExecuteResetPointAsync(ResetPointRequest request, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation(">>> BẮT ĐẦU CHẠY TASK RESET POINT (HỦY ĐIỂM HẾT HẠN) <<<");

        try
        {
            var threshold = DateTime.Today.AddDays(1);
            var pendingPoints = await _repository.GetPendingBurnPointsAsync(threshold, ct);
            _logger.LogInformation("Tìm thấy {Count} bản ghi đang Pending đến hạn cần xử lý.", pendingPoints.Count);

            if (pendingPoints.Count == 0)
            {
                return BOProcessResult.Success(new { processed = 0 }, "Không có điểm hết hạn cần hủy hôm nay.");
            }

            int canceledCount = 0;
            int burnedCount = 0;

            foreach (var item in pendingPoints)
            {
                ct.ThrowIfCancellationRequested();

                // 1. Kiểm tra khách có phát sinh giao dịch trước ngày ExpiredDate hay không
                bool hasShopped = false;
                var lastTrans = await _repository.GetLastTransactionAsync(item.MemID, ct);
                if (lastTrans != null && lastTrans.TransDate < item.ExpiredDate)
                {
                    hasShopped = true;
                }

                if (hasShopped)
                {
                    // Khách đã mua sắm -> HỦY LỆNH XÓA ĐIỂM (Cứu điểm cho khách)
                    await _repository.UpdateBurnStatusAsync(item.ID, status: "Cancel", isBurn: false, isCancel: true, ct: ct);
                    canceledCount++;
                    _logger.LogInformation("Hội viên {MemId} ĐÃ CÓ GIAO DỊCH mới ({Date}) -> Hủy lệnh xóa điểm.", item.MemID, lastTrans?.TransDate);
                }
                else if (item.ExpectedBurnDate <= DateTime.Today)
                {
                    // Khách không mua sắm và đã tới ngày dự kiến hủy -> THỰC THI TRỪ ĐIỂM
                    await _repository.ExecuteBurnPointsAsync(item.MemID, ct);
                    await _repository.UpdateBurnStatusAsync(item.ID, status: "Burned", isBurn: true, isCancel: false, ct: ct);
                    burnedCount++;

                    // Bắn notification báo số điểm đã bị hủy
                    string title = "Thông báo điểm thưởng đã hết hạn";
                    string content = $"Số điểm tích lũy ({item.Point:N0} điểm) của Quý khách đã hết hạn sử dụng theo quy định của chương trình hội viên Gigamall.";
                    _ = _pushService.SendPushAsync(item.MemID, title, content, ct: ct);
                }
            }

            sw.Stop();
            _logger.LogInformation(">>> HOÀN TẤT TASK RESET POINT: Hủy bỏ xóa={Canceled}, Thực tế đã trừ điểm={Burned} trong {Elapsed}ms <<<", canceledCount, burnedCount, sw.ElapsedMilliseconds);

            return BOProcessResult.Success(new
            {
                totalEvaluated = pendingPoints.Count,
                pointsSavedAndCanceled = canceledCount,
                pointsBurned = burnedCount,
                elapsedMilliseconds = sw.ElapsedMilliseconds
            }, $"Đã xử lý xong {pendingPoints.Count} bản ghi (Đã cứu điểm: {canceledCount}, Đã hủy điểm: {burnedCount}).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi nghiêm trọng khi thực thi ResetPoint");
            return BOProcessResult.Fail("RESET_POINT_ERROR", $"Lỗi thực thi: {ex.Message}");
        }
    }
}
