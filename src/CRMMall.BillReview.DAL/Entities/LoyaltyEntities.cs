using System;

namespace CRMMall.BillReview.DAL.Entities
{
    /// <summary>
    /// Sổ cái điểm và doanh số thành viên (bảng mssCLMLedger)
    /// </summary>
    public class ClmLedgerRecord
    {
        public int ID { get; set; }
        public int CLPID { get; set; } = 1;
        public string MemID { get; set; } = string.Empty;
        public decimal AccSales { get; set; }     // Doanh số tích lũy lũy kế
        public decimal AccPoints { get; set; }    // Tổng điểm thưởng tích lũy kiếm được lũy kế (Earned Points)
        public decimal RedeemPoints { get; set; } // Tổng điểm đã tiêu / đổi quà / quay thưởng lũy kế (Redeemed Points)
        public int? AccTC { get; set; }           // Tổng số lần giao dịch phát sinh
        public DateTime? LastSaleDate { get; set; } // Ngày giao dịch gần nhất
        public DateTime? CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }

        /// <summary>
        /// Số dư điểm thưởng khả dụng có thể dùng để đổi quà/voucher:
        /// RewardBalance = AccPoints - RedeemPoints
        /// </summary>
        public decimal RewardBalance => AccPoints - RedeemPoints;
    }

    /// <summary>
    /// Danh mục hạng thẻ thành viên (bảng G_MemberClass)
    /// </summary>
    public class MemberClassInfo
    {
        public int ID { get; set; }
        public int CLPID { get; set; } = 1;
        public string ClassNo { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public int ClassOrder { get; set; }
        public decimal RewardRate { get; set; } // Tỷ lệ quy đổi điểm thưởng (VND/điểm)
        public decimal ReviewRate { get; set; } // Tỷ lệ quy đổi điểm xếp hạng/thăng hạng (VND/điểm)
        public bool IsMember { get; set; } = true;
    }

    /// <summary>
    /// Cặp tỷ lệ điểm thưởng và điểm xếp hạng thu được từ chính sách hội viên
    /// </summary>
    public class BasePointRate
    {
        public decimal RewardPointRate { get; set; } = 10000m;
        public decimal RankingPointRate { get; set; } = 10000m;
    }

    /// <summary>
    /// Cấu hình ngưỡng thăng hạng thành viên (bảng G_MemRangeClass)
    /// </summary>
    public class MemberClassRange
    {
        public int ID { get; set; }
        public int CLPID { get; set; } = 1;
        public string ClassNo { get; set; } = string.Empty;
        public decimal FromAmt { get; set; }
        public decimal ToAmt { get; set; }
        public string UpgradeClass { get; set; } = string.Empty;
        public int ValidMonths { get; set; } = 12;
    }

    /// <summary>
    /// Sổ cái xếp hạng thành viên (bảng mssMemClassLedger)
    /// </summary>
    public class MemberClassLedgerRecord
    {
        public int ID { get; set; }
        public DateTime EventDate { get; set; } = DateTime.Now;
        public int CLPID { get; set; } = 1;
        public string MemID { get; set; } = string.Empty;
        public string ClassNo { get; set; } = string.Empty;
        public decimal OpenAmt { get; set; }
        public decimal DebitAmt { get; set; }     // Điểm xếp hạng phát sinh tăng (+reviewpoints)
        public decimal CreditAmt { get; set; }    // Điểm xếp hạng giảm trừ (-reviewpoints)
        public decimal CloseAmt { get; set; }     // Điểm chốt = OpenAmt + DebitAmt - CreditAmt
        public string Unit { get; set; } = "P";
        public string? Notes { get; set; }
        public string DocType { get; set; } = "S";       // 'S' = Sale, 'R' = Review/UpClass
        public string ReviewFlag { get; set; } = "N";    // 'N' = Chưa chạy UpClass, 'Y' = Đã chạy
        public string Status { get; set; } = "O";        // 'O' = Đang mở / Active, 'C' = Đã đóng
        public Guid? LastSaleDocID { get; set; }
        public string CreatedBy { get; set; } = "system";
        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
    }

    /// <summary>
    /// Sổ cái ghi nhận chi tiết giao dịch mua sắm / quẹt thẻ (bảng mssCardTrans)
    /// </summary>
    public class CardTransRecord
    {
        public Guid DocID { get; set; } = Guid.NewGuid();
        public int CLPID { get; set; } = 1;
        public string MemID { get; set; } = string.Empty;
        public string? CardNo { get; set; }
        public string ClassNo { get; set; } = string.Empty;
        public Guid? OrderID { get; set; } // Tham chiếu tới mssOCRTrialTrans.ID
        public string BillNo { get; set; } = string.Empty;
        public string CheckNo { get; set; } = string.Empty;
        public string OutletNumber { get; set; } = string.Empty;
        public DateTime TransDate { get; set; }
        public DateTime POSBusinessDate { get; set; }
        public int TransType { get; set; } = 1; // 1 = Sale
        public decimal SubTotal { get; set; }
        public decimal TotalBillValue { get; set; }
        public decimal RPointGain { get; set; }       // Điểm thưởng nhận được (getpoints)
        public decimal RPointRate { get; set; }       // Tỷ lệ điểm thưởng áp dụng
        public decimal ReviewPointGain { get; set; }  // Điểm xếp hạng nhận được (reviewpoints)
        public decimal ReviewPointRate { get; set; }  // Tỷ lệ điểm xếp hạng áp dụng
        public string? CashierName { get; set; }
        public bool UseMobileApp { get; set; } = true;
        public string BProcess { get; set; } = "Y";
        public string? Notes { get; set; }
        public string CreatedBy { get; set; } = "Operator";
        public DateTime CreatedOn { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Chương trình quay số may mắn & quà tặng khuyến mãi (bảng mssLotteryProgram)
    /// </summary>
    public class LotteryProgramInfo
    {
        public int ID { get; set; }
        public string Type { get; set; } = "Lottery"; // Lottery, FirstTrans, Gift
        public string? Number { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? ExtendExpiredDay { get; set; }
        public decimal? MinBillValue { get; set; }
        public int? ScopeID { get; set; }
        public DateTime? JoinFrom { get; set; }
        public DateTime? JoinTo { get; set; }
        public int? MaxLotteryCount { get; set; }
        public int? MaxLotteryMemberCount { get; set; }
        public int? MaxLotteryCountByDay { get; set; }
        public int? MaxLotteryMemberCountByDay { get; set; }
        public decimal? RankPoint { get; set; }
        public decimal? RewardPoint { get; set; }
        public int? TakeExpiredDay { get; set; }
        public bool? TakeByBillDate { get; set; }
    }

    /// <summary>
    /// Bản ghi kho vé quay số may mắn (bảng mssLotteryGift)
    /// </summary>
    public class LotteryGiftRecord
    {
        public Guid ID { get; set; } = Guid.NewGuid();
        public int? ProgramID { get; set; }
        public string LotteryCode { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? ItemCode { get; set; }
        public string? ItemType { get; set; }
        public string? MemID { get; set; }
        public string? BillNo { get; set; }
        public DateTime? TransDate { get; set; }
        public decimal? SubTotal { get; set; }
        public DateTime? GetCodeDate { get; set; }
        public decimal? GiftValue { get; set; }
        public bool? IsEnable { get; set; } = true;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    /// <summary>
    /// Bản ghi kho quà tặng khuyến mãi / Voucher ưu đãi (bảng mssPromotionGift)
    /// </summary>
    public class PromotionGiftRecord
    {
        public Guid ID { get; set; } = Guid.NewGuid();
        public int? ProgramID { get; set; }
        public string PromotionCode { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? ItemCode { get; set; }
        public string? ItemType { get; set; }
        public string? MemID { get; set; }
        public string? BillNo { get; set; }
        public DateTime? TransDate { get; set; }
        public decimal? SubTotal { get; set; }
        public DateTime? GetCodeDate { get; set; }
        public DateTime? ToDate { get; set; }
        public decimal? GiftValue { get; set; }
        public bool? IsEnable { get; set; } = true;
    }

    /// <summary>
    /// Cấu hình phạm vi gian hàng áp dụng (bảng G_Scope)
    /// </summary>
    public class ScopeInfo
    {
        public int ID { get; set; }
        public string? Number { get; set; }
        public string? Name { get; set; }
        public string? OutletList { get; set; } // Danh sách Outlet phân tách bằng dấu chấm phẩy ';'
    }

    /// <summary>
    /// Mẫu tin thông báo đẩy (bảng G_NotificationMessage)
    /// </summary>
    public class NotificationTemplate
    {
        public int ID { get; set; }
        public string Number { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
