using System.Threading.Tasks;
using CRMMall.BillReview.DAL.Entities;

namespace CRMMall.BillReview.DAL.Repositories
{
    /// <summary>
    /// Kho lưu trữ dữ liệu hồ sơ hội viên và thiết bị di động (Customer &amp; Mobile Device Data Access)
    /// </summary>
    public interface ICustomerRepository
    {
        /// <summary>
        /// Tra cứu hồ sơ thông tin hội viên theo mã định danh duy nhất (MemID).
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Đọc hồ sơ khách hàng (Customer Profile Provider).
        /// - MỤC ĐÍCH: Đọc họ tên, số điện thoại, ngày gia nhập (DateJoined) và trạng thái tài khoản (Status: 'L' - Life / Active, 'S' - Suspend / Bị khóa) từ bảng mssCustomer.
        /// - CÁCH DÙNG: Gọi trong bước kiểm tra tính hợp lệ của hội viên trước khi duyệt hóa đơn hoặc phát vé quay số.
        /// </remarks>
        /// <param name="memId">Mã định danh duy nhất của hội viên.</param>
        /// <returns>Đối tượng <see cref="CustomerInfo"/> hoặc null nếu không tìm thấy.</returns>
        Task<CustomerInfo?> GetCustomerByIdAsync(string memId);

        /// <summary>
        /// Tra cứu Firebase Cloud Messaging (FCM) Device Token thiết bị di động của hội viên từ bảng mssMobileTracking.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Lấy địa chỉ định tuyến thông báo đẩy (FCM Push Token Resolver).
        /// - MỤC ĐÍCH: Lấy Token thiết bị đăng nhập gần nhất của hội viên để điều phối gửi thông báo trực tiếp vào điện thoại.
        /// - CÁCH DÙNG: Gọi trong NotificationService trước khi gửi Push Notification.
        /// </remarks>
        /// <param name="memId">Mã định danh duy nhất của hội viên.</param>
        /// <returns>Chuỗi FCM Token hoặc null nếu hội viên chưa cài hoặc đăng xuất App.</returns>
        Task<string?> GetDeviceTokenAsync(string memId);
    }
}
