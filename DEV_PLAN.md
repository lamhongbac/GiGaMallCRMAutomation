# KẾ HOẠCH TRIỂN KHAI PHÁT TRIỂN CHÍNH THỨC (DEV PLAN)
## DỰ ÁN CRMMALL - PHÂN HỆ ĐỐI SOÁT & XỬ LÝ HÓA ĐƠN OCR (GIGAMALL MIGRATION)

> 🏢 **Hệ sinh thái**: CRM Loyalty Gigamall - Sao Mai Solution  
> 🎯 **Mục tiêu**: Lộ trình kiểm nghiệm và triển khai tính năng bóc tách, đối soát 2 tab, thẩm định và xử lý loyalty cho hóa đơn OCR.  
> 🛡️ **Quy tắc điều hành**: Mọi công việc đều căn cứ vào plan này. Hoàn thành task nào cập nhật ngay trạng thái `DONE` và ghi chép vào `DEV_HISTORY.md`.  
> 🏛️ **Dự án cũ tham khảo**: `D:\GIT PROJECT\CRM SAOMAI\CRMPROFILELogic\SourceCode\CRMProfileManagement`

---

## 📊 MA TRẬN TIẾN ĐỘ THỰC HIỆN TỔNG THỂ (OVERALL PROGRESS MATRIX)

| Mã Task | Hạng mục công việc chi tiết | Phạm vi kỹ thuật | Trạng thái | Nghiệm thu / Ghi chú |
| :--- | :--- | :--- | :---: | :--- |
| **B1-ASIS** | Bảo toàn 100% flow tiếp nhận upload từ Mobile App | `CRMAPI` / `CoreAPI` | <span style="color:green;font-weight:bold;">[DONE]</span> | Hoàn tất & bảo toàn Production |
| **B2-P1.1** | Xây dựng `OcrApiClient` & DTO kết nối OCR Server Qwen2-VL | `CRMMall.BillReview.BLL` | <span style="color:green;font-weight:bold;">[DONE]</span> | Kết nối `http://14.161.34.37:8080` OK |
| **B2-P1.2** | Viết API đọc danh sách pending upload & API save OCR | `CRMMall.BillReview.DAL` & Controller | <span style="color:green;font-weight:bold;">[DONE]</span> | Đọc `Pending`, Save `OCR` |
| **B2-P1.3** | Dựng Giao diện Web Interactive Testing UI cơ bản | `CRMMall.BillReview.GUI\wwwroot` | <span style="color:green;font-weight:bold;">[DONE]</span> | UI xem ảnh, nút OCR, nút Save |
| **B2-P1.4** | Thử nghiệm thực tế toàn trình 5 bill mẫu (E2E Test) | Trực quan trên Browser | <span style="color:green;font-weight:bold;">[DONE]</span> | Bóc tách & lưu DB 100% OK |
| **DOCS-INIT**| Chuẩn hóa bộ tài liệu kỹ thuật (Catalog + 6 Docs chi tiết) | `TECHNICAL_DOCUMENT.md` & `docs/` | <span style="color:green;font-weight:bold;">[DONE]</span> | Đã hoàn tất 26/10/2026 |
| --- | ---------------------------------------------------------------- | ------------------------------------ | ---------- | --------------------------------------- |
| **UI-HEADER**| Xây dựng Header dùng chung cho 2 Tab: Lọc (Date, Brand, Outlet, Status) + Tìm kiếm (MemID, Outlet) + Phân trang | `CRMMall.BillReview.GUI` | <span style="color:green;font-weight:bold;">[DONE]</span> | User đã manual check & xác nhận OK |
| **UI-TAB1** | Nâng cấp **Tab 1: Thực hiện OCR**<br/>- Grid hiển thị bill `OCRStatus = 0/null`<br/>- Modal Detail: Image Viewer (Zoom/Pan/Rotate) + 2 vùng [AI OCR] & [Admin Adjust]<br/>- Nút [OCR] bóc tách $\rightarrow$ Nút [Save OCR] cập nhật `OCRStatus = 1` | `CRMMall.BillReview.GUI` & API | <span style="color:green;font-weight:bold;">[DONE]</span> | User đã manual check & xác nhận OK |
| **IMG-SAVE** | Bổ sung nút **[💾 Lưu Ảnh]** ghi đè trực tiếp ảnh từ màn hình giao diện (sau khi xoay) xuống đĩa cứng & cấu hình `DebugSettings` (`DefaultFromDate`, `DefaultToDate`) | `CRMMall.BillReview.GUI` & BLL | <span style="color:green;font-weight:bold;">[DONE]</span> | User đã kiểm chứng & xác nhận hoàn thành |
| **IMG-ZOOM-CROP** | Khi phóng to (zoom/pan) ảnh trên giao diện và bấm [💾 Lưu Ảnh], lưu lại đúng vùng ảnh đã zoom/crop xuống đĩa cứng để tối ưu vùng OCR | `CRMMall.BillReview.GUI` | <span style="color:green;font-weight:bold;">[DONE]</span> | User đã kiểm chứng & xác nhận OK |
| **UI-TAB2** | Xây dựng **Tab 2: Duyệt Bill đã OCR**<br/>- Hàng đợi bill `OCRStatus = 1` & `Reviewed = 0`<br/>- Bàn đối soát tích hợp Image Viewer (Zoom/Pan/Rotate)<br/>- Đối chiếu OCR vs Admin Adjust `...A` (chỉ nhập khi khác)<br/>- Hàm `computeFinalData()` & Hàm `validateFinalBillData()` độc lập<br/>- Nút [Phê Duyệt] (`Approved = 1`) & Nút [Từ chối] (`Approved = 0`)<br/>- Dùng chung Filter Bar toàn diện với Tab 1 | `CRMMall.BillReview.GUI` & BLL | <span style="color:green;font-weight:bold;">[DONE]</span> | User đã manual test và xác nhận hoàn thành 100% |
| **CRM-PROC**| Xây dựng chức năng **Nút [CRMProcess]** cho các bill Đã duyệt (`Approved = 1`):<br/>1. Tính điểm + Thăng hạng thẻ $\rightarrow$ Ghi log & **Nếu lên hạng: Gửi Notification `MemberUpClass`**<br/>2. Xét Gift Promotion $\rightarrow$ Ghi log & **Cuối bước: Gửi Notification quà tặng `SendPromotion`**<br/>3. Cấp vé số Lottery $\rightarrow$ Ghi log & **Cuối bước: Gửi Notification vé số `SendLottery`**<br/>4. Cập nhật `CRMProcess = 1`, `TransDocID`, `PointGain` | `CRMMall.BillReview.BLL` | `PENDING` | Porting logic từ `GigaBillProcess.cs` sang `LoyaltyProcessService` |
| **AUTO-OCR**| Đóng gói Background Worker / Cron Job 5 phút tự động quét bill ngầm qua CheckPoint Framework | `CRMMall.BillReview.Worker` | `PENDING` | Kích hoạt sau khi UI được nghiệm thu |

