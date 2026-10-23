using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CRMMall.BillReview.BLL.DTOs;

namespace CRMMall.BillReview.BLL.Services
{
    /// <summary>
    /// Điều phối viên nghiệp vụ duyệt và xử lý hóa đơn OCR (Bill Review &amp; Approval Orchestrator)
    /// </summary>
    public interface IBillReviewService
    {
        /// <summary>
        /// Lấy danh sách các hóa đơn bóc tách đang chờ kiểm toán viên đối soát và phê duyệt.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Cung cấp dữ liệu hàng đợi duyệt (Audit Worklist Provider).
        /// - MỤC ĐÍCH: Truy vấn các hóa đơn có Approved IS NULL từ mssOCRTrialTrans, kết hợp thông tin hội viên và tự động chạy IAutoApprovalPolicy để gắn nhãn gợi ý (IsAutoApprovable) cho kiểm toán viên.
        /// - CÁCH DÙNG: Được gọi từ API GET /api/bills/pending trên Web Portal hoặc Worker điều phối.
        /// </remarks>
        /// <param name="fromDate">Lọc từ ngày tải lên (tùy chọn).</param>
        /// <param name="toDate">Lọc đến ngày tải lên (tùy chọn).</param>
        /// <param name="limit">Số lượng bản ghi tối đa (mặc định 100).</param>
        /// <returns>Danh sách các hóa đơn chờ duyệt kèm nhãn đánh giá an toàn.</returns>
        Task<IEnumerable<PendingBillDto>> GetPendingBillsAsync(DateTime? fromDate = null, DateTime? toDate = null, int limit = 100);

        /// <summary>
        /// Lấy chi tiết toàn bộ dữ liệu bóc tách và thông tin hội viên của một hóa đơn cụ thể.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Cung cấp dữ liệu chi tiết cho màn hình đối soát hóa đơn (Bill Detail Inspector).
        /// - MỤC ĐÍCH: Đọc hóa đơn theo ID, nạp thông tin khách hàng, thẻ thành viên và lý do gợi ý tự duyệt.
        /// - CÁCH DÙNG: Được gọi từ API GET /api/bills/{id} khi kiểm toán viên chọn 1 dòng hóa đơn để soi ảnh.
        /// </remarks>
        /// <param name="id">Mã định danh duy nhất của hóa đơn (mssOCRTrialTrans.ID).</param>
        /// <returns>Đối tượng <see cref="PendingBillDto"/> chi tiết hoặc null nếu không tồn tại.</returns>
        Task<PendingBillDto?> GetBillByIdAsync(Guid id);

        /// <summary>
        /// Thực thi trọn gói quy trình phê duyệt hóa đơn qua chuỗi 6 bước chuẩn hóa.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Quy trình nghiệp vụ cốt lõi (Core Approval Pipeline).
        /// - MỤC ĐÍCH:
        ///   1. Kiểm tra tính hợp lệ &amp; chống tích điểm trùng lặp gian lận.
        ///   2. Gọi ILoyaltyPointsService tính điểm và ghi đồng bộ 3 sổ cái (CardTrans, CLMLedger, MemClassLedger).
        ///   3. Gọi IMembershipTierService thẩm định và tự động nâng hạng thẻ.
        ///   4. Gọi ILotteryPromotionService thẩm định hạn ngạch và phát vé quay số / voucher khuyến mãi.
        ///   5. Gọi INotificationService điều phối gửi thông báo đẩy đa kênh tức thì.
        ///   6. Cập nhật trạng thái hóa đơn mssOCRTrialTrans sang 'Approved'.
        /// - CÁCH DÙNG: Được gọi khi kiểm toán viên bấm nút [Phê duyệt] trên Web Portal hoặc khi Worker AI tự động duyệt hóa đơn hợp lệ.
        /// </remarks>
        /// <param name="request">Yêu cầu phê duyệt chứa thông tin đã đối soát (số tiền, số bill, gian hàng, ngày giờ).</param>
        /// <returns>Đối tượng <see cref="BillReviewResult"/> chứa kết quả chi tiết: điểm thưởng, điểm xếp hạng, thăng hạng, vé số, quà tặng.</returns>
        Task<BillReviewResult> ApproveBillAsync(ApproveBillRequest request);

        /// <summary>
        /// Từ chối hóa đơn không hợp lệ và gửi thông báo phản hồi lý do đến khách hàng.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Quy trình từ chối hóa đơn (Audit Rejection Handler).
        /// - MỤC ĐÍCH: Cập nhật Approved = 0, ghi nhận lý do từ chối (ReviewNotes) và gửi Push Notification phản hồi để khách hàng biết lý do.
        /// - CÁCH DÙNG: Được gọi khi kiểm toán viên bấm nút [Từ chối] trên giao diện Web Portal kèm lý do phản hồi.
        /// </remarks>
        /// <param name="request">Yêu cầu từ chối chứa mã hóa đơn và lý do từ chối.</param>
        /// <returns>Kết quả xử lý từ chối.</returns>
        Task<BillReviewResult> RejectBillAsync(RejectBillRequest request);

        /// <summary>
        /// Webhook tiếp nhận kết quả bóc tách hóa đơn từ Server OCR chuyên dụng độc lập.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Cổng kết nối tích hợp (Integration Ingestion Webhook).
        /// - MỤC ĐÍCH: Tiếp nhận kết quả trích xuất ban đầu (Số bill, ngày giờ, gian hàng, tổng tiền, ảnh) từ máy chủ OCR AI và lưu vào mssOCRTrialTrans ở trạng thái 'Pending'.
        /// - CÁCH DÙNG: Được gọi qua HTTP POST từ Server AI chuyên dụng sau khi bóc tách xong ảnh hóa đơn.
        /// </remarks>
        /// <param name="request">Dữ liệu hóa đơn OCR trích xuất từ máy chủ AI.</param>
        /// <returns>Mã định danh hóa đơn mới tạo (ID).</returns>
        Task<Guid> ReceiveOcrScanResultAsync(OcrWebhookRequest request);

        /// <summary>
        /// Kích hoạt tiến trình quét và tự động phê duyệt hóa đơn theo lô (Batch Auto-Approval Runner).
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Động cơ tự động hóa theo lô (Batch Automation Engine).
        /// - MỤC ĐÍCH: Lấy danh sách hóa đơn 'Pending', đánh giá qua IAutoApprovalPolicy. Nếu đạt 5 điều kiện an toàn thì tự động gọi ApproveBillAsync với tên người duyệt là 'AI_AUTO_APPROVE'.
        /// - CÁCH DÙNG: Được gọi từ CheckPoint Framework theo lịch định kỳ (cron job) hoặc từ API POST /api/bills/auto-review.
        /// </remarks>
        /// <param name="maxBatchSize">Số lượng hóa đơn xử lý tối đa mỗi lượt quét (mặc định 50).</param>
        /// <returns>Báo cáo tổng kết số lượng duyệt tự động và số lượng chuyển duyệt tay.</returns>
        Task<AutoReviewSummary> RunAutoApprovalBatchAsync(int maxBatchSize = 50);
    }
}
