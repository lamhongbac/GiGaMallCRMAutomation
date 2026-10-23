using System;
using System.Threading.Tasks;
using CRMMall.BillReview.BLL.DTOs;

namespace CRMMall.BillReview.BLL.Services
{
    /// <summary>
    /// Thư viện logic nghiệp vụ tính toán và tích lũy điểm thưởng Loyalty (Points & Multi-Ledger Engine)
    /// </summary>
    public interface ILoyaltyPointsService
    {
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
        PointsCalculationResult CalculatePoints(decimal billTotal, decimal rewardRate, decimal rankingRate);

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
        Task<PointsAccrualResult> ProcessSalePointsAsync(
            string memId,
            string? cardNo,
            Guid? orderId,
            string billNo,
            string outletNumber,
            DateTime billDateTime,
            decimal billTotal,
            string? cashierName,
            string reviewBy,
            string? reviewNotes);
    }
}
