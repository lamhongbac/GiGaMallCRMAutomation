using CRMMall.BillReview.DAL.Entities;

namespace CRMMall.BillReview.BLL.Policies
{
    /// <summary>
    /// Chính sách thẩm định điều kiện tự động phê duyệt hóa đơn (Auto-Approval Rule Engine)
    /// </summary>
    public interface IAutoApprovalPolicy
    {
        /// <summary>
        /// Đánh giá hóa đơn bóc tách qua bộ 5 quy tắc kiểm soát an toàn trước khi cho phép duyệt tự động.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Bộ lọc an toàn chống gian lận (Anti-fraud &amp; Quality Gatekeeper).
        /// - MỤC ĐÍCH: Thẩm định 5 tiêu chuẩn khắt khe:
        ///   1. Trạng thái tài khoản hội viên phải là 'L' (Life / Đang hoạt động, không bị khóa).
        ///   2. Đầy đủ dữ liệu cốt lõi (Số bill, gian hàng, ngày giờ, tổng tiền).
        ///   3. Giá trị hóa đơn nằm trong ngưỡng tự duyệt an toàn (0 &lt; BillTotal &lt;= 500.000đ).
        ///   4. Hóa đơn phát sinh trong vòng 7 ngày gần nhất.
        ///   5. Không có dấu hiệu hóa đơn giả hoặc trùng lặp số bill.
        /// - CÁCH DÙNG: Được gọi từ AutoApprovalBackgroundWorker trước khi tự động duyệt, hoặc gọi trên Web Portal để gắn nhãn gợi ý cho kiểm toán viên.
        /// </remarks>
        /// <param name="bill">Hóa đơn bóc tách cần đánh giá.</param>
        /// <param name="customer">Thông tin hồ sơ hội viên sở hữu hóa đơn.</param>
        /// <returns>Đối tượng <see cref="AutoApprovalDecision"/> cho biết có được tự duyệt hay không và lý do chi tiết.</returns>
        AutoApprovalDecision Evaluate(OcrTrialTrans bill, CustomerInfo customer);
    }
}
