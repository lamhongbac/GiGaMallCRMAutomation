using System;
using System.Threading.Tasks;
using CRMMall.BillReview.BLL.DTOs;
using CRMMall.BillReview.DAL.Entities;
using CRMMall.BillReview.DAL.Repositories;

namespace CRMMall.BillReview.BLL.Services
{
    /// <summary>
    /// Triển khai nghiệp vụ tính toán và tích lũy điểm thưởng Loyalty
    /// </summary>
    public class LoyaltyPointsService : ILoyaltyPointsService
    {
        private readonly ILoyaltyRepository _loyaltyRepository;

        public LoyaltyPointsService(ILoyaltyRepository loyaltyRepository)
        {
            _loyaltyRepository = loyaltyRepository;
        }

        /// <summary>
        /// Thuật toán tính toán số điểm thưởng (Reward Points) và điểm xếp hạng (Ranking Points) từ doanh số hóa đơn.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Pure Business Calculation function (Hàm nghiệp vụ thuần túy không phụ thuộc I/O).
        /// - MỤC ĐÍCH: Quy đổi doanh số thanh toán (VND) ra điểm thưởng đổi quà và điểm xếp hạng theo tỷ lệ quy đổi của hạng thành viên hoặc gian hàng, áp dụng nguyên tắc làm tròn kế toán AwayFromZero (>= 0.5 làm tròn lên 1).
        /// - CÁCH DÙNG: Gọi hàm này khi cần tính trước số điểm dự kiến (ví dụ: hiển thị preview trên UI hoặc trước khi ghi vào CSDL).
        /// </remarks>
        /// <param name="billTotal">Tổng số tiền thanh toán hợp lệ trên hóa đơn (VND).</param>
        /// <param name="rewardRate">Tỷ lệ quy đổi điểm thưởng (VND/điểm, mặc định 10.000đ/điểm nếu chưa cấu hình).</param>
        /// <param name="rankingRate">Tỷ lệ quy đổi điểm xếp hạng (VND/điểm, mặc định 10.000đ/điểm nếu chưa cấu hình).</param>
        /// <returns>Đối tượng <see cref="PointsCalculationResult"/> chứa số điểm thưởng và điểm xếp hạng đã làm tròn.</returns>
        public PointsCalculationResult CalculatePoints(decimal billTotal, decimal rewardRate, decimal rankingRate)
        {
            decimal effRewardRate = rewardRate > 0 ? rewardRate : 10000m;
            decimal effRankingRate = rankingRate > 0 ? rankingRate : 10000m;

            // Quy tắc làm tròn chuẩn kế toán: Math.Round(..., MidpointRounding.AwayFromZero)
            decimal rewardPoints = Math.Round(billTotal / effRewardRate, MidpointRounding.AwayFromZero);
            decimal rankingPoints = Math.Round(billTotal / effRankingRate, MidpointRounding.AwayFromZero);

            if (rewardPoints < 0) rewardPoints = 0;
            if (rankingPoints < 0) rankingPoints = 0;

            return new PointsCalculationResult
            {
                RewardPoints = rewardPoints,
                RankingPoints = rankingPoints,
                RewardRate = effRewardRate,
                RankingRate = effRankingRate
            };
        }

