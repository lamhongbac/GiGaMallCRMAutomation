# NHẬT KÝ BÁO CÁO CÔNG VIỆC CUỐI NGÀY (DAILY EOD REPORTS)
## DỰ ÁN CRMMALL - GIGAMALL MIGRATION

> 📅 **Mục đích**: Lưu trữ tích lũy các báo cáo tổng kết cuối ngày (End-of-Day Reports) của từng ngày làm việc, giúp nắm bắt tiến độ, vấn đề phát sinh và kế hoạch ngày kế tiếp.

---

## 📅 BÁO CÁO NGÀY: 26/10/2026

* **Người thực hiện**: Antigravity Pair Programmer
* **Người xác nhận**: User (Đã manual test và xác nhận nghiệm thu toàn bộ các task trong ngày)
* **Trọng tâm trong ngày**: Khảo sát mã nguồn dự án cũ (`CRM SAOMAI`), chuẩn hóa quy tắc phát triển, kiến tạo bộ tài liệu kỹ thuật hoàn chỉnh 4 bước, và triển khai thành công 3 tính năng giao diện trọng yếu (`UI-HEADER`, `UI-TAB1`, `IMG-SAVE`) cùng cơ chế xoay & ghi đè ảnh vật lý xuống đĩa cứng.

### 1. Các công việc đã hoàn thành và nghiệm thu trong ngày (DONE):
- [x] **[DOCS-INIT] Chuẩn hóa toàn bộ hệ thống tài liệu dự án**:
  - Khởi tạo [WORKING_RULES.md](WORKING_RULES.md): Kim chỉ nam quy tắc làm việc cho Agent (Plan First, Execute Only On "OK", Vòng đời 4 bước `PENDING` $\rightarrow$ `DOING` $\rightarrow$ `CONFIRMING` $\rightarrow$ `DONE`).
  - Khởi tạo [TECHNICAL_DOCUMENT.md](TECHNICAL_DOCUMENT.md) đóng vai trò Master Catalog tổng thể (4 bước nghiệp vụ, quy tắc Audit Rule, Final Number Rule).
  - Khởi tạo [DEV_PLAN.md](DEV_PLAN.md) và [DEV_HISTORY.md](DEV_HISTORY.md).
  - Khởi tạo 6 tài liệu chuyên đề chi tiết trong thư mục `docs/` với đầy đủ 2 phần As-Is (Hệ thống cũ) và To-Be (Thiết kế mới).
- [x] **[UI-HEADER] Xây dựng thanh công cụ lọc Header dùng chung**:
  - Bộ lọc Từ ngày - Đến ngày, Dropdown Brand, Dropdown Outlet cascading theo Brand, Dropdown trạng thái OCR/Review, Ô tìm kiếm tự do MemID/Gian hàng.
  - Tầng DAL, BLL và API `/api/bills/filter-options` và `/api/bills/pending-upload` phân trang SQL Server (`OFFSET ... FETCH`).
- [x] **[UI-TAB1] Nâng cấp Tab 1 Thực hiện OCR (Interactive AI Console)**:
  - Bảng dữ liệu lọc các hóa đơn chờ xử lý OCR (`OCRStatus = 0 / NULL`).
  - Modal xem ảnh chi tiết với Image Viewer: Phóng to (Zoom In/Out), Xoay 90°, Kéo rê (Pan/Drag mousedown), Điều hướng Bill Trước/Sau (Prev/Next).
  - Phân định rõ 2 Vùng nghiệp vụ: **Vùng 1 (AI OCR trích xuất tự động)** vs **Vùng 2 (Admin Adjust `...A` khóa bảo vệ `🔒 Mở khóa tại Tab 2`)**.
  - Các nút hành động: `[🔍 Gọi OCR]`, `[💾 Lưu OCR]` (chuyển `OCRStatus = 1` và đẩy bill sang Tab 2), `[⚡ 1-Click OCR & Lưu]`.
