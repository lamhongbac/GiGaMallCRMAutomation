using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CRMMall.BillReview.DAL.Entities;

namespace CRMMall.BillReview.DAL.Repositories
{
    /// <summary>
    /// Kho lưu trữ dữ liệu nghiệp vụ khách hàng thân thiết, đa sổ cái và chiến dịch khuyến mãi (Loyalty Data Access)
    /// </summary>
    public interface ILoyaltyRepository
    {
        /// <summary>
        /// Lấy thông tin ví điểm tích lũy và tổng chi tiêu của hội viên từ sổ cái mssCLMLedger.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Truy xuất ví điểm chính của hội viên (Member Points Balance Reader).
        /// - MỤC ĐÍCH: Lấy AccPoints (tổng điểm kiếm được), RedeemPoints (tổng điểm đã tiêu), AccSales (doanh số) và tính RewardBalance khả dụng.
        /// - CÁCH DÙNG: Gọi khi kiểm tra số dư điểm hoặc hiển thị thông tin ví hội viên.
        /// </remarks>
        /// <param name="memId">Mã định danh duy nhất của hội viên.</param>
        Task<ClmLedgerRecord?> GetClmLedgerAsync(string memId);

        /// <summary>
        /// Lấy thông tin sổ cái xếp hạng đang có hiệu lực (Status = 'O') của hội viên từ mssMemClassLedger.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Truy xuất trạng thái hạng thẻ hiện hành (Active Tier Ledger Reader).
        /// - MỤC ĐÍCH: Xác định hạng thẻ hiện tại (ClassNo) và điểm xếp hạng chốt lũy kế (CloseAmt) để tính tỷ lệ điểm và xét thăng hạng.
        /// - CÁCH DÙNG: Gọi trong bước tính điểm bán hàng và bước thẩm định thăng hạng.
        /// </remarks>
        /// <param name="memId">Mã định danh duy nhất của hội viên.</param>
        Task<MemberClassLedgerRecord?> GetActiveMemberClassAsync(string memId);

        /// <summary>
        /// Lấy cấu hình tham số của hạng thành viên từ bảng danh mục G_MemberClass.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Đọc cấu hình chính sách hạng thẻ (Tier Configuration Reader).
        /// - MỤC ĐÍCH: Lấy tỷ lệ quy đổi điểm thưởng mặc định (RewardRate) và điểm xếp hạng mặc định (ReviewRate).
        /// - CÁCH DÙNG: Gọi khi cần xác định tỷ lệ tích điểm hoặc điểm thưởng thăng hạng mới.
        /// </remarks>
        /// <param name="classNo">Mã hạng thẻ (ví dụ: 'MEMBER', 'SILVER', 'GOLD', 'PLATINUM').</param>
        Task<MemberClassInfo?> GetMemberClassAsync(string classNo);

        /// <summary>
        /// Lấy cặp tỷ lệ quy đổi điểm thưởng (RewardRate) và điểm xếp hạng (ReviewRate) theo hạng thẻ và gian hàng phát sinh.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Động cơ tra cứu tỷ lệ điểm đa tầng (Point Rate Policy Resolver).
        /// - MỤC ĐÍCH: Tra cứu chính sách ưu đãi theo gian hàng (Outlet Scope) hoặc hạng thẻ hội viên.
        /// - CÁCH DÙNG: Gọi trước khi tính toán số điểm thưởng và điểm xếp hạng.
        /// </remarks>
        /// <param name="classNo">Mã hạng thẻ.</param>
        /// <param name="billTotal">Tổng tiền hóa đơn.</param>
        /// <param name="transDate">Ngày giao dịch.</param>
        /// <param name="outletNumber">Mã gian hàng.</param>
        Task<BasePointRate> GetPointRateAsync(string classNo, decimal billTotal, DateTime transDate, string outletNumber);

        /// <summary>
        /// Ghi nhận chi tiết giao dịch mua sắm và cập nhật đồng thời 3 sổ cái kế toán trong 1 Transaction nguyên tử duy nhất.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Giao dịch kế toán đa sổ cái nguyên tử (Atomic Multi-Ledger Persistence).
        /// - MỤC ĐÍCH: 
        ///   1. Thêm bản ghi giao dịch mssCardTrans.
        ///   2. Cập nhật ví mssCLMLedger (AccSales += billTotal, AccPoints += rewardPoints, AccTC += 1, RedeemPoints GIỮ NGUYÊN).
        ///   3. Cập nhật sổ cái xếp hạng mssMemClassLedger (DebitAmt += rankPoints, CloseAmt chốt lũy kế, ReviewFlag = 'N').
        /// - CÁCH DÙNG: Được gọi duy nhất từ LoyaltyPointsService.ProcessSalePointsAsync để đảm bảo tính toàn vẹn dữ liệu.
        /// </remarks>
        Task<bool> RecordSaleAndPointsTransactionAsync(
            CardTransRecord cardTrans,
            decimal rewardPoints,
            decimal rankPoints,
            decimal billTotal,
            DateTime billDateTime,
            string reviewBy);

        /// <summary>
        /// Lấy danh mục dải quy định ngưỡng thăng hạng thành viên từ bảng G_MemRangeClass.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Đọc cấu hình dải nâng hạng (Tier Progression Range Reader).
        /// - MỤC ĐÍCH: Cung cấp danh sách điều kiện ngưỡng điểm (FromAmt, ToAmt, UpgradeClass) để đối chiếu nâng hạng.
        /// - CÁCH DÙNG: Gọi trong MembershipTierService khi rà soát nâng hạng.
        /// </remarks>
        Task<IEnumerable<MemberClassRange>> GetMemberRangesAsync();

