# ĐẶC TẢ GIAO DIỆN ĐỐI SOÁT & THẨM ĐỊNH HÓA ĐƠN (ADMIN REVIEW CONSOLE)
## BƯỚC 3: MÀN HÌNH WEB 2 TAB, IMAGE VIEWER TƯƠNG TÁC VÀ ĐIỀU CHỈNH TRƯỜNG ...A

> 🏢 **Hệ sinh thái**: CRM Loyalty Gigamall - Sao Mai Solution  
> 🎯 **Mục tiêu**: Xây dựng bảng điều khiển Web Console hiện đại thay thế WinForms cũ, tổ chức thành 2 Tab riêng biệt, Image Viewer đa năng (Zoom/Pan/Rotate) và cơ chế thẩm định chốt số Final.  
> 🏛️ **Dự án cũ tham khảo**: `D:\GIT PROJECT\CRM SAOMAI\CRMPROFILELogic\SourceCode\CRMProfileManagement`  
> 📁 **Tệp nguồn tham khảo**: `OCRBillProcess/frmOCRBillReview.cs` & `OCRBillProcess/frmZoomPic.cs`

---

## 🔸 PHẦN 1: HIỆN TRẠNG HỆ THỐNG CŨ (AS-IS / LEGACY ANALYSIS)

### 1.1. Cấu trúc màn hình WinForms Desktop cũ (`frmOCRBillReview.cs`)
1. **Kiến trúc MDI Desktop**:
   - Sử dụng `SplitContainer` chia đôi màn hình: Bên trái là `DataGridView` (`dgvOCRTrialTrans`), bên phải là các `TextBox` và `PictureBox` (`pictureBox1`).
   - Phải cài đặt trực tiếp trên từng máy tính Windows của nhân viên CSKH/kiểm toán viên.
2. **Xem ảnh hạn chế**:
   - Khung xem ảnh nhỏ, khi cần phóng to phải bấm nút `btnZoom` để bật một cửa sổ popup độc lập (`frmZoomPic.cs`), không cho phép kéo rê (Pan) hoặc xoay ảnh (Rotate) trực tiếp mượt mà.
3. **Trộn lẫn quy trình bóc tách và duyệt**:
   - Một màn hình vừa thực hiện nhận dạng OCR (nút `btnOCR`), vừa hiển thị kết quả, vừa cho phép duyệt (`btnApproved`) hoặc từ chối (`btnReject`), không phân tách rõ rệt giữa hóa đơn vừa upload và hóa đơn đã bóc tách xong.
4. **Xử lý blocking UI**:
   - Khi tải ảnh lớn hoặc gọi API Google Vision, toàn bộ giao diện WinForms bị đóng băng (Not Responding) do chạy đồng bộ trên Main UI Thread.

---

## 🔹 PHẦN 2: THIẾT KẾ HỆ THỐNG MỚI (TO-BE / MODERN SPECIFICATION)

### 2.1. Kiến trúc Giao diện Web Console Hiện đại (`CRMMall.BillReview.GUI`)
Giao diện được xây dựng trên nền tảng Web responsive, chia thành 3 khối mạch lạc:

```
┌────────────────────────────────────────────────────────────────────────────────────────────────┐
│ [HEADER CHUNG]: BỘ LỌC TẬP TRUNG & TÌM KIẾM NHANH                                              │
│ [ Từ ngày - Đến ngày ] [ Dropdown Brand ] [ Dropdown Outlet ] [ Dropdown Status ] [ Tìm MemID ]│
├────────────────────────────────────────────────────────────────────────────────────────────────┤
│ [ĐIỀU HƯỚNG TAB]:                                                                              │
│   ├── 🔵 TAB 1: THỰC HIỆN BÓC TÁCH OCR (Bill mới upload: OCRStatus = 0/null)                    │
│   └── 🟢 TAB 2: ĐỐI SOÁT & DUYỆT BILL  (Bill đã OCR: OCRStatus = 1 & ReviewStatus = 0)         │
├────────────────────────────────────────────────────────────────────────────────────────────────┤
│ [MODAL CHI TIẾT BILL DÙNG CHUNG (INTERACTIVE BILL DETAIL MODAL)]:                              │
│   ┌────────────────────────────────────────┬─────────────────────────────────────────────────┐ │
│   │ KHUNG XEM ẢNH HÓA ĐƠN TƯƠNG TÁC        │ KHU VỰC THÔNG TIN DỮ LIỆU ĐỐI SOÁT              │ │
│   │ - Phóng to / Thu nhỏ (Zoom In/Out)     │ ┌──────────────────────┐ ┌────────────────────┐ │ │
│   │ - Kéo di chuyển ảnh tự do (Pan/Drag)   │ │ VÙNG 1: AI (OCR)     │ │ VÙNG 2: ADMIN (...A)│ │ │
│   │ - Xoay ảnh 90° / 180° / 270° (Rotate)  │ │ (Chỉ đọc - Readonly) │ │ (Cho phép chỉnh sửa)│ │ │
│   │                                        │ └──────────────────────┘ └────────────────────┘ │ │
│   │                                        │ [Ghi chú thẩm định - ReviewNotes Textarea]      │ │
│   │                                        │ 🔘 Nút [Đồng ý / Approved] 🔘 Nút [Từ chối]     │ │
│   └────────────────────────────────────────┴─────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────────────────────────────────────────┘
```

