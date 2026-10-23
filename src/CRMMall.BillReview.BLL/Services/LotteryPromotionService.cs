using System;
using System.Threading.Tasks;
using CRMMall.BillReview.BLL.DTOs;
using CRMMall.BillReview.DAL.Repositories;

namespace CRMMall.BillReview.BLL.Services
{
    /// <summary>
    /// Triển khai nghiệp vụ đánh giá và phân phối vé số may mắn và quà tặng khuyến mãi
    /// </summary>
    public class LotteryPromotionService : ILotteryPromotionService
    {
        private readonly ILoyaltyRepository _loyaltyRepository;

        public LotteryPromotionService(ILoyaltyRepository loyaltyRepository)
        {
            _loyaltyRepository = loyaltyRepository;
        }

        /// <summary>
        /// Đánh giá điều kiện chiến dịch quay số may mắn (Lucky Draw) và bốc thăm phát hành vé dự thưởng.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Động cơ phân phối vé số may mắn tự động (Lottery Distribution Engine).
        /// - MỤC ĐÍCH: 
        ///   1. Tìm kiếm chương trình quay số (mssLotteryProgram, Type = 'Lottery') thỏa mãn khung giờ, ngày gia nhập, ngưỡng bill tối thiểu và phạm vi cửa hàng (G_Scope).
        ///   2. Kiểm soát hạn ngạch 3 tầng trên mssLotteryGift (Hạn ngạch toàn chiến dịch, Hạn ngạch ngày của hội viên, Hạn ngạch ngày của chương trình).
        ///   3. Bốc thăm ngẫu nhiên có trọng số (Weighted Random) và gán vé số cho hội viên.
        /// - CÁCH DÙNG: Được gọi trong chuỗi duyệt hóa đơn hợp lệ hoặc các chương trình quay số may mắn sau thanh toán tại TTTM.
        /// </remarks>
        /// <param name="memId">Mã định danh hội viên.</param>
        /// <param name="joinDate">Ngày gia nhập hội viên (customer.DateJoined).</param>
        /// <param name="billNo">Số hóa đơn hợp lệ.</param>
        /// <param name="transDate">Ngày phát sinh giao dịch trên hóa đơn.</param>
        /// <param name="billTotal">Tổng số tiền hóa đơn.</param>
        /// <param name="outletNumber">Mã gian hàng phát sinh hóa đơn.</param>
        /// <returns><see cref="LotteryTicketResult"/> nếu đủ điều kiện và còn vé, hoặc null nếu không thỏa điều kiện.</returns>
        public async Task<LotteryTicketResult?> EvaluateAndIssueLotteryAsync(
            string memId,
            DateTime? joinDate,
            string billNo,
            DateTime transDate,
            decimal billTotal,
            string outletNumber)
        {
            // 1. Thẩm định chương trình quay số hợp lệ thỏa mãn điều kiện & hạn ngạch 3 tầng
            var eligibleProgram = await _loyaltyRepository.GetEligibleLotteryProgramAsync(
                billTotal,
                transDate,
                joinDate,
                memId,
                outletNumber);

            if (eligibleProgram == null) return null;

            // 2. Bốc thăm ngẫu nhiên và phát hành vé số may mắn
            var ticket = await _loyaltyRepository.IssueLotteryTicketAsync(
                eligibleProgram,
                memId,
                billNo,
                transDate,
                billTotal);

            if (ticket == null) return null;

            return new LotteryTicketResult
            {
                ProgramId = eligibleProgram.ID,
                ProgramName = eligibleProgram.Name,
                LotteryCode = ticket.LotteryCode,
                GiftName = ticket.Name,
                GiftValue = ticket.GiftValue,
                GetCodeDate = ticket.GetCodeDate ?? DateTime.Now
            };
        }

        /// <summary>
        /// Đánh giá điều kiện chương trình quà tặng khuyến mãi (FirstTrans / Gift) và bốc thăm phát voucher ưu đãi.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Động cơ phân phối quà tặng &amp; voucher ưu đãi (Promotion / Gift Engine).
        /// - MỤC ĐÍCH:
        ///   1. Ưu tiên chương trình FirstTrans (quà cho giao dịch đầu tiên). Nếu hội viên đã từng nhận quà thì tự động fallback sang chương trình Gift thông thường.
        ///   2. Kiểm tra điều kiện thời gian, ngưỡng bill, phạm vi cửa hàng và hạn ngạch 3 tầng trên mssPromotionGift.
        ///   3. Bốc thăm ngẫu nhiên và gán mã voucher, tính hạn sử dụng (TakeExpiredDay).
        /// - CÁCH DÙNG: Được gọi khi duyệt bill mua sắm hoặc khi khách hàng tham gia các chương trình đổi quà/nhận quà tại TTTM.
        /// </remarks>
        /// <param name="memId">Mã định danh hội viên.</param>
        /// <param name="billNo">Số hóa đơn.</param>
        /// <param name="transDate">Ngày giao dịch.</param>
        /// <param name="billTotal">Số tiền giao dịch.</param>
        /// <param name="outletNumber">Mã gian hàng.</param>
        /// <returns><see cref="PromotionGiftResult"/> nếu phát quà thành công, hoặc null nếu không đủ điều kiện.</returns>
        public async Task<PromotionGiftResult?> EvaluateAndIssuePromotionAsync(
            string memId,
            string billNo,
            DateTime transDate,
            decimal billTotal,
            string outletNumber)
        {
            // 1. Thẩm định chương trình khuyến mãi (ưu tiên FirstTrans nếu chưa từng có quà, fallback sang Gift)
            var eligibleProgram = await _loyaltyRepository.GetEligiblePromotionProgramAsync(
                billTotal,
                transDate,
                memId,
                outletNumber);

            if (eligibleProgram == null) return null;

            // 2. Bốc thăm ngẫu nhiên và phát hành quà tặng / voucher khuyến mãi
            var gift = await _loyaltyRepository.IssuePromotionGiftAsync(
                eligibleProgram,
                memId,
                billNo,
                transDate,
                billTotal);

            if (gift == null) return null;

            return new PromotionGiftResult
            {
                ProgramId = eligibleProgram.ID,
                ProgramName = eligibleProgram.Name,
                PromotionCode = gift.PromotionCode,
                GiftName = gift.Name,
                ItemType = gift.ItemType,
                ExpireDate = gift.ToDate,
                GetCodeDate = gift.GetCodeDate ?? DateTime.Now
            };
        }
    }
}