        /// <summary>
        /// Thực thi nâng hạng thẻ hội viên nguyên tử (Atomic Member Tier Upgrade).
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Cập nhật chuyển hạng thẻ an toàn (Tier Transition Executor).
        /// - MỤC ĐÍCH: Đóng bản ghi hạng cũ (Status = 'C'), tạo bản ghi hạng mới (Status = 'O', DocType = 'R', ReviewFlag = 'Y'), và cộng điểm thưởng thăng hạng vào ví mssCLMLedger và bảng mssCLMGiftPoint.
        /// - CÁCH DÙNG: Gọi trong MembershipTierService khi hội viên vượt ngưỡng thăng hạng.
        /// </remarks>
        Task<bool> UpgradeMemberClassAsync(string memId, string newClassNo, decimal bonusPoints, string notes);

        /// <summary>
        /// Thẩm định chương trình quay số may mắn (Lottery) thỏa mãn điều kiện và hạn ngạch 3 tầng.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Thẩm định điều kiện vé số may mắn (Lottery Program Eligibility Auditor).
        /// - MỤC ĐÍCH: Kiểm tra ngày hiệu lực, ngày gia nhập, ngưỡng bill, Scope gian hàng và 3 tầng hạn ngạch trên mssLotteryGift.
        /// - CÁCH DÙNG: Gọi trong LotteryPromotionService trước khi cấp vé quay số.
        /// </remarks>
        Task<LotteryProgramInfo?> GetEligibleLotteryProgramAsync(decimal billAmount, DateTime transDate, DateTime? joinDate, string memId, string outletNumber);

        /// <summary>
        /// Thẩm định chương trình quà tặng khuyến mãi (FirstTrans hoặc Gift) thỏa mãn điều kiện và hạn ngạch 3 tầng.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Thẩm định điều kiện quà tặng khuyến mãi (Promotion Program Eligibility Auditor).
        /// - MỤC ĐÍCH: Ưu tiên FirstTrans (nếu chưa từng nhận quà) hoặc fallback sang Gift, kiểm tra ngày, ngưỡng bill, Scope và 3 tầng hạn ngạch trên mssPromotionGift.
        /// - CÁCH DÙNG: Gọi trong LotteryPromotionService trước khi cấp voucher quà tặng.
        /// </remarks>
        Task<LotteryProgramInfo?> GetEligiblePromotionProgramAsync(decimal billAmount, DateTime transDate, string memId, string outletNumber);

        /// <summary>
        /// Bốc thăm ngẫu nhiên và phát hành vé số may mắn từ kho mssLotteryGift.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Cấp phát vé quay số may mắn (Lottery Ticket Issuer).
        /// - MỤC ĐÍCH: Lấy vé chưa phát (MemID IS NULL), bốc thăm ngẫu nhiên có trọng số và gán MemID, BillNo, TransDate, SubTotal.
        /// - CÁCH DÙNG: Gọi trong LotteryPromotionService sau khi đã chọn được chương trình hợp lệ.
        /// </remarks>
        Task<LotteryGiftRecord?> IssueLotteryTicketAsync(LotteryProgramInfo program, string memId, string billNo, DateTime transDate, decimal subTotal);

        /// <summary>
        /// Bốc thăm ngẫu nhiên và phát hành quà tặng / voucher khuyến mãi từ kho mssPromotionGift.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Cấp phát voucher quà tặng (Promotion Voucher Issuer).
        /// - MỤC ĐÍCH: Lấy voucher chưa phát, bốc thăm ngẫu nhiên, tính ngày hết hạn và gán MemID, BillNo, TransDate, SubTotal, ToDate.
        /// - CÁCH DÙNG: Gọi trong LotteryPromotionService sau khi đã chọn được chương trình hợp lệ.
        /// </remarks>
        Task<PromotionGiftRecord?> IssuePromotionGiftAsync(LotteryProgramInfo program, string memId, string billNo, DateTime transDate, decimal subTotal);

        /// <summary>
        /// Lấy mẫu tin thông báo đẩy theo mã định danh mẫu từ G_NotificationMessage.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Đọc mẫu tin thông báo (Notification Template Reader).
        /// - MỤC ĐÍCH: Lấy tiêu đề và nội dung cấu hình sẵn theo mã sự kiện.
        /// - CÁCH DÙNG: Gọi khi chuẩn bị nội dung thông báo đẩy.
        /// </remarks>
        Task<NotificationTemplate?> GetNotificationTemplateAsync(string templateNumber);

        /// <summary>
        /// Ghi log lịch sử gửi thông báo đẩy vào bảng mssNotification.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Lưu vết thông báo đẩy (Notification Audit Logger).
        /// - MỤC ĐÍCH: Ghi nhận sự kiện thông báo để Firebase Background Worker gửi tới thiết bị di động và phục vụ tra cứu lịch sử thông báo trên App.
        /// - CÁCH DÙNG: Gọi trong NotificationService sau khi phát sinh sự kiện cộng điểm, thăng hạng, tặng vé số, tặng quà.
        /// </remarks>
        Task<bool> SaveNotificationLogAsync(string memId, string? appId, string title, string eventName, string subject, string body);
    }
}
