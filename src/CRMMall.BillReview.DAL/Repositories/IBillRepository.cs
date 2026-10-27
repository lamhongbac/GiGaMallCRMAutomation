using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CRMMall.BillReview.DAL.Entities;

namespace CRMMall.BillReview.DAL.Repositories
{
    /// <summary>
    /// Kho lưu trữ dữ liệu hóa đơn OCR và trạng thái đối soát (OCR Bill Data Access)
    /// </summary>
    public interface IBillRepository
    {
        /// <summary>
        /// Truy vấn danh sách các hóa đơn OCR đang ở trạng thái chờ duyệt (Approved IS NULL).
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Cung cấp hàng đợi hóa đơn cần xử lý (Pending Worklist Reader).
        /// - MỤC ĐÍCH: Lấy danh sách hóa đơn từ bảng mssOCRTrialTrans theo khoảng thời gian và giới hạn số lượng.
        /// - CÁCH DÙNG: Gọi từ API GET /api/bills/pending trên giao diện Web Portal hoặc tiến trình AutoApprovalBackgroundWorker.
        /// </remarks>
        /// <param name="fromDate">Ngày bắt đầu tải ảnh lên (tùy chọn).</param>
        /// <param name="toDate">Ngày kết thúc tải ảnh lên (tùy chọn).</param>
        /// <param name="limit">Số lượng bản ghi tối đa (mặc định 100).</param>
        /// <summary>
        /// Truy vấn danh sách các hóa đơn chờ xử lý (Pending Bills):
        /// Điều kiện chuẩn: (OCRStatus = 0 OR OCRStatus IS NULL) AND (Approved = 0 OR Approved IS NULL).
        /// </summary>
        Task<IEnumerable<OcrTrialTrans>> GetPendingBillsAsync(DateTime? fromDate = null, DateTime? toDate = null, int limit = 100);

        /// <summary>
        /// Truy vấn danh sách hóa đơn đã bóc tách OCR xong chờ thẩm định (Tab 2):
        /// Điều kiện: OCRStatus = 1 AND (Approved = 0 OR Approved IS NULL).
        /// </summary>
        Task<IEnumerable<OcrTrialTrans>> GetBillsForReviewAsync(DateTime? fromDate = null, DateTime? toDate = null, int limit = 100);

        /// <summary>
        /// Lấy chi tiết thông tin hóa đơn bóc tách theo mã định danh duy nhất (ID).
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Truy xuất chi tiết hóa đơn (Bill Detail Reader).
        /// - MỤC ĐÍCH: Đọc toàn bộ dữ liệu bóc tách của 1 hóa đơn để hiển thị trên giao diện soi ảnh.
        /// - CÁCH DÙNG: Gọi khi kiểm toán viên chọn 1 hóa đơn trên màn hình đối soát.
        /// </remarks>
        /// <param name="id">Mã định danh duy nhất của hóa đơn (mssOCRTrialTrans.ID).</param>
        Task<OcrTrialTrans?> GetByIdAsync(Guid id);

        /// <summary>
        /// Thêm mới bản ghi hóa đơn bóc tách từ Webhook máy chủ OCR AI chuyên dụng.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Tiếp nhận hóa đơn mới vào hàng đợi (Bill Ingestion Writer).
        /// - MỤC ĐÍCH: Lưu trữ kết quả trích xuất ban đầu (ảnh, số bill, gian hàng, ngày giờ, số tiền) vào mssOCRTrialTrans với DataStatus = 'Pending'.
        /// - CÁCH DÙNG: Gọi từ endpoint Webhook /api/bills/webhook/ocr-result.
        /// </remarks>
        /// <param name="bill">Đối tượng hóa đơn OCR mới.</param>
        Task<bool> InsertAsync(OcrTrialTrans bill);

        /// <summary>
        /// Cập nhật trạng thái phê duyệt hoặc từ chối và chốt dữ liệu cuối cùng của hóa đơn.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Chốt trạng thái kiểm toán hóa đơn (Audit Decision Finalizer).
        /// - MỤC ĐÍCH: Cập nhật Approved (1 = duyệt, 0 = từ chối), Reviewed = 1, ReviewDate = GETDATE(), ghi nhận người duyệt, lý do, số điểm tích lũy và dữ liệu chốt cuối cùng (BillNumberA, BillTotalA, OutletNumberA).
        /// - CÁCH DÙNG: Gọi ở bước cuối cùng của ApproveBillAsync hoặc RejectBillAsync trong BillReviewService.
        /// </remarks>
        Task<bool> UpdateApprovalStatusAsync(
            Guid id, 
            bool approved, 
            string reviewBy, 
            string? reviewNotes, 
            decimal? approvedAmount, 
            string? approvedBillNumber, 
            DateTime? approvedBillDateTime, 
            string? approvedOutletNumber, 
            string? approvedBrandNumber, 
            decimal? pointGain);

