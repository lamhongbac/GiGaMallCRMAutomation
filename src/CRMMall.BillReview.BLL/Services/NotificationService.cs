using System;
using System.Threading.Tasks;
using CRMMall.BillReview.BLL.DTOs;
using CRMMall.BillReview.DAL.Repositories;

namespace CRMMall.BillReview.BLL.Services
{
    /// <summary>
    /// Triển khai điều phối và gửi thông báo đẩy Firebase Push Notification
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ILoyaltyRepository _loyaltyRepository;

        public NotificationService(
            ICustomerRepository customerRepository,
            ILoyaltyRepository loyaltyRepository)
        {
            _customerRepository = customerRepository;
            _loyaltyRepository = loyaltyRepository;
        }

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
        public async Task SendSalePointsNotificationAsync(string memId, string billNumber, string outletNumber, decimal points, decimal rate)
        {
            var token = await _customerRepository.GetDeviceTokenAsync(memId);
            string title = "Gigamall - Tích điểm thành công!";
            string body = $"Hóa đơn #{billNumber} tại gian hàng {outletNumber} đã được phê duyệt. Bạn được cộng +{points:N0} điểm thưởng (Tỷ lệ: {rate:N0}đ/điểm).";
            
            await _loyaltyRepository.SaveNotificationLogAsync(
                memId, 
                token, 
                title, 
                eventName: "GetPointBySale", 
                subject: title, 
                body: body);
        }

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
        public async Task SendTierUpgradedNotificationAsync(string memId, string newTier)
        {
            var token = await _customerRepository.GetDeviceTokenAsync(memId);
            string title = "Chúc mừng thăng hạng thành viên!";
            string body = $"Chúc mừng bạn đã được nâng lên hạng {newTier}! Hãy khám phá các quyền lợi đặc quyền mới tại Gigamall.";
            
            await _loyaltyRepository.SaveNotificationLogAsync(
                memId, 
                token, 
                title, 
                eventName: "MemberUpClass", 
                subject: title, 
                body: body);
        }

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
        public async Task SendLotteryTicketNotificationAsync(string memId, LotteryTicketResult ticket)
        {
            var token = await _customerRepository.GetDeviceTokenAsync(memId);
            string title = "Bạn nhận được Vé quay số may mắn!";
            string body = $"Chúc mừng bạn nhận được mã vé quay số #{ticket.LotteryCode} từ chương trình '{ticket.ProgramName}'. Hãy kiểm tra trong mục Vé số may mắn!";
            
            await _loyaltyRepository.SaveNotificationLogAsync(
                memId,
                token,
                title,
                eventName: "SendLottery",
                subject: title,
                body: body);
        }

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
        public async Task SendPromotionGiftNotificationAsync(string memId, PromotionGiftResult promo)
        {
            var token = await _customerRepository.GetDeviceTokenAsync(memId);
            string title = "Bạn nhận được Quà tặng khuyến mãi!";
            string body = promo.ExpireDate.HasValue
                ? $"Chúc mừng bạn nhận được mã ưu đãi #{promo.PromotionCode} từ chương trình '{promo.ProgramName}'. Hạn dùng đến: {promo.ExpireDate.Value:dd/MM/yyyy}."
                : $"Chúc mừng bạn nhận được mã ưu đãi #{promo.PromotionCode} từ chương trình '{promo.ProgramName}'.";
            
            await _loyaltyRepository.SaveNotificationLogAsync(
                memId,
                token,
                title,
                eventName: "SendPromotion",
                subject: title,
                body: body);
        }

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
        public async Task SendBillRejectedNotificationAsync(string memId, string reason)
        {
            var token = await _customerRepository.GetDeviceTokenAsync(memId);
            string title = "Thông báo kết quả duyệt hóa đơn";
            string body = $"Hóa đơn của bạn không đủ điều kiện tích điểm. Lý do: {reason}";
            
            await _loyaltyRepository.SaveNotificationLogAsync(
                memId,
                token,
                title,
                eventName: "RejectTransaction",
                subject: title,
                body: body);
        }
    }
}
