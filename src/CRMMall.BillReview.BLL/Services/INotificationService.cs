using System.Threading.Tasks;
using CRMMall.BillReview.BLL.DTOs;

namespace CRMMall.BillReview.BLL.Services
{
    /// <summary>
    /// Thư viện logic điều phối và gửi thông báo đẩy (Firebase Push Notification Dispatcher)
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Gửi thông báo đẩy chúc mừng tích điểm thành công sau khi duyệt hóa đơn mua sắm.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Kênh thông báo biến động điểm (Point Gain Notification).
        /// - MỤC ĐÍCH: Tra cứu FCM Token thiết bị của hội viên từ mssMobileTracking, khởi tạo mẫu tin sự kiện GetPointBySale và ghi log vào bảng mssNotification để Firebase Worker gửi tới ứng dụng Gigamall.
        /// - CÁCH DÙNG: Gọi ngay sau khi giao dịch tích điểm (ProcessSalePointsAsync) thành công.
        /// </remarks>
        /// <param name="memId">Mã định danh hội viên.</param>
        /// <param name="billNumber">Số hóa đơn đã duyệt.</param>
        /// <param name="outletNumber">Mã gian hàng.</param>
        /// <param name="points">Số điểm thưởng vừa được cộng.</param>
        /// <param name="rate">Tỷ lệ quy đổi điểm áp dụng (VND/điểm).</param>
        Task SendSalePointsNotificationAsync(string memId, string billNumber, string outletNumber, decimal points, decimal rate);

        /// <summary>
        /// Gửi thông báo đẩy chúc mừng thăng hạng thẻ thành viên mới.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Kênh thông báo vinh danh thăng hạng (Tier Progression Notification).
        /// - MỤC ĐÍCH: Khởi tạo thông báo sự kiện MemberUpClass thông báo hạng thẻ mới và mời hội viên trải nghiệm các đặc quyền mới trên Gigamall App.
        /// - CÁCH DÙNG: Gọi ngay khi MembershipTierService phát hiện hội viên thăng hạng thành công.
        /// </remarks>
        /// <param name="memId">Mã định danh hội viên.</param>
        /// <param name="newTier">Tên hạng thẻ mới (ví dụ: GOLD, PLATINUM, DIAMOND).</param>
        Task SendTierUpgradedNotificationAsync(string memId, string newTier);

        /// <summary>
        /// Gửi thông báo đẩy thông báo mã vé quay số may mắn đã được trao tặng.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Kênh thông báo vé số trúng thưởng (Lucky Draw Ticket Notification).
        /// - MỤC ĐÍCH: Thông báo mã vé số (LotteryCode) cho hội viên và hướng dẫn mở màn hình Lucky Draw trên Mobile App để theo dõi đợt quay số.
        /// - CÁCH DÙNG: Gọi khi LotteryPromotionService phát hành vé số thành công.
        /// </remarks>
        /// <param name="memId">Mã định danh hội viên.</param>
        /// <param name="ticket">Thông tin chi tiết vé số may mắn vừa được cấp.</param>
        Task SendLotteryTicketNotificationAsync(string memId, LotteryTicketResult ticket);

        /// <summary>
        /// Gửi thông báo đẩy thông báo voucher / quà tặng khuyến mãi vừa nhận được.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Kênh thông báo ưu đãi / quà tặng (Promotion Voucher Notification).
        /// - MỤC ĐÍCH: Thông báo mã ưu đãi (PromotionCode), tên quà tặng và hạn sử dụng tới ví voucher của hội viên trên ứng dụng di động.
        /// - CÁCH DÙNG: Gọi khi LotteryPromotionService phát hành quà tặng khuyến mãi thành công.
        /// </remarks>
        /// <param name="memId">Mã định danh hội viên.</param>
        /// <param name="promo">Thông tin chi tiết voucher / quà tặng vừa được cấp.</param>
        Task SendPromotionGiftNotificationAsync(string memId, PromotionGiftResult promo);

        /// <summary>
        /// Gửi thông báo đẩy thông báo lý do từ chối hóa đơn không hợp lệ.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Kênh phản hồi lý do kiểm toán (Audit Rejection Feedback).
        /// - MỤC ĐÍCH: Gửi lý do chi tiết (ví dụ: ảnh mờ, bill không đúng định dạng, sai ngày) để khách hàng nắm rõ và chụp lại hóa đơn khác.
        /// - CÁCH DÙNG: Gọi khi kiểm toán viên từ chối hóa đơn (RejectBillAsync).
        /// </remarks>
        /// <param name="memId">Mã định danh hội viên.</param>
        /// <param name="reason">Lý do từ chối cụ thể do kiểm toán viên nhập.</param>
        Task SendBillRejectedNotificationAsync(string memId, string reason);
    }
}