        /// <summary>
        /// Kiểm tra chống gian lận: Ngăn chặn tích điểm trùng lặp cùng một số hóa đơn trên cùng một gian hàng.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Chốt chặn kiểm soát gian lận tích điểm trùng (Anti-fraud Duplicate Checker).
        /// - MỤC ĐÍCH: Kiểm tra xem cặp (OutletNumber, BillNumber) đã từng được phê duyệt (Approved = 1) trên hệ thống trước đây chưa.
        /// - CÁCH DÙNG: Gọi ở Bước 1 của quy trình phê duyệt hóa đơn để từ chối các bill chụp lại gian lận.
        /// </remarks>
        /// <param name="outletNumber">Mã gian hàng phát sinh hóa đơn.</param>
        /// <param name="billNumber">Số hóa đơn cần kiểm tra.</param>
        /// <param name="excludeBillId">Mã hóa đơn hiện tại cần loại trừ trong phép so khớp.</param>
        /// <returns>True nếu số hóa đơn đã tồn tại trên gian hàng đó, False nếu là hóa đơn mới hợp lệ.</returns>
        Task<bool> IsBillNumberExistsAsync(string outletNumber, string billNumber, Guid excludeBillId);

        /// <summary>
        /// Lấy danh sách các hóa đơn vừa upload chưa thực hiện OCR có phân trang server-side (OFFSET ... FETCH NEXT).
        /// </summary>
        Task<(IEnumerable<OcrTrialTrans> Items, int TotalCount)> GetPendingUploadBillsPagedAsync(DateTime? fromDate = null, DateTime? toDate = null, int pageIndex = 1, int pageSize = 25);

        /// <summary>
        /// Lấy danh sách các hóa đơn vừa upload nhưng chưa thực hiện OCR (DataStatus = 'Pending' hoặc ScanDate IS NULL).
        /// </summary>
        /// <param name="limit">Số lượng tối đa cần lấy.</param>
        Task<IEnumerable<OcrTrialTrans>> GetPendingUploadBillsAsync(DateTime? fromDate = null, DateTime? toDate = null, int limit = 50);

        /// <summary>
        /// Cập nhật kết quả bóc tách OCR vào bảng mssOCRTrialTrans và chuyển DataStatus = 'OCR'.
        /// </summary>
        Task<bool> UpdateOcrScanResultAsync(
            Guid billId, 
            string? outletNumber, 
            string? brandNumber, 
            string? outletName, 
            string? brandName, 
            string? billNumber, 
            string? billDateTime, 
            string? billTotal, 
            string? cashierName,
            string? rawOcrResponse = null);

        /// <summary>
        /// Khởi tạo và đồng bộ cấu trúc bảng mssOCRTrialTrans nếu chưa tồn tại trên CSDL.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Tự động khởi tạo schema CSDL (Schema Migration Guard).
        /// - MỤC ĐÍCH: Đảm bảo bảng và các index cần thiết sẵn sàng hoạt động mà không bị lỗi Missing Table.
        /// - CÁCH DÙNG: Được gọi trong quá trình khởi động ứng dụng (Startup).
        /// </remarks>
        Task EnsureTableSchemaAsync();

        /// <summary>
        /// Truy vấn danh sách hóa đơn phân trang nâng cao hỗ trợ bộ lọc Header (Ngày, Gian hàng, Thương hiệu, Tìm kiếm, Trạng thái).
        /// </summary>
        Task<(IEnumerable<OcrTrialTrans> Items, int TotalCount)> GetBillsPagedAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? brandNumber = null,
            string? outletNumber = null,
            string? keyword = null,
            bool? ocrStatus = null,
            bool? reviewed = null,
            bool? approved = null,
            int pageIndex = 1,
            int pageSize = 25);

        /// <summary>
        /// Lấy danh mục thương hiệu (Brands) phục vụ bộ lọc Header
        /// </summary>
        Task<IEnumerable<string>> GetDistinctBrandsAsync();

        /// <summary>
        /// Lấy danh mục gian hàng (Outlets) phục vụ bộ lọc Header
        /// </summary>
        Task<IEnumerable<(string OutletNumber, string OutletName, string BrandNumber)>> GetDistinctOutletsAsync();
    }
}
