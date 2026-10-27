# ĐẶC TẢ TIẾP NHẬN HÓA ĐƠN & LƯU TRỮ HÌNH ẢNH (BILL INGESTION & STORAGE)
## BƯỚC 1: API TIẾP NHẬN MOBILE, NAMING CONVENTION VÀ QUẢN LÝ TẬP TIN ẢNH

> 🏢 **Hệ sinh thái**: CRM Loyalty Gigamall - Sao Mai Solution  
> 🎯 **Mục tiêu**: Bảo toàn 100% luồng tiếp nhận hiện tại từ Mobile App, chuẩn hóa quy tắc đặt tên file, đường dẫn vật lý và cơ chế phục vụ ảnh an toàn.  
> 🏛️ **Dự án cũ tham khảo**: `D:\GIT PROJECT\CRM SAOMAI\CRMPROFILELogic\SourceCode\CRMProfileManagement`  
> 📁 **Tệp nguồn tham khảo**: `CRMAPI/Controllers/MobOCRController.cs` & `CoreAPI/Controllers/MobTransController.cs`

---

## 🔸 PHẦN 1: HIỆN TRẠNG HỆ THỐNG CŨ (AS-IS / LEGACY ANALYSIS)

### 1.1. Các kênh API tiếp nhận hiện tại
Hệ thống cũ có 2 API tiếp nhận song song phục vụ ứng dụng Gigamall và Core Mobile:
* **Kênh Gigamall**: `POST /GiGa/Mob/OCR/SaveImage` thuộc controller `MobOCRController.cs` (Project `CRMAPI`).
* **Kênh Core Mobile**: `POST /api/Mob/Trans/UploadBill` thuộc controller `MobTransController.cs` (Project `CoreAPI`).

### 1.2. Hợp đồng Request DTO (`IOCRInput`)
Mobile App gửi gói tin JSON chứa ảnh Base64:
```json
{
  "AppName": "GiGa",
  "Language": "VN",
  "MemID": "0901234567",
  "Type": 0,
  "BrandNumber": "",
  "OutletNumber": "",
  "ImageBase64": "/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8UHRofHh0aHBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/...",
  "TokenInfo": {
    "Token": "d41d8cd98f00b204e9800998ecf8427e",
    "SecretKey": "..."
  }
}
```

