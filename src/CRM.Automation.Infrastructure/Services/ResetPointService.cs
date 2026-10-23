using System.Diagnostics;
using CRM.Automation.Core.Interfaces;
using CRM.Automation.Core.Models;
using CRM.Automation.Core.Models.DTOs;
using CRM.Automation.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace CRM.Automation.Infrastructure.Services;

public class ResetPointService : IResetPointService
{
    private readonly BurnPointRepository _repository;
    private readonly IFirebasePushService _pushService;
    private readonly ILogger<ResetPointService> _logger;

    public ResetPointService(
        BurnPointRepository repository, 
        IFirebasePushService pushService, 
        ILogger<ResetPointService> logger)
    {
        _repository = repository;
        _pushService = pushService;
        _logger = logger;
    }

    public async Task<BOProcessResult> ExecuteResetPointAsync(
        string executionId = "", 
        int batchSize = 500, 
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation(">>> BẮT ĐẦU CHẠY TASK RESET POINT [{ExecutionId}] (HỦY ĐIỂM HẾT HẠN): BatchSize={BatchSize} <<<", 
            executionId, batchSize);

        try
        {
            // 1. Quét danh sách điểm Pending đến hạn ExpiredDate <= Ngày mai (Tương đương DateTime.Today.AddDays(1) trong dự án cũ)
            var threshold = DateTime.Today.AddDays(1);
            var pendingPoints = await _repository.GetPendingBurnPointsAsync(threshold, ct);
            _logger.LogInformation("[{ExecutionId}] Tìm thấy {Count} bản ghi đang Pending đến hạn cần xử lý.", executionId, pendingPoints.Count);

            if (pendingPoints.Count == 0)
            {
                return BOProcessResult.Success(new { executionId, processed = 0 }, "Không có điểm hết hạn cần hủy hôm nay.");
            }

            int canceledCount = 0;
            int burnedCount = 0;
            decimal totalBurnedPoints = 0;

            foreach (var item in pendingPoints)
            {
                ct.ThrowIfCancellationRequested();

                // 2. Kiểm tra giao dịch gần nhất của Member (Kế thừa từ BaseCardTrans.GetLastTrans)
                bool shouldCancelBurn = false;
                var lastTrans = await _repository.GetLastTransactionAsync(item.MemID, ct);
                if (lastTrans != null && lastTrans.TransDate < item.ExpiredDate)
                {
                    shouldCancelBurn = true;
                }

                if (shouldCancelBurn)
                {
                    // Hội viên có giao dịch trước ngày hết hạn -> Hủy tác vụ burn điểm, bảo lưu điểm
                    await _repository.UpdateBurnStatusAsync(item.ID, status: "Cancel", isBurn: false, isCancel: true, ct: ct);
                    canceledCount++;
                    _logger.LogInformation("[{ExecutionId}] Hội viên {MemId} ĐÃ CÓ GIAO DỊCH mới ({Date}) -> Hủy lệnh xóa điểm (Cancel Burn).", 
                        executionId, item.MemID, lastTrans?.TransDate);
                }
                else if (item.ExpectedBurnDate <= DateTime.Today)
                {
                    // Hội viên không có giao dịch trước hạn và đã đến ngày dự kiến burn điểm -> THỰC THI TRỪ ĐIỂM
                    decimal burnedPoint = await _repository.ExecuteBurnPointsAsync(item.MemID, ct);
                    await _repository.UpdateBurnStatusAsync(item.ID, status: "Burned", isBurn: true, isCancel: false, ct: ct);
                    
                    burnedCount++;
                    totalBurnedPoints += burnedPoint;

                    // 3. Đọc template thông báo từ bảng [dbo].[G_NotificationMessage] (Number = 'ResetRedeemPoint')
                    var template = await _repository.GetNotificationTemplateAsync("ResetRedeemPoint", ct);
                    string subject = template?.Subject ?? "Thông báo điểm thưởng đã hết hạn";
                    string content = template?.Content ?? "Số điểm tích lũy (#Point# điểm) của Quý khách đã hết hạn sử dụng.";

                    string pointsFormatted = (burnedPoint > 0 ? burnedPoint : item.Point).ToString("N0");
                    subject = subject.Replace("#Point#", pointsFormatted);
                    content = content.Replace("#Point#", pointsFormatted);

                    // 4. Lưu vào hộp thư tin nhắn của Member [dbo].[mssNotification]
                    await _repository.CreateInAppNotificationAsync(
                        memId: item.MemID,
                        subject: subject,
                        content: content,
                        eventName: "ResetRedeemPoint",
                        screenName: "AccountInfo",
                        functionName: "GetMemberClass",
                        expiresDate: DateTime.Now.AddMonths(1),
                        ct: ct
                    );

                    // 5. Đẩy Push Notification tới thiết bị
                    _ = _pushService.SendPushAsync(
                        memId: item.MemID, 
                        title: subject, 
                        message: content, 
                        screen: "AccountInfo", 
                        function: "GetMemberClass", 
                        ct: ct
                    );

                    _logger.LogInformation("[{ExecutionId}] Đã trừ {Points} điểm hết hạn của hội viên {MemId}.", 
                        executionId, pointsFormatted, item.MemID);
                }
            }

            sw.Stop();
            _logger.LogInformation(">>> [{ExecutionId}] HOÀN TẤT TASK RESET POINT: Đã cứu điểm={Canceled}, Đã trừ điểm={Burned} (Tổng điểm trừ={TotalPoints}) trong {Elapsed}ms <<<", 
                executionId, canceledCount, burnedCount, totalBurnedPoints, sw.ElapsedMilliseconds);

            return BOProcessResult.Success(new
            {
                executionId,
                totalEvaluated = pendingPoints.Count,
                pointsSavedAndCanceled = canceledCount,
                membersBurned = burnedCount,
                totalPointsBurned = totalBurnedPoints,
                batchSize = batchSize,
                elapsedMilliseconds = sw.ElapsedMilliseconds
            }, $"Đã xử lý xong {pendingPoints.Count} bản ghi (Đã cứu điểm: {canceledCount}, Đã hủy điểm: {burnedCount}, Tổng điểm hủy: {totalBurnedPoints:N0}).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ExecutionId}] Lỗi nghiêm trọng khi thực thi ResetPoint", executionId);
            return BOProcessResult.Fail($"Lỗi thực thi: {ex.Message}", "RESET_POINT_ERROR");
        }
    }
}