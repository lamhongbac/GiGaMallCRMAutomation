using System;

namespace CRMMall.BillReview.BLL.Configurations
{
    /// <summary>
    /// Đối tượng cấu hình toàn cục tầm vực Application (Singleton).
    /// Quản trị viên chỉ cần thay đổi giá trị Env ("DEV" hoặc "PROD") tại appsettings.json,
    /// toàn bộ kết nối CSDL, đường dẫn ảnh và hành vi nghiệp vụ sẽ tự động chuyển đổi đồng bộ.
    /// </summary>
    public class ApplicationSetting
    {
        /// <summary>Giá trị môi trường: "DEV" hoặc "PROD"</summary>
        public string Env { get; set; } = "DEV";

        /// <summary>Nhận diện hệ thống đang chạy ở môi trường Production hay không</summary>
        public bool IsProduction => string.Equals(Env, "PROD", StringComparison.OrdinalIgnoreCase);

        /// <summary>Chuỗi kết nối CSDL tự động phân giải theo môi trường hiện hành</summary>
        public string ActiveConnectionString => IsProduction 
            ? ConnectionStrings.PRODConnection 
            : ConnectionStrings.DEVConnection;

        /// <summary>Nhãn hiển thị môi trường trên giao diện</summary>
        public string Label => IsProduction ? PROD.Label : DEV.Label;

        /// <summary>Thư mục gốc lưu trữ ảnh hóa đơn theo môi trường (BaseImagePath)</summary>
        public string BaseImagePath => IsProduction ? PROD.BaseImagePath : DEV.BaseImagePath;

        /// <summary>Alias tương thích ngược cho BaseImagePath</summary>
        public string ImageBasePath => BaseImagePath;

        /// <summary>Địa chỉ máy chủ OCR AI theo môi trường</summary>
        public string OcrBaseUrl => IsProduction ? PROD.OcrBaseUrl : DEV.OcrBaseUrl;

        /// <summary>Cặp chuỗi kết nối DEV và PROD</summary>
        public ConnectionStringsConfig ConnectionStrings { get; set; } = new();

        /// <summary>Nhóm cấu hình môi trường DEV</summary>
        public EnvProfileConfig DEV { get; set; } = new();

        /// <summary>Nhóm cấu hình môi trường PROD</summary>
        public EnvProfileConfig PROD { get; set; } = new();

        /// <summary>Nhóm cấu hình chung dùng đúng cho mọi môi trường</summary>
        public CommonConfig CommonSettings { get; set; } = new();

        /// <summary>Nhóm cấu hình hỗ trợ gỡ lỗi và kiểm thử nhanh</summary>
        public DebugConfig DebugSettings { get; set; } = new();
    }

    public class ConnectionStringsConfig
    {
        public string DEVConnection { get; set; } = string.Empty;
        public string PRODConnection { get; set; } = string.Empty;
    }

    public class EnvProfileConfig
    {
        public string Label { get; set; } = string.Empty;
        /// <summary>Đường dẫn gốc của folder chứa image (VD: D:\Project\CRMAPI\Images\Bill\ trên PROD, D:\GGDrive\TEST_IMAGES\ trên DEV)</summary>
        public string BaseImagePath { get; set; } = string.Empty;
        /// <summary>Alias tương thích ngược</summary>
        public string ImageBasePath { get => BaseImagePath; set => BaseImagePath = value; }
        public string OcrBaseUrl { get; set; } = "http://14.161.34.37:8080";
    }

    public class CommonConfig
    {
        public string OCRImageFolder { get; set; } = "OCR";
        public string DuplicateAction { get; set; } = "allow";
        public int TimeoutSeconds { get; set; } = 30;
        public OcrEndpointsSettings Endpoints { get; set; } = new();
    }

    public class DebugConfig
    {
        public string? DefaultFromDate { get; set; }
        public string? DefaultToDate { get; set; }
    }
}
