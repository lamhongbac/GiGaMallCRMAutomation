using System.Diagnostics;
using CRM.Automation.Core.Interfaces;
using CRM.Automation.Core.Models;
using CRM.Automation.Core.Models.DTOs;
using CRM.Automation.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace CRM.Automation.Infrastructure.Services;

public class BurnPointService : IBurnPointService
{
    private readonly BurnPointRepository _repository;
    private readonly IFirebasePushService _pushService;
    private readonly ILogger<BurnPointService> _logger;

    public BurnPointService(
        BurnPointRepository repository, 
        IFirebasePushService pushService, 
        ILogger<BurnPointService> logger)
    {
        _repository = repository;
        _pushService = pushService;
        _logger = logger;
    }

    public async Task<BOProcessResult> ExecuteReminderAsync(
        string executionId = "",
        int resetPointYears = 2,
        int remindDay = 30,
        int batchSize = 500,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation(">>> BẮT ĐẦU CHẠY TASK BURN POINT REMINDER [{ExecutionId}]: Years={Years}, Days={Days}, BatchSize={BatchSize} <<<", 
            executionId, resetPointYears, remindDay, batchSize);

        try
        {
            // 1. Quét danh sách hội viên đến hạn (dùng trực tiếp giá trị tham số đã có default)
            var members = await _repository.GetMembersToRemindBurnPointAsync(resetPointYears, remindDay, ct);
            _logger.LogInformation("[{ExecutionId}] Tìm thấy {Count} hội viên không phát sinh giao dịch trong {Years} năm và điểm hết hạn trong {Days} ngày.", 
                executionId, members.Count, resetPointYears, remindDay);

            if (members.Count == 0)
            {
                return BOProcessResult.Success(new { executionId, processed = 0, years = resetPointYears, days = remindDay }, 
                    "Không có hội viên nào cần cảnh báo hôm nay.");
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
            _logger.LogInformation("[{ExecutionId}] Đã lưu {Count} bản ghi cảnh báo vào bảng BurnPoint.", executionId, inserted);

            // 3. Gửi thông báo đẩy bất đồng bộ (Concurrent Batching theo batchSize)
            int sentSuccess = 0;
            var parallelOptions = new ParallelOptions 
            { 
                MaxDegreeOfParallelism = Math.Min(batchSize, 20), 
                CancellationToken = ct 
            };

            await Parallel.ForEachAsync(recordsToInsert, parallelOptions, async (record, token) =>
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
            _logger.LogInformation(">>> [{ExecutionId}] HOÀN TẤT TASK BURN POINT REMINDER: Đã gửi {Sent}/{Total} thông báo trong {Elapsed}ms <<<", 
                executionId, sentSuccess, recordsToInsert.Count, sw.ElapsedMilliseconds);

            return BOProcessResult.Success(new
            {
                executionId,
                totalIdentified = members.Count,
                recordsCreated = inserted,
                notificationsSent = sentSuccess,
                effectiveYears = resetPointYears,
                effectiveDays = remindDay,
                batchSize = batchSize,
                elapsedMilliseconds = sw.ElapsedMilliseconds
            }, $"Đã xử lý xong {members.Count} hội viên, gửi thành công {sentSuccess} thông báo trong {sw.ElapsedMilliseconds}ms.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ExecutionId}] Lỗi nghiêm trọng khi thực thi BurnPointReminder", executionId);
            return BOProcessResult.Fail($"Lỗi thực thi: {ex.Message}", "EXECUTION_ERROR");
        }
    }
}
