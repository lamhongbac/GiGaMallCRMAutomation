# NHẬT KÝ THỰC THI CHI TIẾT (DEV HISTORY)
## DỰ ÁN CRMMALL - GIGAMALL MIGRATION

> 📝 **Mục đích**: Ghi vết chi tiết từng hạng mục công việc đã triển khai thành công, bao gồm thời gian, file thay đổi, kết quả test và bằng chứng nghiệm thu thực tế.

---

## 📌 [TASK: DOCS-INIT] - Khởi tạo & Chuẩn hóa Hệ thống Tài liệu Kỹ thuật Toàn diện
* **Thời gian hoàn tất**: 26/10/2026 11:35 (GMT+7)
* **Người thực hiện**: Antigravity Pair Programmer
* **Người nghiệm thu**: User (Lệnh OK)
* **Tập tin đã khởi tạo**:
  - `TECHNICAL_DOCUMENT.md`: Sơ đồ danh mục tổng thể (Master Catalog).
  - `DEV_PLAN.md`: Kế hoạch master đồng bộ tiến độ.
  - `DEV_HISTORY.md`: Nhật ký kỹ thuật.
  - `DAILY_UPDATE.md`: Báo cáo EOD cuối ngày.
  - Thư mục `docs/` với 6 tài liệu chi tiết (đầy đủ 2 phần As-Is và To-Be):
    * `docs/01_DATABASE_SCHEMA_MIGRATION.md`
    * `docs/02_BILL_INGESTION_AND_STORAGE.md`
    * `docs/03_OCR_AI_INTEGRATION.md`
    * `docs/04_ADMIN_REVIEW_CONSOLE.md`
    * `docs/05_LOYALTY_CRMPROCESS_ENGINE.md`
    * `docs/06_BACKGROUND_AUTOMATION.md`
* **Nội dung thực hiện chi tiết**:
  - Đã rà soát toàn bộ tài liệu cũ từ `D:\GIT PROJECT\CRM SAOMAI\CRMPROFILELogic\SourceCode\CRMProfileManagement`.
  - Phân tách kiến trúc tài liệu thành mô hình Catalog tổng thể + Chuyên đề chi tiết nhằm tránh quá tải thông tin.
  - Định rõ thư mục tham khảo dự án cũ và các file trọng yếu (`frmOCRBillReview.cs`, `GigaBillProcess.cs`, `mssOCRTran.cs`).
* **Trạng thái**: `DONE` (Cập nhật DEV_PLAN.md: DOCS-INIT -> DONE).

---

## 📌 [TASK: B2-P1.1 -> B2-P1.4] - Xây dựng Khung Thử nghiệm Gọi OCR (Giai đoạn 2.1)
* **Thời gian hoàn tất**: Đã hoàn thành và kiểm thử trước đó
* **Người thực hiện**: Đội ngũ phát triển CRMMall
* **Người nghiệm thu**: User
* **Tập tin đã tạo**:
  - `src/CRMMall.BillReview.BLL/Services/OcrApiClient.cs`
  - `src/CRMMall.BillReview.DAL/Repositories/BillRepository.cs`
  - `src/CRMMall.BillReview.GUI/Controllers/BillsController.cs`
  - `src/CRMMall.BillReview.GUI/wwwroot/index.html`
* **Bằng chứng kiểm nghiệm (Evidence)**:
  - Unit test và kiểm thử E2E nạp 5 bill mẫu: Bấm nút [OCR] gọi server `http://14.161.34.37:8080/api/v1/extract-bill` nhận diện chính xác tổng tiền, số bill, gian hàng; bấm [Save OCR] lưu thành công vào CSDL.
* **Trạng thái**: `DONE` (Đã xác thực hoàn tất Giai đoạn 2.1).

---

