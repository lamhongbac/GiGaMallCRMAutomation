# GigaMall CRM Enterprise Solution (.NET 8) - `CRMMall.sln`

> **Hệ sinh thái CRM Loyalty & Tự động hóa Toàn diện GigaMall** - Chuyển đổi và hiện đại hóa trên nền tảng **.NET 8 LTS**, tích hợp kiến trúc 3 lớp Microservice (DAL - BLL - GUI) và nền tảng tự động hóa **CheckPoint Orchestration Platform**.

---

## 1. Cấu trúc Tổng thể Giải pháp Hợp nhất (Unified Solution)

Hệ thống đã được quy tụ tập trung vào duy nhất một solution: `D:\GIT PROJECT\GIGAMallMigration\CRMMall.sln` (thay thế và gộp dứt điểm `CRMAutomationSln.sln`), bao gồm **7 projects** phân định rành mạch theo Clean Architecture:

```
D:\GIT PROJECT\GIGAMallMigration\
├── CRMMall.sln                              # Solution tổng thể duy nhất quản lý toàn bộ hệ sinh thái
├── README.md                                # Tài liệu kiến trúc & hiện trạng hệ thống
├── DEV_PLAN_PHASE_1.md                      # Kế hoạch phát triển chi tiết Giai đoạn 1
│
├── src\
│   ├── 📂 PHÂN HỆ DUYỆT HÓA ĐƠN & LOYALTY (BILL REVIEW SUBSYSTEM):
│   │   ├── CRMMall.BillReview.DAL\          # Data Access Layer: Dapper, ConnectionFactory, Repositories
│   │   ├── CRMMall.BillReview.BLL\          # Business Logic Layer: Tích điểm, Nâng hạng (UpClass), Vé số, Push Noti
│   │   └── CRMMall.BillReview.GUI\          # Presentation Layer: Web API & Web Portal dành cho kiểm toán viên
│   │
│   └── 📂 PHÂN HỆ TỰ ĐỘNG HÓA & LẬP LỊCH (CHECKPOINT AUTOMATION SUBSYSTEM):
│       ├── CRM.Automation.Core\             # Core Entities, Enums, DTOs, Task Contracts
│       ├── CRM.Automation.Infrastructure\   # Triển khai tác vụ CheckPoint, tham chiếu dùng chung BLL/DAL
│       └── CRM.Automation.Api\              # Web API tiếp nhận Trigger từ CheckPoint Platform (ApiKey Filter)
│
├── tests\
│   └── CRM.Automation.Tests\                # xUnit Test Suite kiểm thử bộ quy tắc tự duyệt & nâng hạng
│
└── database\
    └── CLMHQTEST_POC_Setup.sql              # Script CSDL mẫu & Stored Procedures phục vụ thử nghiệm
```

---

## 2. Triết lý Thiết kế: Chia sẻ BLL & Single Source of Truth

Toàn bộ logic nghiệp vụ cốt lõi của Trung tâm Thương mại được đóng gói độc lập trong **`CRMMall.BillReview.BLL`** dưới dạng các **Domain Services tái sử dụng cao**:
1. **`ILoyaltyPointsService` / `LoyaltyPointsService`**:
   - Tính toán điểm thưởng và điểm xếp hạng theo tỷ lệ quy định tại `mssCLMCardTransRule`.
   - Cập nhật đồng bộ các sổ cái: `mssCLMLedger`, `mssMemClassLedger`, `mssCardTrans`.
2. **`IMembershipTierService` / `MembershipTierService`**:
   - Thẩm định tổng điểm tích lũy theo năm hoặc kỳ đánh giá.
   - Tự động đóng thẻ cũ (Status 'C'), mở thẻ mới (Status 'O') và tặng điểm thăng hạng vào `mssCLMGiftPoint`.
   - **Dùng chung 100%**: Phục vụ cả khi kiểm toán viên duyệt bill có phát sinh thăng hạng VÀ khi CheckPoint kích hoạt job thăng hạng hàng loạt ban đêm (`TASK_UP_CLASS`).
3. **`ILotteryPromotionService` / `LotteryPromotionService`**:
   - Quản lý 2 nhánh chương trình: Vé quay số may mắn (Lottery) và Quà tặng khuyến mãi (FirstTrans / Gift).
   - Bộ kiểm soát hạn ngạch 3 tầng (Quota theo Member/Chiến dịch, Member/Ngày, Toàn chiến dịch/Ngày).
   - Thuật toán bốc thăm ngẫu nhiên có trọng số (Weighted Random Allocation).
4. **`INotificationService` / `NotificationService`**:
   - Bắn Firebase Push Notification tức thì đến Mobile App với đầy đủ mẫu thông điệp cá nhân hóa.
5. **`IBillReviewService` / `BillReviewService`**:
   - Điều phối toàn bộ quy trình duyệt bill 6 bước nguyên tử.

---

## 3. Tích hợp Hệ thống Tự động hóa CheckPoint Platform

Dự án `CRM.Automation.Api` và `CRM.Automation.Infrastructure` đóng vai trò là Service Node chịu sự điều phối của **CheckPoint Scheduler**:

- **CheckPoint (Scheduler / Orchestrator)**:
  - Chỉ gửi các tham số định danh phiên chạy: `ExecutionId`, `TaskCode`, `TriggerTime`.
  - Không mang vác tham số nghiệp vụ (Non-business orchestrator).
- **Automation API Layer (`CRM.Automation.Api`)**:
  - Xác thực qua `ApiKey` header hoặc JWT Token.
  - Đọc cấu hình nghiệp vụ từ `appsettings.json` và chuyển tiếp xuống Infrastructure/BLL.
- **Automation Infrastructure (`CRM.Automation.Infrastructure`)**:
  - Gọi trực tiếp các Service trong `CRMMall.BillReview.BLL` để thực thi xử lý nghiệp vụ thực tế trên CSDL.

---

## 4. Hiện trạng Kiểm tra & Nghiệm thu (Verification Status)

- **Biên dịch Solution**:
  ```powershell
  dotnet build "D:\GIT PROJECT\GIGAMallMigration\CRMMall.sln"
  # Kết quả: Build succeeded. 0 Warning(s), 0 Error(s)
  ```
- **Kiểm thử tự động (Unit Tests)**:
  ```powershell
  dotnet test "D:\GIT PROJECT\GIGAMallMigration\CRMMall.sln"
  # Kết quả: Passed! Failed: 0, Passed: 7, Skipped: 0, Total: 7 (100% Pass)
  ```
