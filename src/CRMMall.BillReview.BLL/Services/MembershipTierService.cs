using System.Linq;
using System.Threading.Tasks;
using CRMMall.BillReview.BLL.DTOs;
using CRMMall.BillReview.DAL.Repositories;

namespace CRMMall.BillReview.BLL.Services
{
    /// <summary>
    /// Triển khai nghiệp vụ thẩm định và tự động nâng hạng thẻ thành viên
    /// </summary>
    public class MembershipTierService : IMembershipTierService
    {
        private readonly ILoyaltyRepository _loyaltyRepository;

        public MembershipTierService(ILoyaltyRepository loyaltyRepository)
        {
            _loyaltyRepository = loyaltyRepository;
        }

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
        public async Task<TierUpgradeResult> EvaluateAndUpgradeTierAsync(string memId, string? billNo = null)
        {
            var activeClass = await _loyaltyRepository.GetActiveMemberClassAsync(memId);
            if (activeClass == null)
            {
                return new TierUpgradeResult
                {
                    IsUpgraded = false,
                    Message = $"Không tìm thấy sổ cái xếp hạng của hội viên {memId}."
                };
            }

            var ranges = await _loyaltyRepository.GetMemberRangesAsync();
            var clmLedger = await _loyaltyRepository.GetClmLedgerAsync(memId);

            // Điểm thẩm định nâng hạng: CloseAmt trên mssMemClassLedger hoặc AccPoints trên mssCLMLedger
            decimal evalPoints = activeClass.CloseAmt > 0 ? activeClass.CloseAmt : (clmLedger?.AccPoints ?? 0);

            var targetRange = ranges.FirstOrDefault(r => 
                r.ClassNo == activeClass.ClassNo && 
                evalPoints >= r.FromAmt);

            if (targetRange != null && !string.IsNullOrWhiteSpace(targetRange.UpgradeClass))
            {
                var newClassInfo = await _loyaltyRepository.GetMemberClassAsync(targetRange.UpgradeClass);
                decimal bonusPoints = newClassInfo?.RewardRate ?? 0;

                string note = string.IsNullOrWhiteSpace(billNo)
                    ? $"Tự động thăng hạng từ {activeClass.ClassNo} lên {targetRange.UpgradeClass} (Điểm tích lũy: {evalPoints:N0})"
                    : $"Tự động thăng hạng từ {activeClass.ClassNo} lên {targetRange.UpgradeClass} sau khi duyệt hóa đơn {billNo}";

                await _loyaltyRepository.UpgradeMemberClassAsync(
                    memId,
                    targetRange.UpgradeClass,
                    bonusPoints,
                    note);

                return new TierUpgradeResult
                {
                    IsUpgraded = true,
                    OldTier = activeClass.ClassNo,
                    NewTier = targetRange.UpgradeClass,
                    BonusPointsAwarded = bonusPoints,
                    Message = $"Chúc mừng hội viên {memId} được nâng hạng từ {activeClass.ClassNo} lên {targetRange.UpgradeClass}!"
                };
            }

            return new TierUpgradeResult
            {
                IsUpgraded = false,
                OldTier = activeClass.ClassNo,
                NewTier = activeClass.ClassNo,
                BonusPointsAwarded = 0,
                Message = $"Hội viên {memId} duy trì hạng {activeClass.ClassNo} (Điểm hiện tại: {evalPoints:N0})."
            };
        }
    }
}