- [x] **[IMG-SAVE] Xoay & Ghi đè file ảnh trực tiếp xuống đĩa cứng & Cấu hình Debug ngày**:
  - Bổ sung nút **`[💾 Lưu Ảnh]`** trên thanh công cụ xem ảnh: Trích xuất ảnh đang hiển thị trên giao diện (sau khi xoay thẳng góc) qua HTML5 Canvas và gửi lên server ghi đè trực tiếp xuống file vật lý trên đĩa cứng (`fullImagePath = basePath + DbPath + ImageFile`).
  - Củng cố công thức đường dẫn ảnh với `TrimStart('\', '/')` chống lỗi root drive.
  - Bổ sung cấu hình `DebugSettings` (`"DefaultFromDate": "2026-09-02"`, `"DefaultToDate": "2026-09-02"`) trong `appsettings.json` và xử lý khởi động trang tự động điền 2 ô Từ ngày - Đến ngày, nạp ngay 149 hóa đơn có sẵn ảnh thật trên ổ đĩa.
  - **Nghiệm thu**: User đã trực tiếp test và xác nhận hoàn thành xuất sắc lúc 17:18 ngày 26/10/2026.

### 2. Kế hoạch công việc cho ngày mai (27/10/2026):
1. **Task `IMG-ZOOM-CROP`** *(Ghi nhận theo yêu cầu mới của User)*:
   - Khi kiểm toán viên phóng to (zoom) và kéo rê (pan) ảnh hóa đơn đến vùng trọng tâm trên màn hình và bấm nút `[💾 Lưu Ảnh]`, hệ thống sẽ trích xuất và lưu lại đúng vùng ảnh đã zoom/crop đó xuống đĩa cứng.
   - Giúp loại bỏ các phần biên thừa, hỗ trợ mô hình AI OCR tập trung nhận diện chính xác các dòng chữ hóa đơn bị nhỏ hoặc mờ.
2. **Task `UI-TAB2` (Thẩm Định & Đối Soát Hóa Đơn - Admin Review & Adjust)**:
   - Lọc các hóa đơn đã bóc tách OCR chờ kiểm toán viên duyệt (`OCRStatus = 1` & `Reviewed = 0 / NULL`).
   - Mở khóa Vùng 2 Admin Adjust trong Modal cho phép chỉnh sửa các trường `...A` (`OutletNumberA`, `BillNumberA`, `BillTotalA`, `BillDateTimeA`, `ReviewNotes`).
   - Xử lý 2 nút bấm: `[✅ Phê Duyệt]` (`Approved = 1`, `Reviewed = 1`) và `[❌ Từ Chối]` (`Approved = 0`, `Reviewed = 1`, bắt buộc nhập lý do từ chối).
   - Đóng băng hóa đơn sau khi duyệt thành Read-only và chuyển sang hàng đợi tích điểm `CRMProcess`.

---
*Báo cáo được chốt vào lúc 17:25 ngày 26/10/2026 bởi Antigravity Pair Programmer.*

---

## 📅 BÁO CÁO NGÀY: 27/10/2026

* **Người thực hiện**: Antigravity Pair Programmer
* **Người xác nhận**: User (Đã manual test và xác nhận nghiệm thu toàn bộ các task trong ngày)
* **Trọng tâm trong ngày**: Hoàn thiện toàn diện tính năng Lưu đúng vùng ảnh Zoom/Crop xuống ổ đĩa (`IMG-ZOOM-CROP`), xây dựng và chuẩn hóa toàn diện Bàn Thẩm Định & Đối Soát Tab 2 (`UI-TAB2`), bảo toàn tuyệt đối nguyên tắc Audit Fields `...A`, sửa lỗi tương phản CSS nhập liệu, hoàn thiện luồng Từ chối hóa đơn, và chốt toàn bộ Kế hoạch chi tiết cho Phase 4 (`CRM-PROC`) kèm cơ chế Notification Mock Mode.