---

## 📅 LỘ TRÌNH THỰC HIỆN CHI TIẾT (ACTION PLAN)

### Phase 1: Header dùng chung & Điều hướng 2 Tab (`UI-HEADER`)
- **Mục tiêu**: Tạo khu vực điều khiển tập trung ở đầu trang Web cho cả Tab 1 và Tab 2.
- **Tiêu chí nghiệm thu**:
  - Filter: Từ ngày - Đến ngày (`fromDate` - `toDate`).
  - Dropdown: Brand, Outlet lấy từ danh mục.
  - Dropdown Status: Trạng thái OCR (`Chưa OCR` / `Đã OCR`), Trạng thái Review (`Chờ duyệt` / `Đã duyệt` / `Đã từ chối`).
  - Ô tìm kiếm: Tìm nhanh theo `MemID` hoặc Tên gian hàng.
  - Phân trang: Chọn số dòng trên trang (10, 20, 50), trang hiện tại / tổng số trang.

### Phase 2: Tab 1 - Thực hiện OCR (`UI-TAB1`)
- **Mục tiêu**: Bảng dữ liệu bill chờ bóc tách + Modal tương tác ảnh đầy đủ.
- **Tiêu chí nghiệm thu**:
  - Load danh sách bill `(OCRStatus = 0 OR OCRStatus IS NULL) AND (Reviewed = 0 OR Reviewed IS NULL)`.
  - Bấm vào dòng mở Modal xem ảnh hóa đơn: Phóng to (Zoom), Kéo rê (Pan), Xoay (Rotate 90/180/270).
  - Vùng AI OCR hiển thị kết quả; Vùng Admin Adjust hiển thị nhưng tạm khóa.
  - Nút **[OCR]**: Gửi ảnh tới OCR Server, hiển thị spinner và binding kết quả lên vùng AI OCR.
  - Nút **[Save OCR]**: Lưu kết quả vào `mssOCRTrans`, set `OCRStatus = 1`, bill chuyển sang Tab 2.

