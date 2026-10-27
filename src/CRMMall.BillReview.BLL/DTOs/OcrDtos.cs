using System;
using System.Collections.Generic;

namespace CRMMall.BillReview.BLL.DTOs
{
    /// <summary>
    /// DTO đại diện cho một hóa đơn chờ xử lý OCR (Bước 1 -> Bước 2.1) hoặc chờ duyệt (Bước 3)
    /// </summary>
    public class PendingUploadBillDto
    {
        public Guid ID { get; set; }
        public string MemID { get; set; } = string.Empty;
        public DateTime UploadImageDateTime { get; set; }
        public string? ImagePath { get; set; }
        public string? ImageFileName { get; set; }
        public string? ImageUrl { get; set; }
        public string DataStatus { get; set; } = "Pending";
        public bool? OCRStatus { get; set; }
        public bool? Reviewed { get; set; }
        public bool? Approved { get; set; }
        public bool? CRMProcess { get; set; }
        public bool HasOcrResult { get; set; }
        
        // Dữ liệu OCR đã có
        public string? ScannedOutletNumber { get; set; }
        public string? ScannedOutletName { get; set; }
        public string? ScannedBrandName { get; set; }
        public string? ScannedBillNumber { get; set; }
        public string? ScannedBillDateTime { get; set; }
        public string? ScannedBillTotal { get; set; }
        public string? ScannedCashierName { get; set; }
        public string? ResolvedOutlet { get; set; }
        public float? Confidence { get; set; }
        public double? LatencySeconds { get; set; }

        // Dữ liệu Admin Adjust (...A)
        public string? BillNumberA { get; set; }
        public DateTime? BillDateTimeA { get; set; }
        public decimal? BillTotalA { get; set; }
        public string? OutletNumberA { get; set; }
        public string? BrandNumberA { get; set; }
        public string? ReviewNotes { get; set; }
    }

    /// <summary>
    /// Phản hồi từ Server OCR chuyên dụng (FastAPI / Qwen2-VL) theo 04_CRM_API_INTEGRATION_SPEC.md
    /// </summary>
    public class OcrExtractApiResponse
    {
        public bool Success { get; set; }
        public int BillId { get; set; }
        public string Filename { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public bool IsDuplicate { get; set; }
        public string? DuplicateAction { get; set; }
        public string? ActionTaken { get; set; }
        public double LatencySeconds { get; set; }
        public OcrExtractedData? Data { get; set; }
        public string? Message { get; set; }
        public string? RawOutput { get; set; }
    }

    /// <summary>
    /// Chi tiết các trường dữ liệu AI bóc tách được từ hóa đơn
    /// </summary>
    public class OcrExtractedData
    {
        public string? BrandName { get; set; }
        public string? OutletName { get; set; }
        public string? OutletNumber { get; set; }
        public string? ResolvedOutlet { get; set; }
        public string? BillNumber { get; set; }
        public string? BillDatetime { get; set; }
        public decimal? BillTotal { get; set; }
        public bool IsValidBill { get; set; } = true;
        public float MatchScore { get; set; }
        public float Confidence { get; set; }
        public string? CashierName { get; set; }
        public string? ActionTaken { get; set; }
    }

    /// <summary>
    /// Yêu cầu lưu kết quả bóc tách OCR vào bảng mssOCRTrialTrans
    /// </summary>
    public class SaveOcrResultRequest
    {
        public string? BrandName { get; set; }
        public string? OutletName { get; set; }
        public string? OutletNumber { get; set; }
        public string? BrandNumber { get; set; }
        public string? BillNumber { get; set; }
        public string? BillDateTime { get; set; }
        public string? BillTotal { get; set; }
        public string? CashierName { get; set; }
        public string? RawOcrResponse { get; set; }
    }

    /// <summary>
    /// Kết quả xử lý lưu OCR
    /// </summary>
    public class SaveOcrResultResponse
    {
        public bool Success { get; set; }
        public Guid BillId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string DataStatus { get; set; } = "OCR";
        public DateTime ScanDate { get; set; }
    }

    /// <summary>
    /// Danh mục lựa chọn bộ lọc Header
    /// </summary>
    public class FilterOptionsDto
    {
        public List<string> Brands { get; set; } = new();
        public List<OutletOptionDto> Outlets { get; set; } = new();
    }

    public class OutletOptionDto
    {
        public string OutletNumber { get; set; } = string.Empty;
        public string OutletName { get; set; } = string.Empty;
        public string? BrandNumber { get; set; }
    }

    /// <summary>
    /// Kết quả danh sách phân trang (Server-side Paged Result)
    /// </summary>
    public class PagedResultDto<T>
    {
        public IEnumerable<T> Items { get; set; } = System.Linq.Enumerable.Empty<T>();
        public int TotalCount { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / (double)PageSize) : 0;
    }
}
