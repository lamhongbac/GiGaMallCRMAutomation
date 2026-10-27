# ĐẶC TẢ TỰ ĐỘNG HÓA NGẦM CRON JOB 5 PHÚT (BACKGROUND AUTOMATION)
## BƯỚC 2.2: DỊCH VỤ BACKGROUND WORKER & TÍCH HỢP CHECKPOINT FRAMEWORK

> 🏢 **Hệ sinh thái**: CRM Loyalty Gigamall - Sao Mai Solution  
> 🎯 **Mục tiêu**: Đóng gói chu trình tự động quét hóa đơn mới upload, tự động gửi sang OCR Server bóc tách và tự động lưu CSDL thành Worker ngầm chạy chu kỳ 5 phút/lần.  
> 🏛️ **Dự án cũ tham khảo**: `D:\GIT PROJECT\CRM SAOMAI\CRMPROFILELogic\SourceCode\CRMProfileManagement`  
> 📁 **Tệp nguồn tham khảo**: `AutomationService/` & `CRM_CheckPoint_Net8_Integration_Plan.md`

---

## 🔸 PHẦN 1: HIỆN TRẠNG HỆ THỐNG CŨ (AS-IS / LEGACY ANALYSIS)

### 1.1. Mô hình cũ trong `AutomationService`
- Hệ thống cũ sử dụng dịch vụ Windows Service (`AutomationService`) hoặc chạy định kỳ qua Windows Task Scheduler.
- Cơ chế quét phụ thuộc chuỗi kết nối cục bộ, không có cơ chế giám sát heartbeat, không hỗ trợ dừng/chạy linh hoạt từ xa.
- Khi gặp sự cố mạng hoặc máy chủ AI bận, worker bị treo hoặc bỏ sót hóa đơn, không có hàng đợi thử lại (Retry Queue / Dead-letter).

---

## 🔹 PHẦN 2: THIẾT KẾ HỆ THỐNG MỚI (TO-BE / MODERN SPECIFICATION)

### 2.1. Điều kiện tiên quyết để kích hoạt Auto OCR (Step 2.2)
> ⚠️ **Quy tắc điều hành**: Bước 2.2 (Auto OCR) **CHỈ ĐƯỢC PHÉP KÍCH HOẠT** sau khi toàn bộ giao diện kiểm chứng tương tác (Tab 1, Tab 2, Nút [CRMProcess]) đã được người dùng chạy thử nghiệm thực tế và nghiệm thu đạt độ tin cậy 100%.

### 2.2. Kiến trúc Worker Cron Job 5 phút (`CRMMall.BillReview.Worker`)
Xây dựng `AutoOcrBackgroundWorker` kế thừa `BackgroundService` trong .NET 8 (hoặc tích hợp CheckPoint Framework Task Type 1 WebAPI):

```csharp
public class AutoOcrBackgroundWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AutoOcrBackgroundWorker> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingBillsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong chu kỳ Auto OCR");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ProcessPendingBillsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var billRepo = scope.ServiceProvider.GetRequiredService<IBillRepository>();
        var ocrClient = scope.ServiceProvider.GetRequiredService<IOcrApiClient>();

        // 1. Quét danh sách bill chưa OCR theo lô (Batch 20 bills)
        var pendingBills = await billRepo.GetPendingOcrBillsAsync(batchSize: 20);

        foreach (var bill in pendingBills)
        {
            // 2. Đọc file ảnh & gọi OCR Server
            var result = await ocrClient.ExtractBillAsync(...);

            // 3. Cập nhật kết quả & set OCRStatus = 1
            await billRepo.SaveOcrResultAsync(bill.ID, result);
        }
    }
}
```

### 2.3. Cơ chế Chịu lỗi & Idempotency
1. **Xử lý trùng lặp (Idempotency)**: Sử dụng khóa lạc quan hoặc cập nhật có điều kiện `WHERE OCRStatus = 0 OR OCRStatus IS NULL` để đảm bảo không có 2 worker nào xử lý trùng 1 hóa đơn.
2. **Cơ chế Retry & Dead-letter**: Nếu hóa đơn bị lỗi bóc tách do ảnh quá mờ hoặc file hỏng, hệ thống tăng cờ `RetryCount`. Sau 3 lần thất bại, bill được đánh dấu `DataStatus = 'OCR_Failed'` và chuyển sang hàng đợi kiểm toán viên xử lý thủ công, không làm tắc nghẽn luồng xử lý tự động.