### 1.3. Naming Convention & Lưu trữ vật lý
Trong hàm `Common.SaveOCRImage`:
1. **Thư mục con ngày tháng (`subFolder`)**: Định dạng `Transaction/{ddMMyy}` (Ví dụ: `Transaction/241026`).
2. **Quy tắc đặt tên file (`ImageFileName`)**: Công thức `{MemID}_{ddMMyyHHmmss}.png` (Ví dụ: `0901234567_241026101530.png`).
3. **Đường dẫn thư mục gốc (`BaseImagePath`)**:
   - Production: `D:\Project\CRMAPI\Images\Bill\`
   - Test/Dev: `D:\GGDrive\TEST_IMAGES\`
4. **Đường dẫn ảnh đầy đủ (Full Path)**:
   ```csharp
   fullImagePath = Path.Combine(BaseImagePath, dbimagePath, ImageFileName);
   ```
5. **Di chuyển ảnh khi duyệt hoàn tất**: Khi duyệt xong, hệ thống cũ dùng `File.Move` di chuyển ảnh vào thư mục con `OCR`:
   `Path.Combine(BaseImagePath, dbimagePath, "OCR", ImageFileName)`

---

## 🔹 PHẦN 2: THIẾT KẾ HỆ THỐNG MỚI (TO-BE / MODERN SPECIFICATION)

### 2.1. Nguyên tắc cốt lõi: Bảo toàn 100% hợp đồng API
* **Không thay đổi hợp đồng DTO**: Giữ nguyên toàn bộ cấu trúc request của `IOCRInput` và response `BODataProcessResult`. Mobile App ngoài Production không cần cập nhật ứng dụng vẫn hoạt động bình thường.
* **Phản hồi tức thì (Non-blocking)**: Ngay sau khi giải mã Base64 và lưu file thành công, API INSERT bản ghi vào `mssOCRTrans` với `OCRStatus = 0`, `ReviewStatus = 0`, `DataStatus = 'Pending'` và phản hồi ngay mã 200 OK cho Mobile App:
```json
{
  "OK": true,
  "ErrorNumber": 0,
  "Message": "Yêu cầu tích điểm của bạn đã được tiếp nhận và đang được xử lý.",
  "Content": {
    "ImagePath": "http://crm.gigamall.vn/Uploads/Bill/Transaction/241026/0901234567_241026101530.png"
  }
}
```

### 2.2. Dịch vụ lưu trữ & Phục vụ ảnh trong .NET 8 (`IBillStorageService`)
Xây dựng interface quản lý lưu trữ độc lập trong `CRMMall.BillReview.BLL`:
```csharp
public interface IBillStorageService
{
    Task<string> SaveBase64ImageAsync(string memId, string base64Data, string subFolder);
    string GetPhysicalPath(string imagePath, string fileName);
    Task<byte[]> GetImageBytesAsync(string imagePath, string fileName);
    Task MoveToReviewedFolderAsync(string imagePath, string fileName);
}
```

### 2.3. Endpoint phục vụ ảnh trực quan cho Web Console
Để giao diện Web UI hiển thị được ảnh mà không bị lộ đường dẫn vật lý máy chủ, Backend cung cấp Endpoint:
* `GET /api/bills/{id}/image`: Trả về luồng nhị phân `image/png` hoặc `image/jpeg` kèm Header Cache để tối ưu tốc độ tải ảnh trên trình duyệt.


### 4. Quy Tắc Xây Dựng Đường Dẫn Tệp Ảnh Đầy Đủ (Image Full Path Resolution)
- **Công thức chuẩn**:
  ```
  fullImagePath = basePath + DbPath + ImageFileName
  ```
- **Trong đó**:
  - `basePath`: Cấu hình trong `appsettings.json` theo môi trường:
    * DEV: `D:\GGDrive\TEST_IMAGES\`
    * PROD: `D:\Project\CRMAPI\Images\Bill\`
  - `DbPath`: Đọc từ trường `ImagePath` trong bảng `mssOCRTrialTrans` (quy ước thư mục theo ngày `Transaction/ddMMyy`, ví dụ `Transaction/020926`).
  - `ImageFileName`: Đọc từ trường `ImageFileName` trong bảng `mssOCRTrialTrans` (ví dụ `35986_020926001257.png`).
- **Ví dụ kết quả cho ngày 02/09/2026**:
  `D:\GGDrive\TEST_IMAGES\Transaction\020926\35986_020926001257.png`

### 5. Tính Năng Xoay & Ghi Đè Ảnh Trực Tiếp Xuống Đĩa Cứng (Rotate & Overwrite Image)
- **Vấn đề**: Khi khách hàng chụp ảnh hóa đơn bị xoay ngang/ngược hướng, AI OCR (Qwen2-VL) quét text theo chiều ngang thông thường sẽ bóc tách sai lệch.
- **Giải pháp**:
  - Trên Modal xem ảnh: Kiểm toán viên bấm `[🔄 Xoay 90°]` để xem trước hướng thẳng chuẩn trên giao diện.
  - Bấm nút **`[💾 Lưu Ảnh]`**: Giao diện HTML5 Canvas xuất ảnh đang hiển thị và gọi `POST /api/bills/{id}/save-image`.
  - Backend ghi đè trực tiếp (`File.WriteAllBytesAsync`) vào tệp ảnh gốc trên đĩa cứng.
  - Khi bấm `[🔍 Gọi OCR]`, tệp ảnh đã chuẩn hướng được gửi sang AI server, mang lại độ chính xác 100%.
