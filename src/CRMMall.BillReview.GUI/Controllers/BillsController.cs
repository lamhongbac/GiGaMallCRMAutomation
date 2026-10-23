using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CRMMall.BillReview.BLL.DTOs;
using CRMMall.BillReview.BLL.Services;

namespace CRMMall.BillReview.GUI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BillsController : ControllerBase
    {
        private readonly IBillReviewService _billReviewService;

        public BillsController(IBillReviewService billReviewService)
        {
            _billReviewService = billReviewService;
        }

        /// <summary>
        /// Lấy danh sách các hóa đơn chờ duyệt (kèm gợi ý tự duyệt từ Rule Engine)
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
            var result = await _billReviewService.ApproveBillAsync(request);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Từ chối hóa đơn và phản hồi lý do đến khách hàng
        /// </summary>
        [HttpPost("reject")]
        public async Task<IActionResult> RejectBill([FromBody] RejectBillRequest request)
        {
            var result = await _billReviewService.RejectBillAsync(request);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
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