### 2.2. Chi tiết Khối Header Dùng Chung (Common Header Component)
* **Bộ lọc ngày (`fromDate` - `toDate`)**: Cho phép chọn khoảng thời gian upload hoặc ngày giao dịch.
* **Dropdown Thương hiệu (`cbBrand`) & Gian hàng (`cbOutlet`)**: Cascading dropdown (chọn Brand lọc danh sách Outlet tương ứng).
* **Dropdown Trạng thái (`cbStatus`)**:
  - Trạng thái OCR: `Tất cả`, `Chưa OCR`, `Đã OCR`.
  - Trạng thái Duyệt: `Tất cả`, `Chờ duyệt`, `Đã duyệt (Approved)`, `Đã từ chối (Rejected)`.
* **Thanh tìm kiếm (`txtSearch`)**: Tìm tức thời theo `MemID` (Số điện thoại) hoặc tên gian hàng.
* **Phân trang (`Pagination`)**: Tùy chọn 10, 20, 50 dòng/trang; chuyển trang nhanh và hiển thị tổng số bản ghi.

### 2.3. Chi tiết TAB 1: Thực hiện bóc tách OCR
* **Điều kiện lọc dữ liệu**: `(OCRStatus = 0 OR OCRStatus IS NULL) AND (Reviewed = 0 OR Reviewed IS NULL)`.
* **Grid hiển thị**: Cột mã hóa đơn, `MemID`, thời gian upload, ảnh thumbnail xem trước.
* **Thao tác trên từng dòng**: Bấm vào dòng mở Modal Chi tiết Bill.
* **Hành động trong Modal Tab 1**:
  - Nút **[OCR]**: Gửi ảnh tới OCR Server (`/api/v1/extract-bill`), hiển thị spinner và đổ kết quả bóc tách lên **Vùng 1: AI (OCR)**.
  - Vùng 2: Admin Adjust hiển thị nhưng tạm vô hiệu hóa (disabled).
  - Nút **[Save OCR]**: Gọi API Backend lưu dữ liệu bóc tách vào `mssOCRTrans`, ghi nhận `OCRStatus = 1`, đóng modal. Bill tự động chuyển sang Tab 2.

### 2.4. Chi tiết TAB 2: Duyệt bill đã OCR (Admin Review)
* **Điều kiện lọc dữ liệu**: `OCRStatus = 1 AND (Reviewed = 0 OR Reviewed IS NULL)`.
* **Grid hiển thị**: Cột `MemID`, Gian hàng OCR, Số bill OCR, Ngày giờ OCR, Tổng tiền OCR, nút [Xem chi tiết & Duyệt].
* **Thao tác trong Modal Tab 2**:
  - **Vùng 1: AI (OCR)**: Hiển thị toàn bộ dữ liệu máy đọc ban đầu (Read-only, không thể chỉnh sửa).
  - **Vùng 2: Admin Adjust**: Mở khóa toàn bộ các trường có tiếp vị ngữ `...A`:
    * `BrandNumberA` (Dropdown / Textbox)
    * `OutletNumberA` (Dropdown / Textbox)
    * `BillNumberA` (Textbox)
    * `BillDateTimeA` (Datetime Picker)
    * `BillTotalA` (Numeric Textbox)
  - Hộp nhập **`ReviewNotes`**: Nhập lý do (Bắt buộc nhập nếu Từ chối).
* **Nút quyết định thẩm định**:
  1. **Nút [Đồng ý] (`Approved = 1`)**:
     - Kiểm tra trùng lặp: `IsDuplicateOCRBill(OutletNumberA, BillNumberA, BillDateTimeA)`.
     - Cập nhật CSDL: Lưu các trường `...A`, `Reviewed = 1`, `Approved = 1`, `ReviewBy = CurrentUser`, `ReviewDate = GETDATE()`.
     - Đóng băng toàn bộ form sang chế độ Read-only.
  2. **Nút [Từ chối] (`Approved = 0`)**:
     - Kiểm tra bắt buộc phải có `ReviewNotes`.
     - Cập nhật CSDL: `Reviewed = 1`, `Approved = 0`, `ReviewBy = CurrentUser`, `ReviewDate = GETDATE()`, `ReviewNotes = ...`.
     - Gửi Push Notification thông báo lý do từ chối đến điện thoại hội viên.
