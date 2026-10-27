using System;

namespace CRMMall.BillReview.DAL.Entities
{
    /// <summary>
    /// Bản ghi giao dịch hóa đơn OCR (bảng mssOCRTrialTrans).
    /// Dữ liệu đầu vào ban đầu do Server OCR chuyên dụng bóc tách.
    /// </summary>
    public class OcrTrialTrans
    {
        public Guid ID { get; set; } = Guid.NewGuid();
        public string MemID { get; set; } = string.Empty;
        public DateTime UploadImageDateTime { get; set; } = DateTime.Now;
        public string? ImagePath { get; set; }
        public string? ImageFileName { get; set; }

        // Dữ liệu bóc tách ban đầu do Server OCR gửi sang
        public string? OutletNumber { get; set; }
        public string? BrandNumber { get; set; }
        public string? OutletAddress { get; set; }
        public string? BillNumber { get; set; }
        public string? BillDateTime { get; set; }
        public string? BillDateTimeFormat { get; set; }
        public string? BillTotal { get; set; }
        public string? CashierName { get; set; }
        public DateTime? ScanDate { get; set; }

        // Trạng thái đối soát & duyệt
        public DateTime? ReviewDate { get; set; }
        public string? ReviewBy { get; set; }
        public Guid? TransDocID { get; set; }
        public bool? Approved { get; set; } // null: Chờ duyệt, true: Đã duyệt, false: Từ chối
        public decimal? PointGain { get; set; }
        public bool? Reviewed { get; set; }
        public string? ReviewNotes { get; set; }
        public string? DataStatus { get; set; }
        public bool? OCRStatus { get; set; }

        // Giá trị phê duyệt cuối cùng (Final Approved Values)
        public string? BillNumberA { get; set; }
        public DateTime? BillDateTimeA { get; set; }
        public decimal? BillTotalA { get; set; }
        public string? BrandNumberA { get; set; }
        public string? OutletNumberA { get; set; }

        // Thông tin gian hàng bổ sung
        public string? OutletName { get; set; }
        public string? BrandName { get; set; }
        public string? FloorName { get; set; }
        public string? MallInfo { get; set; }
    }
}