### 1. Các công việc đã hoàn thành và nghiệm thu trong ngày (DONE):
- [x] **[IMG-ZOOM-CROP] Lưu đúng vùng ảnh đã Zoom/Pan/Crop trực tiếp xuống đĩa cứng**:
  - Trích xuất tọa độ viewport của ảnh sau khi phóng to và kéo rê, tính toán crop rect qua HTML5 Canvas và gửi API ghi đè file ảnh vật lý trên đĩa cứng.
  - Xử lý triệt để bug trạng thái nút bấm Lưu ảnh (trả về trạng thái bình thường sau khi lưu xong).
  - **Nghiệm thu**: User đã test trực tiếp trên trình duyệt và xác nhận hoàn thành xuất sắc.
- [x] **[UI-TAB2] Hoàn thiện toàn diện Tab 2: Bàn Thẩm Định & Đối Soát Hóa Đơn**:
  - **Hàng đợi hóa đơn Tab 2**: Lọc danh sách các hóa đơn đã bóc tách OCR (`OCRStatus = 1 AND (Reviewed = 0 OR Reviewed IS NULL)`).
  - **Bàn đối soát tích hợp Image Viewer**: Hỗ trợ đầy đủ phóng to/thu nhỏ (Zoom +/- cuộn chuột), kéo rê (Pan/Drag), xoay (Rotate 90°) trực tiếp trên ảnh hóa đơn tại Tab 2.
  - **Bảng đối soát 2 cột OCR vs Admin Adjust**:
    * Không cần nút "Sao chép OCR": Các ô nhập `...A` (`txtOutletA`, `txtBillNoA`, `txtTotalA`, `txtDateA`) để trống mặc định. Admin chỉ nhập khi OCR sai, nếu OCR đúng thì để trống.
    * Bảng **Số liệu chốt duyệt (Final Summary Box)** real-time với nhãn nhận diện nguồn `[✏️ Admin]` vs `[🤖 OCR]`.
  - **Hàm tính toán & Validation độc lập**:
    * Hàm `computeFinalBillData()`: Tính toán theo quy tắc ưu tiên `FinalValue = AdminValueA ?? OcrValue`.
    * Hàm `validateFinalBillData(finalData, options)`: Độc lập trả về `boolean`, kiểm tra `FinalTotal > 0`, gian hàng, số bill và ngày giờ không vượt quá hiện tại.
  - **Bảo toàn nguyên tắc Audit Fields `...A` cốt lõi**:
    * Sửa triệt để lỗi vô tình copy đè số OCR vào các cột `_A` trong CSDL.
    * Đảm bảo hệ thống **CHỈ GHI NHẬN ĐÚNG NHỮNG GÌ ADMIN GÕ VÀO**. Nếu ô nào Admin không nhập (để trống), trường `...A` tương ứng (`BillNumberA`, `OutletNumberA`, `BillDateTimeA`, `BrandNumberA`) trong CSDL **BẮT BUỘC LÀ `NULL`**.
    * Hàm chống trùng hóa đơn `IsBillNumberExistsAsync` sử dụng `COALESCE(OutletNumberA, OutletNumber)` và `COALESCE(BillNumberA, BillNumber)` để chống trùng chính xác cho cả số OCR lẫn số Admin sửa.
  - **Sửa lỗi tương phản giao diện (CSS Contrast)**:
    * Khắc phục triệt để tình trạng màu nền và màu chữ cùng màu đen tối khiến không nhìn thấy con số gõ vào.
    * Thiết lập màu chữ trắng sáng `#ffffff !important` và `-webkit-text-fill-color: #ffffff !important` trên nền navy tối sâu `#0b1329`.
    * Ô số tiền `#txtTotalA` có màu xanh ngọc lục bảo `#34d399 !important` với font size 15px, font-weight 700 nổi bật. Con trỏ gõ màu cyan `#38bdf8` phát sáng.
  - **Nút [✓ Phê Duyệt] (`Approve`)**: Luôn gọi hàm validation trước, cập nhật `Approved = 1`, `Reviewed = 1`, `DataStatus = 'Approved'`.
  - **Nút [✕ Từ Chối] (`Reject`) & Modal Từ Chối**:
    * Modal từ chối với danh sách lý do mẫu thường dùng và ô nhập chi tiết.
    * Khắc phục triệt để lỗi không đóng dialog sau khi bấm từ chối (nguyên nhân do `dbo.mssMobileTracking` chưa có trong DB test dẫn đến ngoại lệ 500 khi gửi notification).
    * Tạm REM gửi push notification cho member trong giai đoạn test, bọc an toàn trong `CustomerRepository.GetDeviceTokenAsync` và `BillsController`.
    * Đảm bảo khi Từ chối, toàn bộ các cột `...A` được giữ nguyên là `NULL`.
  - **Dùng chung Header Filter Bar**: Di chuyển toàn bộ thanh bộ lọc ra ngoài dùng chung hoàn hảo cho cả Tab 1 và Tab 2.
  - **Nghiệm thu**: User đã trực tiếp manual test các trường hợp phê duyệt và từ chối, chính thức nghiệm thu đóng task `UI-TAB2` lúc 15:06 ngày 27/10/2026.