## 📌 [TASK: UI-HEADER] - Xây Dựng Thanh Công Cụ Lọc Header Chung (Filter Toolbar)
* **Thời gian hoàn tất**: 26/10/2026 13:30 (GMT+7)
* **Người thực hiện**: Antigravity Pair Programmer
* **Người nghiệm thu**: User (Lệnh OK)
* **Tập tin đã cập nhật/thêm mới**:
  - `CRMMall.BillReview.DAL/Repositories/IBillRepository.cs` & `BillRepository.cs`:
    * Thêm `GetDistinctBrandsAsync()` và `GetDistinctOutletsAsync()` để query danh mục Brand/Outlet từ DB.
    * Thêm `GetBillsPagedAsync(...)` hỗ trợ lọc động theo khoảng ngày (`fromDate`, `toDate`), `brandNumber`, `outletNumber`, `keyword` (MemID/BillNumber), và cờ trạng thái (`ocrStatus`, `reviewed`, `approved`), kết hợp phân trang chuẩn SQL Server (`OFFSET ... ROWS FETCH NEXT ... ROWS ONLY`).
  - `CRMMall.BillReview.BLL/DTOs/OcrDtos.cs`:
    * Thêm `FilterOptionsDto` và `OutletOptionDto`.
  - `CRMMall.BillReview.BLL/Services/IBillReviewService.cs` & `BillReviewService.cs`:
    * Triển khai `GetFilterOptionsAsync()` và `GetBillsPagedAsync(...)`.
  - `CRMMall.BillReview.GUI/Controllers/BillsController.cs`:
    * Endpoint `GET /api/bills/filter-options` trả về danh mục Brand/Outlet.
    * Endpoint `GET /api/bills/pending-upload` & `GET /api/bills/paged` tiếp nhận đầy đủ query params phân trang và bộ lọc.
  - `CRMMall.BillReview.GUI/wwwroot/index.html`:
    * Gắn thanh Header Toolbar gồm: Bộ lọc ngày (Từ ngày - Đến ngày), Dropdown Brand (#selBrandFilter), Dropdown Outlet cascading theo Brand (#selOutletFilter), Dropdown Trạng thái (#selStatusFilter), Ô tìm kiếm tự do (#txtSearchPending), Nút [Lọc], Nút [Xóa lọc].
    * Gắn thanh phân trang (Trang trước, Trang sau, Trang hiện tại / Tổng trang, Tổng số hóa đơn).
* **Kết quả kiểm thử**:
  - API `GET /api/bills/filter-options` trả về 200 OK với danh sách Outlet từ DB `CLMHQTEST`.
  - API `GET /api/bills/pending-upload?page=1&pageSize=10` trả về 200 OK với danh sách 10 hóa đơn kèm `totalCount = 3848`.
  - Kiểm thử giao diện trên trình duyệt xác nhận Dropdown lọc và thanh phân trang hoạt động đồng bộ.
* **Trạng thái**: `DONE` (Cập nhật DEV_PLAN.md: UI-HEADER -> DONE)

---

## 📌 [TASK: UI-TAB1] - Nâng Cấp Tab 1 Thực Hiện & Thử Nghiệm OCR (Interactive AI Console)
* **Thời gian hoàn tất**: 26/10/2026 13:50 (GMT+7)
* **Người thực hiện**: Antigravity Pair Programmer
* **Người nghiệm thu**: User (Lệnh OK)
* **Tập tin đã cập nhật**:
  - `CRMMall.BillReview.GUI/wwwroot/index.html`:
    * Bảng dữ liệu Tab 1 hiển thị danh sách các hóa đơn vừa upload đang chờ OCR (`OCRStatus = 0 / NULL`).
    * Lightbox Modal chi tiết khi click vào hàng bill hoặc nút [Soi & OCR]:
      - Cột trái: Bộ Image Viewer toàn diện hỗ trợ Phóng to (Zoom In +), Thu nhỏ (Zoom Out -), Xoay ảnh 90 độ (Rotate), Kéo rê tự do (Pan/Drag mousedown) và Điều hướng Hóa đơn Trước/Sau (Prev/Next, phím tắt mũi tên).
      - Cột phải - Phân định rõ rệt 2 Vùng nghiệp vụ:
        * **VÙNG 1: AI (OCR BÓC TÁCH)**: Hiển thị Brand/Outlet trích xuất, Số bill, Ngày giờ, Tổng tiền định dạng VND, Độ tin cậy (Confidence & Match Score), Độ trễ AI và nút xem chi tiết Raw JSON.
        * **VÙNG 2: ADMIN ADJUST (...A)**: Hiển thị 4 trường điều chỉnh ghi đè (`modalOutletNumberA`, `modalBillNumberA`, `modalBillTotalA`, `modalBillDateTimeA`) ở trạng thái khóa `disabled` kèm nhãn `🔒 Mở khóa tại Tab 2` (Đảm bảo tuân thủ nguyên tắc không can thiệp số liệu thẩm định tại Tab 1).
      - Cụm nút thao tác trực tiếp:
        * Nút `[🔍 Gọi OCR]`: Gọi API `POST /api/bills/{id}/ocr` kết nối đến máy chủ AI `14.161.34.37:8080/api/v1/extract-bill` (theo chuẩn `04_CRM_API_INTEGRATION_SPEC.md`).
        * Nút `[💾 Lưu OCR]`: Gọi API `POST /api/bills/{id}/save-ocr` lưu kết quả trích xuất vào CSDL `mssOCRTrialTrans` và bật cờ `OCRStatus = 1`.
        * Nút `[⚡ Gọi & Lưu OCR (1-Click)]`: Thực hiện chuỗi vừa bóc tách vừa tự động lưu.
      - Sau khi lưu thành công, danh sách Tab 1 tự động refresh để hóa đơn đã xong OCR được loại bỏ khỏi hàng đợi chờ OCR và chuyển tiếp sang hàng đợi thẩm định Tab 2.
* **Kết quả kiểm thử**:
  - Trình duyệt subagent truy cập `http://localhost:5254`, mở modal chi tiết của hóa đơn `#11604`.
  - Image Viewer hiển thị ảnh phóng to/thu nhỏ/xoay mượt mà.
  - Phân tách 2 Vùng AI OCR và Admin Adjust hiển thị trực quan và chuẩn UX.
  - Nút `[🔍 Gọi OCR]` kích hoạt đúng quy trình gọi máy chủ bóc tách.
* **Trạng thái**: `DONE` (Cập nhật DEV_PLAN.md: UI-TAB1 -> DONE)

---

## 📌 [TASK: IMG-SAVE] - Thêm Nút [💾 Lưu Ảnh] Ghi Đè Xuống Đĩa Cứng & Cấu Hình Debug Ngày Mặc Định
* **Thời gian hoàn tất**: 26/10/2026 15:35 (GMT+7)
* **Người thực hiện**: Antigravity Pair Programmer
* **Người yêu cầu**: User
* **Mục tiêu**:
  1. Hóa đơn bị xoay ngang khiến OCR AI bóc tách sai lệch $
ightarrow$ Cho phép xoay trên giao diện và bấm nút `[💾 Lưu Ảnh]` để ghi đè (overwrite) trực tiếp file vật lý trên đĩa cứng trước khi gọi OCR.
  2. Bổ sung `DebugSettings` trong `appsettings.json` với `DefaultFromDate` & `DefaultToDate` (mặc định: `2026-09-02`) để khi mở trang web tự động tải đúng khoảng ngày có sẵn ảnh trong `D:\GGDrive\TEST_IMAGES\Transaction\020926`.
* **Tập tin đã cập nhật**:
  - `CRMMall.BillReview.BLL/Configurations/ApplicationSetting.cs`: Thêm class `DebugConfig` (`DefaultFromDate`, `DefaultToDate`).
  - `CRMMall.BillReview.GUI/appsettings.json`: Thêm mục `"DebugSettings": { "DefaultFromDate": "2026-09-02", "DefaultToDate": "2026-09-02" }`.
  - `CRMMall.BillReview.BLL/Services/IBillReviewService.cs` & `BillReviewService.cs`:
    * Thêm `Task<bool> OverwriteBillImageAsync(Guid billId, byte[] imageBytes)` ghi đè trực tiếp xuống đĩa cứng theo `fullImagePath = basePath + DbPath + ImageFile`.
    * Củng cố `TrimStart('\', '/')` cho `dbimagePath` để chống lỗi `Path.Combine` bỏ qua `baseImagePath`.
  - `CRMMall.BillReview.GUI/Controllers/BillsController.cs`:
    * Endpoint `GET /api/bills/config/environment` trả về `defaultFromDate` và `defaultToDate`.
    * Endpoint `POST /api/bills/{id}/save-image` tiếp nhận file ảnh từ form-data hoặc base64 và gọi BLL ghi đè file vật lý.
  - `CRMMall.BillReview.GUI/wwwroot/index.html`:
    * Thêm nút `[💾 Lưu Ảnh]` (`#btnSaveRotatedImage`) trên thanh công cụ xem ảnh.
    * Hàm `saveImageFromUI()` vẽ ảnh từ màn hình (bao gồm góc xoay) lên HTML5 Canvas, chuyển thành Blob PNG và POST lên server.
    * Hàm `loadEnvironmentConfig()` tự động điền `FromDate` và `ToDate` từ cấu hình debug để nạp ngay bill ngày 02/09/2026.
* **Kết quả kiểm thử**:
  - Gọi test API `POST /api/bills/738bf710-2e03-435e-b189-2746f4715528/save-image`: Trả về `{ success: true, message: "Đã ghi đè file ảnh xuống đĩa cứng thành công!" }`.
  - Endpoint `GET /api/bills/config/environment`: Trả về đúng `defaultFromDate: "2026-09-02"`.
* **Trạng thái**: `DONE` (User đã trực tiếp manual test trên trình duyệt và ra lệnh xác nhận hoàn thành lúc 17:18 26/10/2026)

---

## 📌 [TASK: UI-OPTIMIZE] - Tối Ưu Giao Diện Modal Bóc Tách OCR (Tab 1): Loại Bỏ Vùng 2 (Admin Adjust)
* **Thời gian hoàn tất**: 27/10/2026 09:50 (GMT+7)
* **Người thực hiện**: Antigravity Pair Programmer
* **Người yêu cầu**: User ("day la man hinh boc tach OCR, kg can khu vuc vung 2 phia duoi , hay remove khu vuc do de man hinh tro len thong thoang")
* **Nội dung thay đổi**:
  - Giao diện Modal chi tiết tại Tab 1 là khu vực tập trung cho nghiệp vụ bóc tách OCR. Vùng 2 (Admin Adjust `...A`) vốn dĩ đã bị khóa tại Tab 1 và thuộc quyền xử lý của **Tab 2 (Bàn Thẩm Định & Đối Soát)**.
  - Loại bỏ hoàn toàn khối HTML `#areaAdminAdjust` (Vùng 2) cùng các input `modalOutletNumberA`, `modalBillNumberA`, `modalBillTotalA`, `modalBillDateTimeA` khỏi modal.
  - Cập nhật tiêu đề phân vùng còn lại thành `🤖 KẾT QUẢ AI (OCR BÓC TÁCH)` giúp layout cột phải thoáng đãng, tập trung tối đa không gian hiển thị cho dữ liệu OCR và các nút bóc tách.
  - Làm sạch code Javascript trong `updateModalOcrView()` tương ứng.
---

## 📌 [TASK: UI-TAB2] - Hoàn Thiện Toàn Diện Bàn Thẩm Định & Đối Soát Hóa Đơn (Tab 2)
* **Thời gian hoàn tất code**: 27/10/2026 11:30 (GMT+7)
* **Người thực hiện**: Antigravity Pair Programmer
* **Người yêu cầu**: User
* **Mục tiêu & Nguyên tắc nghiệp vụ đã thống nhất**:
  1. **Không có nút "Sao chép từ OCR"**: Vùng 2 (Admin Adjust `...A`) để trống mặc định. Admin chỉ nhập vào khi OCR bóc tách sai; nếu OCR đúng thì Admin không cần làm gì.
  2. **Hàm `computeFinalBillData(bill)`**: Tính toán số liệu chốt duyệt theo quy tắc ưu tiên: `FinalValue = AdminValueA ? AdminValueA : OcrValue`.
  3. **Hàm `validateFinalBillData(finalData, options)`**: Hàm độc lập trả về `boolean`, kiểm tra tính hợp lệ của số Final: `FinalTotal > 0`, `FinalOutlet` không rỗng, `FinalBillNo` không rỗng, `FinalDateTime <= Hiện tại` (dung sai 5 phút), hỗ trợ mở rộng options kiểm tra.
  4. **Nút `[✅ Phê Duyệt]` (`Approve`)**: Luôn gọi hàm validation trên, chỉ khi `valid === true` mới thực hiện gọi API `POST /api/bills/approve`.
  5. **Nút `[❌ Từ Chối]` (`Reject`)**: Mở Modal Từ chối (`#rejectModal`), bắt buộc nhập lý do (`ReviewNotes`), gọi `POST /api/bills/reject`.
  6. **Image Viewer cho Tab 2**: Tích hợp Zoom (+/- / cuộn chuột), Pan (drag kéo rê), Xoay (90°) trực tiếp trên ảnh hóa đơn tại Bàn đối soát Tab 2 để soi rõ từng chi tiết.
  7. **Khối Final Summary Box**: Cập nhật real-time khi Admin gõ vào ô `...A`, hiển thị nhãn nhận biết `[✏️ Admin]` vs `[🤖 OCR]`.
* **Tập tin đã cập nhật**:
  - `src/CRMMall.BillReview.GUI/wwwroot/index.html`:
    * CSS: Bổ sung layout `.workspace`, `.review-img-toolbar`, `.review-img-viewport`, `.review-img-wrapper`, `.audit-card`, `.audit-row`, `.audit-col-ocr`, `.audit-col-adjust`, `.final-summary-card`, `.badge-source-ocr`, `.badge-source-admin`. Thêm `.common-filter-bar` dùng chung cố định dưới `<header>`.
    * HTML `#viewReview`: Bổ sung thanh công cụ xem ảnh, bảng đối soát 2 cột OCR vs Admin, khối Số liệu chốt duyệt và cụm nút Phê duyệt / Từ chối.
    * HTML Common: Di chuyển toàn bộ cụm Filter Bar (`.common-filter-bar`) ra ngoài màn hình chính dùng chung cho cả Tab 1 và Tab 2.
    * HTML Modal: Bổ sung `#rejectModal` với dropdown lý do mẫu và ô nhập lý do bắt buộc.
    * Javascript: Viết `computeFinalBillData()`, `validateFinalBillData()`, `updateFinalSummaryUI()`, `onAdminInputChange()`, `applyReviewImgTransform()`, `zoomReviewImg()`, `rotateReviewImg()`, `resetReviewImg()`, `initReviewViewerEvents()`, `loadReviewBills()`, `renderReviewQueue()`, `filterReviewQueue()`, `selectReviewBill()`, `approveBill()`, `openRejectModal()`, `closeRejectModal()`, `onSelectRejectTemplate()`, `confirmReject()`.
    * JS Sync: Viết `applyCurrentFilter()`, `onStatusFilterChange()`, đồng bộ `switchTab()` giữa Tab 1 (`OCRStatus = 0/NULL`) và Tab 2 (`OCRStatus = 1`).
* **Cập nhật bổ sung (12:15 27/10/2026)**: Theo yêu cầu của User, đã tạm REM logic kiểm tra `MemID` và hồ sơ hội viên tại `BillReviewService.ApproveBillAsync` để phục vụ manual test khâu duyệt số Final khi chưa có dữ liệu hội viên; đồng thời bọc an toàn các bước Loyalty tránh văng lỗi khi `customer == null`. Sẽ mở lại kiểm tra này sau khi chuẩn bị xong dữ liệu hội viên.
* **Cập nhật sửa lỗi Audit Fields & Contrast Giao diện (13:45 27/10/2026)**:
  - **Sửa triệt để lỗi ghi đè dữ liệu OCR vào các cột `_A`**:
    * Tuân thủ nguyên tắc cốt lõi: Hệ thống **CHỈ GHI NHẬN ĐÚNG NHỮNG GÌ ADMIN TYPE VÀO** các ô điều chỉnh. Tuyệt đối **KHÔNG** lấy số OCR đè vào các trường `...A`. Nếu ô nào Admin không nhập (để trống), trường `...A` tương ứng trong CSDL (`BillNumberA`, `OutletNumberA`, `BillDateTimeA`, `BrandNumberA`) bắt buộc là `NULL`.
    * Cập nhật `approveBill()` trong `index.html`: Chỉ đóng gói các trường `outletNumberA`, `billNumberA`, `billTotalA`, `billDateTimeA` khi Admin thực sự gõ giá trị, ngược lại truyền `null`.
    * Cập nhật `ApproveBillAsync` trong `BillReviewService.cs`: Chỉ lưu giá trị Admin gõ vào các trường `...A` qua `UpdateApprovalStatusAsync`, không gán `approvedBillNumber`/`approvedOutletNumber` (OCR) vào `...A`.
    * Cập nhật `IsBillNumberExistsAsync` trong `BillRepository.cs`: Sử dụng `COALESCE(OutletNumberA, OutletNumber)` và `COALESCE(BillNumberA, BillNumber)` để chống trùng lặp chính xác cho cả bill dùng số OCR lẫn bill có Admin điều chỉnh.
  - **Sửa lỗi hiển thị chữ trên ô nhập liệu (CSS Contrast)**:
    * Khắc phục tình trạng background và foreground cùng màu tối khiến không nhìn thấy số vừa gõ.
    * Đặt background ô nhập là `#0b1329` (navy tối sâu), màu chữ `#ffffff !important` và `-webkit-text-fill-color: #ffffff !important` (trắng sáng 100%), con trỏ nhấp nháy màu cyan sáng `#38bdf8 !important`.
    * Ô số tiền điều chỉnh `#txtTotalA` có màu chữ xanh ngọc lục bảo `#34d399 !important` với font size 15px, font-weight 700 nổi bật.
    * Placeholder có màu xám sáng `#94a3b8 !important` dễ phân biệt.
  - **Dọn dẹp bản ghi test bill 260904**:
    * Đã reset bản ghi bill 260904 trên CSDL `CLMHQTEST` về trạng thái ban đầu (`Approved = NULL, Reviewed = 0, DataStatus = 'Pending', các trường ...A = NULL`) để User thực hiện test duyệt lại ngay.
* **Cập nhật sửa lỗi Modal Từ Chối & Notification Exception (15:05 27/10/2026)**:
  - **Nguyên nhân lỗi dialog không đóng**:
    * Khi bấm "Xác nhận từ chối", tiến trình gọi `_notificationService.SendBillRejectedNotificationAsync` truy vấn bảng `dbo.mssMobileTracking` để lấy DeviceToken. Tuy nhiên trên môi trường test (`CLMHQTEST`), bảng này chưa được thiết lập, dẫn đến `SqlException: Invalid object name 'dbo.mssMobileTracking'`.
    * Lỗi ngoại lệ chưa được bắt (unhandled 500) làm API trả về mã lỗi, khiến client JavaScript không kích hoạt hàm `closeRejectModal()`, dialog giữ nguyên trên màn hình.
    * Ngoài ra, `RejectBillAsync` trước đây truyền `bill.BillNumber` và `0` vào hàm cập nhật, khiến các cột `_A` (`BillNumberA`, `BillTotalA`) bị ghi đè không đúng bản chất từ chối.
  - **Giải pháp xử lý**:
    * Tại `BillReviewService.RejectBillAsync`: Tạm REM cuộc gọi gửi Push Notification thiết bị (tuân thủ nguyên tắc chỉ lưu thông tin admin và trạng thái ở bước này); đồng thời đặt toàn bộ các tham số `...A` là `null` (`approvedAmount: null, approvedBillNumber: null, ...`) để không làm sai lệch dữ liệu `_A`.
    * Tại `CustomerRepository.GetDeviceTokenAsync`: Bọc `try-catch` an toàn, trả về `null` nếu bảng tracking không tồn tại hoặc lỗi kết nối.
    * Tại `BillsController`: Thêm `try-catch` cho cả 2 action `ApproveBill` và `RejectBill` để bắt mọi ngoại lệ và trả về phản hồi JSON chuẩn xác (`StatusCode(500, { success = false, message = ... })`).
    * Tại `index.html`: Cải thiện hàm `confirmReject()`, parse JSON an toàn và gọi `closeRejectModal()` khi thành công.
    * Dọn dẹp dữ liệu test: Reset bản ghi bill của hội viên 16618 (`12f28a56-ec72-4969-ba17-91d630d001c2`) về `Pending` để User thực hiện test từ chối lại.
* **Trạng thái**: <span style="color:green;font-weight:bold;">[DONE]</span> (User đã manual test thành công và chính thức nghiệm thu đóng task UI-TAB2 lúc 15:06 27/10/2026).
* **Cập nhật Plan Phase 4 CRM-PROC (15:20 27/10/2026)**:
  - Bổ sung cấu hình `NotificationSettings: { EnableRealPush: false, MockMode: true, LogToConsole: true }` trong `appsettings.json`.
  - Đảm bảo trong giai đoạn debug/test, hệ thống vẫn thực thi 100% logic tạo message, mapping template, ghi log DB `mssNotification` (Status = 'MockSent') và hiển thị chi tiết ra Web Console để kiểm chứng, nhưng **tuyệt đối không gửi push ra thiết bị khách hàng thật**.
  - Kế thừa chuẩn hệ thống message đã hình thành trước đó: Bảng mẫu `G_NotificationMessage`, nhật ký `mssNotification`, các Event chuẩn (`GetPointBySale`, `MemberUpClass`, `SendPromotion`, `SendLottery`) và các trường thay thế (`{Point}`, `{ClassName}`, `{PromotionCode}`, `{TicketCodes}`).
  - Bổ sung gửi notification độc lập ở cuối mỗi bước nghiệp vụ: Tích điểm, Nâng hạng (nếu có UpClass), Quà tặng khuyến mãi (nếu có quà), Vé số may mắn (nếu có vé).





