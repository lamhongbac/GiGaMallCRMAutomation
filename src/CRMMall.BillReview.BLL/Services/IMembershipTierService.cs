using System.Threading.Tasks;
using CRMMall.BillReview.BLL.DTOs;

namespace CRMMall.BillReview.BLL.Services
{
    /// <summary>
    /// Thư viện logic nghiệp vụ thẩm định và tự động nâng hạng thẻ thành viên (Membership Tier / UpClass Engine)
    /// </summary>
    public interface IMembershipTierService
    {
        /// <summary>
        /// Thẩm định điểm tích lũy xếp hạng hiện tại và thực thi nâng hạng thẻ nguyên tử nếu hội viên vượt ngưỡng quy định.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Động cơ thăng hạng tự động (Automated Tier Progression Engine).
        /// - MỤC ĐÍCH: Đối chiếu điểm xếp hạng chốt lũy kế (CloseAmt trên mssMemClassLedger hoặc AccPoints trên mssCLMLedger) với bảng dải thăng hạng G_MemRangeClass. Nếu đạt điều kiện: đóng hạng cũ (Status = 'C'), mở hạng mới (Status = 'O'), và cộng điểm thưởng nâng hạng (RewardRate) vào ví và bảng mssCLMGiftPoint.
        /// - CÁCH DÙNG: Gọi ngay sau khi giao dịch tích điểm bán hàng hoàn tất trong BillReviewService, hoặc gọi từ tiến trình Worker quét định kỳ kiểm tra thăng hạng toàn bộ hội viên cuối kỳ.
        /// </remarks>
        /// <param name="memId">Mã định danh duy nhất của hội viên cần xét nâng hạng.</param>
        /// <param name="billNo">Số hóa đơn kích hoạt việc xét nâng hạng (tùy chọn, dùng để ghi chú lịch sử thăng hạng).</param>
        /// <returns>Đối tượng <see cref="TierUpgradeResult"/> thông báo có thăng hạng hay không, hạng cũ, hạng mới và số điểm thưởng thăng hạng được tặng.</returns>
        Task<TierUpgradeResult> EvaluateAndUpgradeTierAsync(string memId, string? billNo = null);
    }
}