### Phase 3: Tab 2 - Duyệt Bill đã OCR (`UI-TAB2`)
- **Mục tiêu**: Màn hình làm việc chính của kiểm toán viên để thẩm định và ra quyết định.
- **Tiêu chí nghiệm thu**:
  - Load danh sách bill `OCRStatus = 1 AND (Reviewed = 0 OR Reviewed IS NULL)`.
  - Mở Modal: Vùng AI OCR chỉ đọc (Read-only); Vùng Admin Adjust cho phép nhập: `BillNumberA`, `BillDateTimeA`, `BillTotalA`, `BrandNumberA`, `OutletNumberA`.
  - Hộp nhập ghi chú thẩm định `ReviewNotes`.
  - Nút **[Đồng ý]**: Kiểm tra trùng bill, cập nhật các trường `...A`, `Reviewed = 1`, `Approved = 1`, đóng băng dữ liệu.
  - Nút **[Từ chối]**: Bắt buộc nhập `ReviewNotes`, cập nhật `Reviewed = 1`, `Approved = 0`, bắn thông báo lý do ra Mobile.

### Phase 4: Step 4 - Nút [CRMProcess] & Logic Loyalty (`CRM-PROC`)
- **Mục tiêu**: Xử lý tính toán điểm, thăng hạng thẻ, quà tặng khuyến mãi và vé số may mắn cho các bill đã duyệt đồng ý (`Reviewed = 1 AND Approved = 1 AND (CRMProcess = 0 OR CRMProcess IS NULL)`).
- **Cấu hình Debug / Kiểm chứng Notification (`appsettings.json`)**:
  - `NotificationSettings:EnableRealPush = false`: Tuyệt đối không gửi notification ra thiết bị khách hàng thật trong giai đoạn debug/test.
  - `NotificationSettings:MockMode = true`: Chế độ giả lập an toàn để kiểm chứng — thực hiện đầy đủ 100% flow tạo message, ánh xạ template, ghi log DB `mssNotification` (Status = 'MockSent'), và hiển thị chi tiết payload (Tiêu đề, nội dung, EventName, người nhận) lên Web Console/Modal để kiểm toán.
  - **Tương thích Hệ thống Message cũ**: Kế thừa chuẩn bảng mẫu `G_NotificationMessage`, bảng nhật ký `mssNotification`, các Event chuẩn (`GetPointBySale`, `MemberUpClass`, `SendPromotion`, `SendLottery`) và các trường tham số thay thế (`{Point}`, `{ClassName}`, `{PromotionCode}`, `{TicketCodes}`).
- **Tiêu chí nghiệm thu**:
  - Giao diện: Hàng đợi bill Chờ CRMProcess, Nút [CRMProcess] (chạy đơn lẻ hoặc chạy theo lô) kèm console log tiến trình real-time.
  - **Mục tiêu 1: Tính điểm & Thăng hạng thẻ (Points & Tier)**:
    * Tính điểm cơ bản + Điểm thăng hạng từ số Final (`BillTotalA ?? BillTotal`) $\rightarrow$ Đồng bộ 3 sổ cái (`CardTrans`, `CLMLedger`, `MemClassLedger`) $\rightarrow$ Ghi log.
    * Gửi notification tích điểm thành công (`GetPointBySale`).
    * **Kiểm tra UpClass: NẾU CÓ LÊN HẠNG $\rightarrow$ Gửi Notification chúc mừng thăng hạng thẻ (`MemberUpClass`)**.
  - **Mục tiêu 2: Xét Quà Tặng Khuyến Mãi (Gift Promotion)**:
    * Tra cứu chương trình khuyến mãi theo Gian hàng/Brand và ngưỡng doanh số $\rightarrow$ Cấp mã Coupon/Voucher $\rightarrow$ Ghi log.
    * **Cuối bước: NẾU ĐƯỢC NHẬN QUÀ/VOUCHER $\rightarrow$ Gửi Notification quà tặng (`SendPromotion`) kèm mã ưu đãi & hạn dùng**.
  - **Mục tiêu 3: Cấp Vé Số May Mắn (Lucky Draw / Lottery)**:
    * Quy đổi doanh số ra số lượng vé $\rightarrow$ Sinh danh sách mã vé số may mắn ngẫu nhiên duy nhất $\rightarrow$ Ghi log.
    * **Cuối bước: NẾU ĐƯỢC CẤP VÉ SỐ $\rightarrow$ Gửi Notification vé số (`SendLottery`) hiển thị chi tiết các mã vé số dự thưởng**.
  - **Hoàn tất chu trình**: Cập nhật `CRMProcess = 1`, `CRMProcessDate = GETDATE()`, `TransDocID`, `PointGain`, `DataStatus = 'Completed'`.

### Phase 5: Step 2.2 - Tự động hóa ngầm Auto OCR 5 phút (`AUTO-OCR`)
- **Mục tiêu**: Chuyển đổi từ gọi OCR thủ công bằng nút bấm sang Worker quét tự động.
- **Tiêu chí nghiệm thu**:
  - Đóng gói Worker / CheckPoint Task chạy chu kỳ 5 phút/lần.
  - Quét lô bill `OCRStatus = 0/null` $\rightarrow$ Gọi OCR Server $\rightarrow$ Lưu DB và set `OCRStatus = 1`.