        /// <summary>
        /// Thực thi trọn gói quy trình tích điểm giao dịch mua sắm và cập nhật đồng bộ 3 sổ cái kế toán.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Dịch vụ nghiệp vụ lõi (Core Domain Service) xử lý giao dịch bán hàng tích điểm thành viên.
        /// - MỤC ĐÍCH: Tra cứu hạng thẻ đang mở (Status = 'O') -> Tra cứu tỷ lệ điểm theo Outlet &amp; Hạng thẻ -> Tính điểm -> Ghi đồng bộ 3 sổ cái (mssCardTrans, mssCLMLedger, mssMemClassLedger) trong 1 Transaction nguyên tử duy nhất.
        /// - CÁCH DÙNG: Được gọi từ quy trình duyệt hóa đơn (BillReviewService), từ API quẹt thẻ POS tại quầy thanh toán, hoặc từ hệ thống E-commerce khi đơn hàng hoàn tất.
        /// </remarks>
        /// <param name="memId">Mã định danh duy nhất của hội viên (MemID).</param>
        /// <param name="cardNo">Mã số thẻ vật lý/điện tử của hội viên.</param>
        /// <param name="orderId">Mã định danh hóa đơn bóc tách (mssOCRTrialTrans.ID) hoặc mã đơn hàng tham chiếu.</param>
        /// <param name="billNo">Số hóa đơn bán hàng.</param>
        /// <param name="outletNumber">Mã gian hàng / cửa hàng phát sinh hóa đơn.</param>
        /// <param name="billDateTime">Ngày giờ phát sinh giao dịch trên hóa đơn.</param>
        /// <param name="billTotal">Tổng số tiền thanh toán thực tế (VND).</param>
        /// <param name="cashierName">Tên thu ngân in trên bill (nếu có).</param>
        /// <param name="reviewBy">Tên người duyệt hóa đơn hoặc 'AI_AUTO_APPROVE' hoặc mã thu ngân POS.</param>
        /// <param name="reviewNotes">Ghi chú duyệt hoặc lý do điều chỉnh.</param>
        /// <returns>Đối tượng <see cref="PointsAccrualResult"/> thông báo thành công/thất bại, số điểm được tích và mã chứng từ giao dịch DocID.</returns>
        public async Task<PointsAccrualResult> ProcessSalePointsAsync(
            string memId,
            string? cardNo,
            Guid? orderId,
            string billNo,
            string outletNumber,
            DateTime billDateTime,
            decimal billTotal,
            string? cashierName,
            string reviewBy,
            string? reviewNotes)
        {
            // 1. Lấy thông tin sổ cái xếp hạng đang mở (Status = 'O')
            var activeClass = await _loyaltyRepository.GetActiveMemberClassAsync(memId);
            if (activeClass == null)
            {
                return new PointsAccrualResult
                {
                    Success = false,
                    Message = $"Không tìm thấy sổ cái xếp hạng đang mở (mssMemClassLedger Status = 'O') của hội viên {memId}."
                };
            }

            // 2. Tra cứu tỷ lệ điểm theo hạng thẻ và gian hàng
            var basePointRate = await _loyaltyRepository.GetPointRateAsync(
                activeClass.ClassNo,
                billTotal,
                billDateTime,
                outletNumber);

            // 3. Tính toán số điểm theo quy chuẩn
            var calc = CalculatePoints(billTotal, basePointRate.RewardPointRate, basePointRate.RankingPointRate);

            // 4. Khởi tạo đối tượng bản ghi giao dịch mssCardTrans
            var cardTrans = new CardTransRecord
            {
                DocID = Guid.NewGuid(),
                CLPID = 1,
                MemID = memId,
                CardNo = cardNo,
                ClassNo = activeClass.ClassNo,
                OrderID = orderId,
                BillNo = billNo,
                CheckNo = billNo,
                OutletNumber = outletNumber,
                TransDate = billDateTime.Date,
                POSBusinessDate = billDateTime,
                TransType = 1, // Sale
                SubTotal = billTotal,
                TotalBillValue = billTotal,
                RPointGain = calc.RewardPoints,
                RPointRate = calc.RewardRate,
                ReviewPointGain = calc.RankingPoints,
                ReviewPointRate = calc.RankingRate,
                CashierName = cashierName,
                UseMobileApp = true,
                BProcess = "Y",
                Notes = reviewNotes,
                CreatedBy = reviewBy,
                CreatedOn = DateTime.Now
            };

            // 5. Cập nhật đồng bộ đa sổ cái nguyên tử (mssCardTrans, mssCLMLedger, mssMemClassLedger)
            await _loyaltyRepository.RecordSaleAndPointsTransactionAsync(
                cardTrans,
                calc.RewardPoints,
                calc.RankingPoints,
                billTotal,
                billDateTime,
                reviewBy);

            return new PointsAccrualResult
            {
                Success = true,
                Message = $"Tích lũy thành công +{calc.RewardPoints:N0} điểm thưởng (+{calc.RankingPoints:N0} điểm xếp hạng).",
                PointsAwarded = calc.RewardPoints,
                RankPointsAwarded = calc.RankingPoints,
                RewardRate = calc.RewardRate,
                ReviewRate = calc.RankingRate,
                CardTransDocID = cardTrans.DocID
            };
        }
    }
}