- [x] **[PLAN-INIT] Hoàn thiện Kế hoạch chi tiết Phase 4 (`CRM-PROC`) & Phase 5 (`AUTO-OCR`)**:
  - Cập nhật tài liệu kỹ thuật [05_LOYALTY_CRMPROCESS_ENGINE.md](docs/05_LOYALTY_CRMPROCESS_ENGINE.md) và [DEV_PLAN.md](DEV_PLAN.md).
  - Bổ sung gửi Notification độc lập ở cuối mỗi bước: Tích điểm (`GetPointBySale`), Thăng hạng thẻ nếu có UpClass (`MemberUpClass`), Quà tặng khuyến mãi nếu có quà (`SendPromotion`), Vé số may mắn nếu có vé (`SendLottery`).
  - Bổ sung cấu hình **Notification Debug / Mock Mode** trong `appsettings.json` (`EnableRealPush: false`, `MockMode: true`, `LogToConsole: true`) để kiểm chứng 100% logic tạo message và lưu vết DB `mssNotification`, nhưng tuyệt đối không bắn push ra thiết bị khách hàng thật trong giai đoạn debug.
  - Kế thừa và tương thích chuẩn xác với hệ thống message cũ (`G_NotificationMessage`, `mssNotification`).

### 2. Kế hoạch công việc cho ngày mai (28/10/2026):
1. **Triển khai Phase 4: Task `CRM-PROC` (Nút [CRMProcess] & Động Cơ Loyalty Engine)**:
   - Cấu hình `NotificationSettings` (MockMode) trong `appsettings.json`.
   - Bổ sung hàng đợi hóa đơn "Đã duyệt - Chờ CRMProcess" (`Approved = 1 AND (CRMProcess = 0 OR CRMProcess IS NULL)`).
   - Xây dựng Nút `[⚡ CRMProcess]` (đơn lẻ / theo lô) kèm Popup Real-time Console Log tiến trình.
   - Triển khai Chặng 1: Tích điểm `mssCardTrans`, đồng bộ 3 sổ cái, gửi Notification tích điểm `GetPointBySale`; xét UpClass thăng hạng thẻ $\rightarrow$ gửi Notification `MemberUpClass`.
   - Triển khai Chặng 2: Quét chiến dịch khuyến mãi, cấp mã voucher $\rightarrow$ cuối bước gửi Notification `SendPromotion`.
   - Triển khai Chặng 3: Quy đổi giá trị hóa đơn ra vé số, sinh mã vé may mắn $\rightarrow$ cuối bước gửi Notification `SendLottery`.
   - Triển khai Chặng 4: Cập nhật `CRMProcess = 1`, `CRMProcessDate = GETDATE()`, `TransDocID`, `PointGain`, `DataStatus = 'Completed'`, lưu log `mssNotification`.
2. **Triển khai Phase 5: Task `AUTO-OCR` (Background Worker 5 phút)**:
   - Đóng gói Background Service tự động chạy ngầm chu kỳ 5 phút/lần.
   - Quét hóa đơn `OCRStatus = 0/null` $\rightarrow$ gọi Server OCR AI $\rightarrow$ lưu kết quả DB và set `OCRStatus = 1`.

---
*Báo cáo được chốt vào lúc 15:25 ngày 27/10/2026 bởi Antigravity Pair Programmer.*
