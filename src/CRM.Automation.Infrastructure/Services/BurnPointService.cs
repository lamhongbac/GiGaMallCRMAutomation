using System.Diagnostics;
using CRM.Automation.Core.Interfaces;
using CRM.Automation.Core.Models;
using CRM.Automation.Core.Models.DTOs;
using CRM.Automation.Core.Models.Requests;
using CRM.Automation.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace CRM.Automation.Infrastructure.Services;

public class BurnPointService : IBurnPointService
{
    private readonly BurnPointRepository _repository;
    private readonly IFirebasePushService _pushService;
    private readonly ILogger<BurnPointService> _logger;

    public BurnPointService(BurnPointRepository repository, IFirebasePushService pushService, ILogger<BurnPointService> logger)
    {
        _repository = repository;
        _pushService = pushService;
        _logger = logger;
    }

    public async Task<BOProcessResult> ExecuteReminderAsync(BurnPointReminderRequest request, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation(">>> BẮT ĐẦU CHẠY TASK BURN POINT REMINDER: Years={Years}, Days={Days} <<<", request.ResetPointYears, request.RemindDay);

        if (request.ResetPointYears <= 0 || request.RemindDay <= 0)
        {
            return BOProcessResult.Fail("INVALID_ARGUMENTS", "Tham số ResetPointYears và RemindDay phải lớn hơn 0.");
        }

        try
        {
            // 1. Quét danh sách hội viên đến hạn
            var members = await _repository.GetMembersToRemindBurnPointAsync(request.ResetPointYears, request.RemindDay, ct);
            _logger.LogInformation("Tìm thấy {Count} hội viên không phát sinh giao dịch trong {Years} năm và điểm hết hạn trong {Days} ngày.", members.Count, request.ResetPointYears, request.RemindDay);

            if (members.Count == 0)
            {
                return BOProcessResult.Success(new { processed = 0 }, "Không có hội viên nào cần cảnh báo hôm nay.");
            }

            // 2. Tạo bản ghi bảng BurnPoint
            var recordsToInsert = members.Select(m => new BurnPointRecordDto
            {
                ID = Guid.NewGuid(),
                MemID = m.MemID,
                LastTransDate = m.LastTransDate,
                ExpectedBurnDate = m.ExpectedBurnDate,
                ExpiredDate = m.ExpiredDate,
                Point = m.RemainingPoints,
                BurnType = "NO_TRANSACTION",
                IsReminderSend = false,
                BurnStatus = "Pending",
                IsBurn = false,
                IsCancel = false
            }).ToList();

            int inserted = await _repository.BulkCreateBurnPointsAsync(recordsToInsert, ct);
            _logger.LogInformation("Đã lưu {Count} bản ghi cảnh báo vào bảng BurnPoint.", inserted);

            // 3. Gửi thông báo đẩy bất đồng bộ (Concurrent Batching)
            int sentSuccess = 0;
            await Parallel.ForEachAsync(recordsToInsert, new ParallelOptions { MaxDegreeOfParallelism = 10, CancellationToken = ct }, async (record, token) =>
            {
                string title = "Thông báo điểm thưởng sắp hết hạn";
                string content = $"Điểm tích lũy ({record.Point:N0} điểm) của Quý khách tại Gigamall sẽ hết hạn vào ngày {record.ExpiredDate:dd/MM/yyyy}. Hãy đến mua sắm để duy trì và sử dụng điểm!";

                bool ok = await _pushService.SendPushAsync(record.MemID, title, content, ct: token);
                if (ok)
                {
                    await _repository.UpdateReminderSentAsync(record.ID, token);
                    Interlocked.Increment(ref sentSuccess);
                }
            });

            sw.Stop();
            _logger.LogInformation(">>> HOÀN TẤT TASK BURN POINT REMINDER: Đã gửi {Sent}/{Total} thông báo trong {Elapsed}ms <<<", sentSuccess, recordsToInsert.Count, sw.ElapsedMilliseconds);

            return BOProcessResult.Success(new
            {
                totalIdentified = members.Count,
                recordsCreated = inserted,
                notificationsSent = sentSuccess,
                elapsedMilliseconds = sw.ElapsedMilliseconds
            }, $"Đã xử lý xong {members.Count} hội viên, gửi thành công {sentSuccess} thông báo trong {sw.ElapsedMilliseconds}ms.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi nghiêm trọng khi thực thi BurnPointReminder");
            return BOProcessResult.Fail("EXECUTION_ERROR", $"Lỗi thực thi: {ex.Message}");
        }
    }
}
