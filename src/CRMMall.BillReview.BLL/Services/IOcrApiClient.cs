using System;
using System.Threading.Tasks;
using CRMMall.BillReview.BLL.DTOs;

namespace CRMMall.BillReview.BLL.Services
{
    /// <summary>
    /// Giao diện dịch vụ kết nối Máy chủ OCR Chuyên dụng độc lập (AI VLM Server).
    /// </summary>
    public interface IOcrApiClient
    {
        /// <summary>
        /// [API 3] Bóc tách hóa đơn trực tiếp bằng AI (POST api/v1/extract-bill).
        /// Gửi tệp ảnh thực tế đã lưu trong CRM qua Multipart Form Data và nhận ngay kết quả JSON bóc tách tức thì.
        /// </summary>
        /// <param name="imageBytes">Mảng byte ảnh hóa đơn thực tế.</param>
        /// <param name="fileName">Tên file ảnh (vd: 11604_181026110950.png).</param>
        /// <param name="memId">Mã hội viên sở hữu hóa đơn.</param>
        /// <param name="imageNumber">Mã định danh hóa đơn (bill.ID).</param>
        /// <param name="duplicateAction">Hành vi xử lý trùng: "allow", "warn", "reject".</param>
        /// <returns>Dữ liệu bóc tách đồng bộ dạng cấu trúc từ AI Server.</returns>
        Task<OcrExtractApiResponse> ExtractBillAsync(
            byte[] imageBytes, 
            string fileName, 
            string memId = "", 
            string imageNumber = "", 
            string duplicateAction = "allow");

        /// <summary>
        /// [API 2] Bóc tách thông tin hóa đơn dạng chuỗi Base64.
        /// </summary>
        Task<OcrExtractApiResponse> ExtractBillBase64Async(
            string imageBase64, 
            string fileName, 
            string memId = "", 
            string duplicateAction = "allow");

        /// <summary>
        /// Kiểm tra trạng thái hoạt động của máy chủ OCR (Health Check).
        /// </summary>
        Task<bool> CheckHealthAsync();
    }
}
