namespace CRMMall.BillReview.BLL.Configurations
{
    /// <summary>
    /// Cấu hình kết nối tới Hệ thống OCR AI Chuyên dụng Gigamall.
    /// Toàn bộ thông số được đọc từ appsettings.json, không hardcode trong mã nguồn.
    /// </summary>
    public class OcrSettings
    {
        /// <summary>Địa chỉ gốc của máy chủ OCR (vd: http://14.161.34.37:8080)</summary>
        public string BaseUrl { get; set; } = "http://14.161.34.37:8080";

        /// <summary>Thời gian timeout tối đa cho mỗi lượt gọi (giây)</summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>Khóa bảo mật hoặc Bearer Token (nếu sau này hệ thống kích hoạt xác thực)</summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>Hành vi xử lý khi phát hiện trùng ảnh: "allow", "warn", "reject"</summary>
        public string DuplicateAction { get; set; } = "allow";

        /// <summary>Danh sách đầy đủ 10 Endpoints của hệ sinh thái OCR AI</summary>
        public OcrEndpointsSettings Endpoints { get; set; } = new();
    }

    /// <summary>
    /// Chi tiết 10 Endpoints OCR theo chuẩn CRM_API_INTEGRATION_SPEC.md
    /// </summary>
    public class OcrEndpointsSettings
    {
        /// <summary>1. Bóc tách hóa đơn trực tiếp (Core AI bóc tách tức thì - dùng cho nút Gọi OCR)</summary>
        public string ExtractBill { get; set; } = "api/v1/extract-bill";

        /// <summary>2. Upload ảnh hóa đơn qua Multipart Form</summary>
        public string UploadMultipart { get; set; } = "api/v1/bills/upload";

        /// <summary>3. Upload ảnh hóa đơn dạng Base64 JSON</summary>
        public string UploadBase64 { get; set; } = "api/v1/bills/upload-base64";

        /// <summary>4. Kích hoạt chạy OCR hàng loạt (Trigger Batch OCR)</summary>
        public string TriggerBatchOcr { get; set; } = "api/v1/bills/trigger-ocr";

        /// <summary>5. Kích hoạt OCR cho 1 hóa đơn cụ thể</summary>
        public string TriggerSingleOcr { get; set; } = "api/v1/bills/{id}/trigger-ocr";

        /// <summary>6. Tra cứu chi tiết hóa đơn & Audit Logs</summary>
        public string GetBillDetail { get; set; } = "api/bills/{bill_id}";

        /// <summary>7. Phê duyệt hoặc Từ chối hóa đơn</summary>
        public string SubmitReview { get; set; } = "api/bills/{bill_id}/review";

        /// <summary>8. Chỉnh sửa thông tin hóa đơn (Admin/CSKH)</summary>
        public string EditBill { get; set; } = "api/bills/{bill_id}/edit";

        /// <summary>9. Danh mục 81 gian hàng chuẩn Gigamall</summary>
        public string GetOutlets { get; set; } = "api/outlets";

        /// <summary>10. Xem ảnh hóa đơn</summary>
        public string GetImage { get; set; } = "api/images/{filename}";
    }
}
