using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CRMMall.BillReview.BLL.Configurations;
using CRMMall.BillReview.BLL.DTOs;
using CRMMall.BillReview.BLL.Services;
using CRMMall.BillReview.DAL.Entities;
using CRMMall.BillReview.DAL.Repositories;

namespace CRMMall.BillReview.GUI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BillsController : ControllerBase
    {
        private readonly IBillReviewService _billReviewService;
        private readonly IBillRepository _billRepository;
        private readonly Microsoft.Extensions.Logging.ILogger<BillsController> _logger;

        public BillsController(
            IBillReviewService billReviewService, 
            IBillRepository billRepository,
            Microsoft.Extensions.Logging.ILogger<BillsController> logger)
        {
            _billReviewService = billReviewService;
            _billRepository = billRepository;
            _logger = logger;
        }

        /// <summary>
        /// Lấy thông tin cấu hình môi trường hiển thị lên góc giao diện Web và các thiết lập debug
        /// </summary>
        [HttpGet("config/environment")]
        public IActionResult GetEnvironmentConfig([FromServices] ApplicationSetting appSetting)
        {
            return Ok(new
            {
                env = appSetting.Env,
                isProduction = appSetting.IsProduction,
                label = appSetting.Label,
                defaultFromDate = appSetting.DebugSettings?.DefaultFromDate,
                defaultToDate = appSetting.DebugSettings?.DefaultToDate
            });
        }

        /// <summary>
        /// [TASK UI-HEADER] Lấy danh mục thương hiệu (Brands) và gian hàng (Outlets) cho dropdown bộ lọc
        /// </summary>
        [HttpGet("filter-options")]
        public async Task<IActionResult> GetFilterOptions()
        {
            var options = await _billReviewService.GetFilterOptionsAsync();
            return Ok(options);
        }

        /// <summary>
        /// [TASK UI-HEADER & UI-TAB1] Truy vấn danh sách hóa đơn phân trang với đầy đủ bộ lọc
        /// </summary>
        [HttpGet("paged")]
        [HttpGet("pending-upload")]
        public async Task<IActionResult> GetBillsPaged(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] string? brandNumber,
            [FromQuery] string? outletNumber,
            [FromQuery] string? keyword,
            [FromQuery] bool? ocrStatus,
            [FromQuery] bool? reviewed,
            [FromQuery] bool? approved,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] int? limit = null)
        {
            int effectivePageSize = limit.HasValue && limit.Value > 0 ? limit.Value : pageSize;
            var pagedResult = await _billReviewService.GetBillsPagedAsync(
                fromDate, toDate, brandNumber, outletNumber, keyword, ocrStatus, reviewed, approved, page, effectivePageSize);
            return Ok(pagedResult);
        }

        /// <summary>
        /// [BƯỚC 3] Lấy danh sách các hóa đơn đã OCR xong chờ kiểm toán viên thẩm định (OCRStatus = 1)
        /// </summary>
        [HttpGet("review-queue")]
        public async Task<IActionResult> GetBillsForReview([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] int limit = 100)
        {
            var bills = await _billReviewService.GetBillsForReviewAsync(fromDate, toDate, limit);
            return Ok(bills);
        }

        /// <summary>
        /// [BƯỚC 2.1 & UI-TAB1] Gọi máy chủ OCR chuyên dụng bóc tách ảnh cho một hóa đơn cụ thể (Nút [⚡ OCR] trên giao diện)
        /// </summary>
        [HttpPost("{id:guid}/ocr")]
        public async Task<IActionResult> TriggerOcrForBill(Guid id)
        {
            var result = await _billReviewService.PerformOcrAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// [BƯỚC 2.1 & UI-TAB1] Lưu kết quả bóc tách OCR vào bảng mssOCRTrialTrans và đặt OCRStatus = 1 (Nút [💾 Save OCR])
        /// </summary>
        [HttpPost("{id:guid}/save-ocr")]
        public async Task<IActionResult> SaveOcrResult(Guid id, [FromBody] SaveOcrResultRequest request)
        {
            var result = await _billReviewService.SaveOcrResultAsync(id, request);
            return Ok(result);
        }

        /// <summary>
        /// Phục vụ ảnh hóa đơn trực tiếp cho thẻ <img> trên giao diện Web UI
        /// </summary>
        [HttpGet("{id:guid}/image")]
        public async Task<IActionResult> GetBillImage(Guid id)
        {
            var imageBytes = await _billReviewService.GetBillImageBytesAsync(id);
            if (imageBytes == null || imageBytes.Length == 0)
            {
                return NotFound(new { message = "Không tìm thấy file ảnh hóa đơn." });
            }

            return File(imageBytes, "image/png");
        }

        /// <summary>
        /// [NÚT SAVE BILL ẢNH GIAO DIỆN] Ghi đè trực tiếp tệp ảnh từ màn hình giao diện (sau khi xoay) xuống đĩa cứng
        /// </summary>
        [HttpPost("{id:guid}/save-image")]
        public async Task<IActionResult> SaveBillImage(Guid id)
        {
            byte[]? imageBytes = null;

            if (Request.HasFormContentType && Request.Form.Files.Count > 0)
            {
                var file = Request.Form.Files[0];
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                imageBytes = ms.ToArray();
            }
            else if (Request.ContentType != null && Request.ContentType.Contains("json"))
            {
                using var reader = new StreamReader(Request.Body);
                var jsonStr = await reader.ReadToEndAsync();
                var jObj = System.Text.Json.JsonDocument.Parse(jsonStr);
                if (jObj.RootElement.TryGetProperty("imageBase64", out var base64El))
                {
                    string rawBase64 = base64El.GetString() ?? "";
                    if (rawBase64.Contains(",")) rawBase64 = rawBase64.Substring(rawBase64.IndexOf(",") + 1);
                    imageBytes = Convert.FromBase64String(rawBase64);
                }
            }
            else
            {
                using var ms = new MemoryStream();
                await Request.Body.CopyToAsync(ms);
                if (ms.Length > 0) imageBytes = ms.ToArray();
            }

            if (imageBytes == null || imageBytes.Length == 0)
            {
                return BadRequest(new { success = false, message = "Không nhận được dữ liệu file ảnh hợp lệ." });
            }

            bool ok = await _billReviewService.OverwriteBillImageAsync(id, imageBytes);
            return Ok(new { success = ok, message = ok ? "Đã ghi đè file ảnh xuống đĩa cứng thành công!" : "Ghi đè file ảnh thất bại." });
        }

        /// <summary>
        /// [TIỆN ÍCH THỬ NGHIỆM] Tự động nạp các bill mẫu có sẵn từ thư mục test vào CSDL để thử nghiệm giao diện ngay
        /// </summary>
        [HttpPost("seed-sample-bills")]
        public async Task<IActionResult> SeedSampleBills()
        {
            string testDir = @"D:\GGDrive\TEST_IMAGES";
            if (!Directory.Exists(testDir))
            {
                return BadRequest(new { message = @"Không tìm thấy thư mục ảnh mẫu D:\GGDrive\TEST_IMAGES" });
            }

            var files = Directory.GetFiles(testDir, "*.png");
            int inserted = 0;

            foreach (var file in files)
            {
                if (inserted >= 5) break;
                string filename = Path.GetFileName(file);
                string memId = filename.Split('_')[0];

                var newBill = new OcrTrialTrans
                {
                    ID = Guid.NewGuid(),
                    MemID = memId,
                    UploadImageDateTime = DateTime.Now.AddMinutes(-new Random().Next(5, 120)),
                    ImagePath = "Transaction/" + DateTime.Now.ToString("ddMMyy"),
                    ImageFileName = filename,
                    DataStatus = "Pending",
                    OCRStatus = false,
                    Reviewed = false
                };

                bool ok = await _billRepository.InsertAsync(newBill);
                if (ok) inserted++;
            }

            return Ok(new { success = true, count = inserted, message = $"Đã nạp thành công {inserted} hóa đơn mẫu vào CSDL." });
        }

        /// <summary>
        /// [BƯỚC 3] Lấy danh sách các hóa đơn đã có kết quả OCR chờ kiểm toán viên đối soát duyệt
        /// </summary>
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingBills([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] int limit = 100)
        {
            var bills = await _billReviewService.GetPendingBillsAsync(fromDate, toDate, limit);
            return Ok(bills);
        }

        /// <summary>
        /// Lấy chi tiết một hóa đơn theo mã định danh
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetBillById(Guid id)
        {
            var bill = await _billReviewService.GetBillByIdAsync(id);
            if (bill == null) return NotFound(new { message = "Không tìm thấy hóa đơn." });
            return Ok(bill);
        }

        /// <summary>
        /// Phê duyệt hóa đơn (Kiểm toán viên đối soát và xác nhận)
        /// </summary>
        [HttpPost("approve")]
        public async Task<IActionResult> ApproveBill([FromBody] ApproveBillRequest request)
        {
            try
            {
                var result = await _billReviewService.ApproveBillAsync(request);
                if (!result.Success) return BadRequest(result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý phê duyệt hóa đơn {BillId}", request.BillId);
                return StatusCode(500, new { success = false, message = "Lỗi máy chủ khi phê duyệt: " + ex.Message });
            }
        }

        /// <summary>
        /// Từ chối hóa đơn và phản hồi lý do đến khách hàng
        /// </summary>
        [HttpPost("reject")]
        public async Task<IActionResult> RejectBill([FromBody] RejectBillRequest request)
        {
            try
            {
                var result = await _billReviewService.RejectBillAsync(request);
                if (!result.Success) return BadRequest(result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý từ chối hóa đơn {BillId}", request.BillId);
                return StatusCode(500, new { success = false, message = "Lỗi máy chủ khi từ chối: " + ex.Message });
            }
        }

        /// <summary>
        /// Webhook tiếp nhận kết quả quét hóa đơn từ Server OCR chuyên dụng
        /// </summary>
        [HttpPost("webhook/ocr-result")]
        public async Task<IActionResult> ReceiveOcrResult([FromBody] OcrWebhookRequest request)
        {
            var billId = await _billReviewService.ReceiveOcrScanResultAsync(request);
            return Ok(new { success = true, billId, message = "Đã tiếp nhận hóa đơn vào hàng đợi duyệt." });
        }

        /// <summary>
        /// Kích hoạt tiến trình quét và tự động duyệt theo lô (Dành cho CheckPoint Framework gọi định kỳ)
        /// </summary>
        [HttpPost("auto-review")]
        public async Task<IActionResult> RunAutoReviewBatch([FromQuery] int maxBatch = 50)
        {
            var summary = await _billReviewService.RunAutoApprovalBatchAsync(maxBatch);
            return Ok(summary);
        }
    }
}
