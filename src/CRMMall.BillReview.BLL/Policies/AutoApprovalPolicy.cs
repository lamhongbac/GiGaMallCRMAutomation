using System;
using CRMMall.BillReview.DAL.Entities;

namespace CRMMall.BillReview.BLL.Policies
{
    /// <summary>
    /// Kết quả thẩm định tự động duyệt
    /// </summary>
    public class AutoApprovalDecision
    {
        public bool CanAutoApprove { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Triển khai bộ quy tắc thẩm định tự động phê duyệt hóa đơn
    /// </summary>
    public class AutoApprovalPolicy : IAutoApprovalPolicy
    {
        /// <summary>
        /// Đánh giá hóa đơn bóc tách qua bộ 5 quy tắc kiểm soát an toàn trước khi cho phép duyệt tự động.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Bộ lọc an toàn chống gian lận (Anti-fraud &amp; Quality Gatekeeper).
        /// - MỤC ĐÍCH: Thẩm định 5 tiêu chuẩn an toàn trước khi cho phép hệ thống tự tích điểm mà không cần người duyệt.
        /// - CÁCH DÙNG: Được gọi từ AutoApprovalBackgroundWorker trước khi tự động duyệt, hoặc gọi trên Web Portal để gắn nhãn gợi ý cho kiểm toán viên.
        /// </remarks>
        /// <param name="bill">Hóa đơn bóc tách cần đánh giá.</param>
        /// <param name="customer">Thông tin hồ sơ hội viên sở hữu hóa đơn.</param>
        /// <returns>Đối tượng <see cref="AutoApprovalDecision"/> cho biết có được tự duyệt hay không và lý do chi tiết.</returns>
        public AutoApprovalDecision Evaluate(OcrTrialTrans bill, CustomerInfo customer)
        {
            // 1. Kiểm tra trạng thái hội viên: Bắt buộc là 'L' (Life / Đang hoạt động, không bị khóa 'S')
            if (customer.Status != "L")
            {
                return new AutoApprovalDecision 
                { 
                    CanAutoApprove = false, 
                    Reason = $"Hội viên có trạng thái '{customer.Status}' không hợp lệ (yêu cầu trạng thái 'L' - Life / Đang hoạt động)." 
                };
            }

            // 2. Kiểm tra tính đầy đủ của dữ liệu cốt lõi
            if (string.IsNullOrWhiteSpace(bill.OutletNumber) ||
                string.IsNullOrWhiteSpace(bill.BillNumber) ||
                string.IsNullOrWhiteSpace(bill.BillDateTime) ||
                string.IsNullOrWhiteSpace(bill.BillTotal))
            {
                return new AutoApprovalDecision 
                { 
                    CanAutoApprove = false, 
                    Reason = "Thiếu một trong các trường dữ liệu cốt lõi (Mã gian hàng, Số bill, Ngày giờ, Tổng tiền)." 
                };
            }

            // 3. Kiểm tra tính hợp lệ của số tiền thanh toán
            if (!decimal.TryParse(bill.BillTotal, out var totalAmount) || totalAmount <= 0)
            {
                return new AutoApprovalDecision 
                { 
                    CanAutoApprove = false, 
                    Reason = "Tổng tiền trên hóa đơn không hợp lệ hoặc nhỏ hơn hoặc bằng 0." 
                };
            }

            // 4. Ngưỡng an toàn duyệt tự động giai đoạn đầu: <= 500.000 VNĐ
            const decimal maxAutoApprovalAmount = 500000m;
            if (totalAmount > maxAutoApprovalAmount)
            {
                return new AutoApprovalDecision 
                { 
                    CanAutoApprove = false, 
                    Reason = $"Giá trị hóa đơn ({totalAmount:N0}đ) vượt quá ngưỡng tự duyệt an toàn ({maxAutoApprovalAmount:N0}đ). Cần kiểm toán viên đối soát thủ công." 
                };
            }

            // 5. Kiểm tra thời hạn hóa đơn: Không quá 7 ngày kể từ lúc mua hàng
            if (DateTime.TryParse(bill.BillDateTime, out var billDate))
            {
                if (billDate > DateTime.Now.AddMinutes(15))
                {
                    return new AutoApprovalDecision 
                    { 
                        CanAutoApprove = false, 
                        Reason = "Thời gian trên hóa đơn lớn hơn thời gian hiện tại (nghi vấn làm giả thời gian)." 
                    };
                }

                if (billDate < DateTime.Now.AddDays(-7))
                {
                    return new AutoApprovalDecision 
                    { 
                        CanAutoApprove = false, 
                        Reason = "Hóa đơn đã quá hạn 7 ngày theo quy định tích điểm của TTTM Gigamall." 
                    };
                }
            }
            else
            {
                return new AutoApprovalDecision 
                { 
                    CanAutoApprove = false, 
                    Reason = "Định dạng ngày giờ trên hóa đơn không hợp lệ." 
                };
            }

            return new AutoApprovalDecision 
            { 
                CanAutoApprove = true, 
                Reason = "Hóa đơn thỏa mãn đầy đủ 5 tiêu chí an toàn duyệt tự động." 
            };
        }
    }
}
