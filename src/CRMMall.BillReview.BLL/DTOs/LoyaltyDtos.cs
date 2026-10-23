using System;

namespace CRMMall.BillReview.BLL.DTOs
{
    /// <summary>
    /// Kết quả tính toán điểm thưởng và điểm xếp hạng
    /// </summary>
    public class PointsCalculationResult
    {
        public decimal RewardPoints { get; set; }     // Điểm thưởng kiếm được (getpoints)
        public decimal RankingPoints { get; set; }    // Điểm xếp hạng kiếm được (reviewpoints)
        public decimal RewardRate { get; set; }       // Tỷ lệ điểm thưởng (VND/điểm)
        public decimal RankingRate { get; set; }      // Tỷ lệ điểm xếp hạng (VND/điểm)
    }

    /// <summary>
    /// Kết quả xử lý giao dịch bán hàng và ghi sổ đa tầng
    /// </summary>
    public class PointsAccrualResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal PointsAwarded { get; set; }     // Điểm thưởng (Reward Points)
        public decimal RankPointsAwarded { get; set; } // Điểm xếp hạng (Ranking Points)
        public decimal RewardRate { get; set; }
        public decimal ReviewRate { get; set; }
        public Guid? CardTransDocID { get; set; }
    }

    /// <summary>
    /// Kết quả đánh giá và nâng hạng thành viên
    /// </summary>
    public class TierUpgradeResult
    {
        public bool IsUpgraded { get; set; }
        public string? OldTier { get; set; }
        public string? NewTier { get; set; }
        public decimal BonusPointsAwarded { get; set; }
        public string? Message { get; set; }
    }
}
