using System;

namespace CRMMall.BillReview.BLL.DTOs
{
    public class PendingBillDto
    {
        public Guid ID { get; set; }
        public string MemID { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string? CustomerMobile { get; set; }
        public string? CustomerStatus { get; set; }
        public string? CurrentTier { get; set; }

        public DateTime UploadImageDateTime { get; set; }
        public string? ImagePath { get; set; }
        public string? ImageFileName { get; set; }

        // Trích xuất ban đầu từ Server OCR
        public string? ScannedOutletNumber { get; set; }
        public string? ScannedOutletName { get; set; }
        public string? ScannedBrandNumber { get; set; }
        public string? ScannedBillNumber { get; set; }
        public string? ScannedBillDateTime { get; set; }
        public string? ScannedBillTotal { get; set; }
        public string? CashierName { get; set; }

        // Trạng thái gợi ý tự duyệt
        public bool IsAutoApprovable { get; set; }
        public string? AutoApproveReason { get; set; }
    }

    public class ApproveBillRequest
    {
        public Guid BillId { get; set; }

        // Dữ liệu Admin THỰC SỰ gõ vào (Nếu không gõ thì là null, tuyệt đối không lấy số OCR đè vào đây)
        public string? BillNumberA { get; set; }
        public decimal? BillTotalA { get; set; }
        public DateTime? BillDateTimeA { get; set; }
        public string? OutletNumberA { get; set; }
        public string? BrandNumberA { get; set; }

        // Dữ liệu Final chốt duyệt (kết hợp Admin và OCR để phục vụ validation và logic sau này)
        public string ApprovedBillNumber { get; set; } = string.Empty;
        public decimal ApprovedTotalAmount { get; set; }
        public DateTime ApprovedBillDateTime { get; set; }
        public string ApprovedOutletNumber { get; set; } = string.Empty;
        public string? ApprovedBrandNumber { get; set; }

        public string ReviewBy { get; set; } = "Operator";
        public string? ReviewNotes { get; set; }
    }

    public class RejectBillRequest
    {
        public Guid BillId { get; set; }
        public string ReviewBy { get; set; } = "Operator";
        public string Reason { get; set; } = string.Empty;
    }

    public class LotteryTicketResult
    {
        public int ProgramId { get; set; }
        public string ProgramName { get; set; } = string.Empty;
        public string LotteryCode { get; set; } = string.Empty;
        public string? GiftName { get; set; }
        public decimal? GiftValue { get; set; }
        public DateTime GetCodeDate { get; set; }
    }

    public class PromotionGiftResult
    {
        public int ProgramId { get; set; }
        public string ProgramName { get; set; } = string.Empty;
        public string PromotionCode { get; set; } = string.Empty;
        public string? GiftName { get; set; }
        public string? ItemType { get; set; }
        public DateTime? ExpireDate { get; set; }
        public DateTime GetCodeDate { get; set; }
    }

    public class BillReviewResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal PointsAwarded { get; set; }      // Điểm thưởng (Reward Points / getpoints)
        public decimal RankPointsAwarded { get; set; }  // Điểm xếp hạng (Review Points / reviewpoints)
        public decimal RewardRate { get; set; }         // Tỷ lệ điểm thưởng áp dụng
        public decimal ReviewRate { get; set; }         // Tỷ lệ điểm xếp hạng áp dụng
        public bool IsUpgraded { get; set; }
        public string? NewTier { get; set; }
        
        // Kết quả phân phối vé số may mắn & quà khuyến mãi
        public string? LotteryCode { get; set; }
        public LotteryTicketResult? LotteryAwarded { get; set; }
        public string? PromotionCode { get; set; }
        public PromotionGiftResult? PromotionAwarded { get; set; }
    }

    public class OcrWebhookRequest
    {
        public string MemID { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
        public string? ImageFileName { get; set; }
        public string? OutletNumber { get; set; }
        public string? BrandNumber { get; set; }
        public string? OutletName { get; set; }
        public string? BrandName { get; set; }
        public string? BillNumber { get; set; }
        public string? BillDateTime { get; set; }
        public string? BillTotal { get; set; }
        public string? CashierName { get; set; }
    }

    public class AutoReviewSummary
    {
        public int TotalEvaluated { get; set; }
        public int TotalAutoApproved { get; set; }
        public int TotalManualRequired { get; set; }
        public string Details { get; set; } = string.Empty;
    }
}
