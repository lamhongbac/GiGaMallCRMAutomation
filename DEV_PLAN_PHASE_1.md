# KẾ HOẠCH PHÁT TRIỂN CHI TIẾT - GIAI ĐOẠN 1 (DEV PLAN - PHASE 1)
## HỆ THỐNG TỰ ĐỘNG HÓA CRM GIGAMALL (.NET 8 & CHECKPOINT ORCHESTRATION)

> **Mục tiêu Giai đoạn 1**: Hoàn thiện trọn vẹn **Vòng đời Thẻ & Điểm (Points & Membership Tier Lifecycle)**, chuyển đổi dứt điểm 4 ứng dụng Console cũ sang kiến trúc **.NET 8 Clean Architecture**, tích hợp đồng bộ vào hệ thống lập lịch tự động **CheckPoint Framework**.

---


> 📌 **Kế hoạch Chi tiết Phân hệ Duyệt hóa đơn & OCR Server Integration**:  
> Toàn bộ đặc tả chu trình sống hóa đơn (B1 -> B4) và Kế hoạch phát triển Giai đoạn 2.1 (Interactive UI) & 2.2 (CheckPoint Framework) được theo dõi chi tiết tại:  
> 👉 [`docs/OCR_Bill_Processing_Lifecycle_Spec_And_DevPlan.md`](docs/OCR_Bill_Processing_Lifecycle_Spec_And_DevPlan.md).

## 1. TỔNG QUAN & MA TRẬN TÁC VỤ GIAI ĐOẠN 1

Giai đoạn 1 tập trung chuẩn hóa 4 tác vụ nghiệp vụ nền tảng:

| STT | Mã Tác vụ (TaskCode) | Nghiệp vụ Cốt lõi | Endpoint API (.NET 8) | Chu kỳ CheckPoint (Cron) | Tình trạng hiện tại |
|:---:|:---|:---|:---|:---|:---:|
| **1** | `BURN_POINT_REMINDER` | Quét và gửi Push Notification nhắc nhở điểm sắp hết hạn trước $N$ ngày | `POST /api/tasks/burn-point-reminder` | `0 9 * * *` (09:00 hàng ngày) | **Đã hoàn thành Core** (Cần review SP & Notification) |
| **2** | `RESET_POINT` | Chốt trừ điểm hết hạn thực tế định kỳ cho hội viên không phát sinh GD | `POST /api/tasks/reset-point` | `5 0 1 * *` (00:05 ngày 1 hàng tháng) | **Đã có Controller/Service** (Cần hoàn thiện Transaction & SP) |
| **3** | `UP_CLASS` | Tự động nâng hạng thẻ hội viên khi đủ tích lũy chi tiêu + Tặng quà/điểm + Bắn Noti | `POST /api/tasks/up-class` | `0 1 * * *` (01:00 hàng đêm) | **Đã hoàn thành BLL Core** (`IMembershipTierService` trong `CRMMall.BillReview.BLL`, dùng chung) |
| **4** | `DOWNGRADE_MEMBERSHIP` | Xét duyệt và hạ hạng thẻ định kỳ nếu không đạt chỉ tiêu duy trì hạn thẻ | `POST /api/tasks/downgrade-membership` | `30 0 1 * *` (00:30 ngày 1 hàng tháng/quý) | **Đã có Skeleton** (Cần hoàn thiện Business Logic & SP) |

---

## 2. PHÂN RÃ CÔNG VIỆC CHI TIẾT (WORK BREAKDOWN STRUCTURE - WBS)

```
GIAI ĐOẠN 1: VÒNG ĐỜI THẺ & ĐIỂM
│
├── MODULE 1: BURN POINT REMINDER (Hardening & Hoàn thiện)
│   ├── [1.1] Rà soát và chuẩn hóa Stored Procedure GetMembersToRemindBurnPoint
│   ├── [1.2] Đảm bảo ghi log lịch sử nhắc điểm vào bảng mssPointBurnReminderHistory
│   ├── [1.3] Hoàn thiện tích hợp cổng Push Notification (mssNotification & FCM)
│   └── [1.4] Kiểm thử biên (Không có hội viên hết hạn, hội viên trùng lặp)
│
├── MODULE 2: RESET POINT (Thực thi trừ điểm & Giao dịch an toàn)
│   ├── [2.1] Xây dựng Stored Procedure / Dapper Query chốt trừ điểm mssPoint_ResetExpired
│   ├── [2.2] Đóng gói Transaction an toàn (ACID): Trừ điểm ví hội viên & Ghi Transaction Log
│   ├── [2.3] Cấu hình tham số GracePeriod / ResetInterval qua appsettings.json
│   └── [2.4] Cập nhật Báo cáo biến động điểm sau khi trừ điểm thành công
│
├── MODULE 3: UP CLASS (Tự động nâng hạng thẻ hội viên)
│   ├── [3.1] Khởi tạo Model/DTO: MemberUpClassDto, UpClassResultDto, UpClassSettings
│   ├── [3.2] Port Stored Procedure GetMembersToUpClass sang Repository
│   ├── [3.3] Hiện thực logic Upgrade hạng thẻ (Kế thừa logic BaseCardTrans.Upgrade)
│   ├── [3.4] Tự động tạo giao dịch quà tặng nâng hạng (PromotionGiftTrans)
│   ├── [3.5] Bắn Notification chúc mừng nâng hạng thẻ & cộng điểm thưởng
│   └── [3.6] Expose API Endpoint POST /api/tasks/up-class & Ghi audit log
│
├── MODULE 4: DOWNGRADE MEMBERSHIP (Xét duyệt & Hạ hạng thẻ định kỳ)
│   ├── [4.1] Khởi tạo Model/DTO: MemberDowngradeDto, DowngradeResultDto
│   ├── [4.2] Port Stored Procedure GetMembersToResetClass sang Repository
│   ├── [4.3] Hiện thực logic Downgrade hạng thẻ (Kế thừa BaseCardTrans.Downgrade)
│   ├── [4.4] Bắn Notification thông báo điều chỉnh hạng thẻ hội viên
│   └── [4.5] Expose API Endpoint POST /api/tasks/downgrade-membership
│
├── MODULE 5: BÁO CÁO & GIÁM SÁT VẬN HÀNH (Reporting & Auditing)
│   ├── [5.1] API Báo cáo nhắc nhở & tiến trình cháy điểm (GET /api/reports/burn-point)
│   ├── [5.2] API Báo cáo tổng hợp hiệu quả cứu điểm vs hủy điểm (GET /api/reports/burn-point/summary)
│   ├── [5.3] API Báo cáo chi tiết hội viên đã thực tế bị trừ điểm (GET /api/reports/reset-point)
│   ├── [5.4] API Báo cáo tổng hợp số liệu trừ điểm hết hạn (GET /api/reports/reset-point/summary)
│   ├── [5.5] API Báo cáo lịch sử nâng hạng & hạ hạng thẻ (GET /api/reports/membership/history)
│   └── [5.6] Health Check & Readiness Endpoint (GET /api/tasks/health)
│
└── MODULE 6: TÍCH HỢP ĐIỀU PHỐI VỚI CHECKPOINT FRAMEWORK
    ├── [6.1] Khai báo 4 Tasks trên CheckPoint (Task Type 1 WebAPI / Task Type 3 WebApiAsync)
    ├── [6.2] Cấu hình Header x-Api-Key và Payload định danh tĩnh
    ├── [6.3] Thiết lập chính sách Retry (3 lần, khoảng cách 5 phút) và Dead-letter Alert
    └── [6.4] Hoàn thiện tài liệu Hướng dẫn Vận hành & Manual Test (HTTP Files)
```

---

## 3. THIẾT KẾ KỸ THUẬT CHI TIẾT TỪNG MODULE

### 3.1. Module 1: Burn Point Reminder
* **Tầng Data**: Gọi SP `GetMembersToRemindBurnPoint` (hoặc `mssPointBurnReminder_GetList`).
* **Logic xử lý**:
  - Quét hội viên có điểm tích lũy cách đây $\ge 2$ năm và chưa phát sinh giao dịch trong 30 ngày qua.
  - Phân trang batch (mặc định 500 records/lần) để tránh nghẽn bộ nhớ.
  - Gửi thông báo qua Push Gateway & lưu thông báo vào hộp thư `mssNotification`.
* **Cấu hình `appsettings.json`**:
  ```json
  "BurnPointSettings": {
    "ResetPointYears": 2,
    "RemindDay": 30,
    "BatchSize": 500,
    "PointGatewayUrl": "http://internal-crm-gateway/api/notification"
  }
  ```

### 3.2. Module 2: Reset Point (Hủy điểm hết hạn)
* **Tầng Data**: Thực thi thủ tục chốt hủy điểm `mssPointReset_Execute`.
* **Quy tắc an toàn (Safety Rules)**:
  - Bắt buộc chạy trong `IDbTransaction`. Nếu 1 batch gặp sự cố, rollback an toàn và ghi log lỗi.
  - Không xóa vĩnh viễn dòng dữ liệu cũ, chỉ cập nhật trạng thái `IsExpired = 1`, ghi giảm số dư trong `mssCardLedger` và tạo giao dịch trừ điểm âm `TransType = RESET_EXPIRED`.
* **Cấu hình `appsettings.json`**:
  ```json
  "ResetPointSettings": {
    "ExpiredYears": 2,
    "GracePeriodDays": 0,
    "BatchSize": 1000
  }
  ```

### 3.3. Module 3: UpClass (Tự động nâng hạng thẻ)
* **Chuyển đổi từ**: `D:\GIT PROJECT\CRM SAOMAI\...\Utilities\UpClass\Program.cs`
* **Tầng Data**:
  - Query: `exec GetMembersToUpClass` $\rightarrow$ Trả về danh sách hội viên đủ điều kiện thăng hạng.
  - Mutation: `BaseCardTrans.Upgrade(cardNo)` $\rightarrow$ Cập nhật `ClassNo`, `ClassName` mới trong hệ thống thẻ.
* **Quy trình thực thi**:
  1. Quét danh sách hội viên đủ điều kiện lên hạng từ SP `GetMembersToUpClass`.
  2. Với mỗi hội viên:
     - Thực thi nâng hạng thẻ trong DB.
     - Lấy nội dung thông báo mẫu từ `BaseNotificationMessage.GetMessage("MemberUpClass")`.
     - Gửi Push Notification chúc mừng thăng hạng kèm quà tặng.
     - Tự động sinh `PromotionGiftTransModel` tặng Voucher/Quà chào mừng hạng mới.
     - Tặng điểm thưởng thăng hạng (nếu có chính sách).
     - Ghi log vết vào `mssTraceLog`.

### 3.4. Module 4: DowngradeMembership (Hạ hạng thẻ định kỳ)
* **Chuyển đổi từ**: `D:\GIT PROJECT\CRM SAOMAI\...\DowngradeMembership\Program.cs`
* **Tầng Data**:
  - Query: `exec [GetMembersToResetClass]` $\rightarrow$ Danh sách hội viên hết kỳ hạn duy trì mà không đạt KPI chi tiêu.
  - Mutation: `BaseCardTrans.Downgrade(memID, currentClass, nextClass)`.
* **Quy trình thực thi**:
  1. Quét danh sách hội viên cần điều chỉnh hạng.
  2. Thực thi hạ hạng thẻ và cập nhật quyền lợi thẻ tương ứng.
  3. Gửi thông báo thông tin hạng thẻ mới đến Mobile App của hội viên.
  4. Ghi audit log.

---

## 4. TÍCH HỢP ĐIỀU PHỐI VỚI CHECKPOINT FRAMEWORK

CheckPoint là Scheduler phi nghiệp vụ, chỉ gọi qua WebAPI kèm theo các tham số kỹ thuật tĩnh:

### 4.1. Thông số Cấu hình CheckPoint Tasks

#### Task 1: Nhắc nhở Cháy Điểm
* **Task Code**: `TASK_BURN_POINT_REMINDER`
* **Task Type**: Type 1 (WebAPI Sync) hoặc Type 3 (WebApiAsync)
* **URL**: `http://<crm-automation-host>:5000/api/tasks/burn-point-reminder`
* **HTTP Method**: `POST`
* **Headers**:
  - `Content-Type`: `application/json`
  - `x-api-key`: `GigaMall-CheckPoint-SecretKey-2026`
* **Cron Expression**: `0 9 * * *` (Mỗi ngày lúc 09:00 sáng)
* **Payload**:
  ```json
  {
    "ExecutionId": "CP-BPR-{{$timestamp}}",
    "TaskCode": "TASK_BURN_POINT_REMINDER",
    "TriggerBy": "CheckPointScheduler"
  }
  ```

#### Task 2: Chốt Trừ Điểm Hết Hạn
* **Task Code**: `TASK_RESET_POINT`
* **URL**: `http://<crm-automation-host>:5000/api/tasks/reset-point`
* **HTTP Method**: `POST`
* **Cron Expression**: `5 0 1 * *` (00:05 sáng ngày đầu tiên của mỗi tháng)
* **Payload**:
  ```json
  {
    "ExecutionId": "CP-RSP-{{$timestamp}}",
    "TaskCode": "TASK_RESET_POINT",
    "TriggerBy": "CheckPointScheduler"
  }
  ```

#### Task 3: Tự Động Nâng Hạng Thẻ
* **Task Code**: `TASK_UP_CLASS`
* **URL**: `http://<crm-automation-host>:5000/api/tasks/up-class`
* **HTTP Method**: `POST`
* **Cron Expression**: `0 1 * * *` (01:00 sáng mỗi đêm)
* **Payload**:
  ```json
  {
    "ExecutionId": "CP-UPC-{{$timestamp}}",
    "TaskCode": "TASK_UP_CLASS",
    "TriggerBy": "CheckPointScheduler"
  }
  ```

#### Task 4: Hạ Hạng Thẻ Định Kỳ
* **Task Code**: `TASK_DOWNGRADE_MEMBERSHIP`
* **URL**: `http://<crm-automation-host>:5000/api/tasks/downgrade-membership`
* **HTTP Method**: `POST`
* **Cron Expression**: `30 0 1 * *` (00:30 sáng ngày mùng 1 hàng tháng)
* **Payload**:
  ```json
  {
    "ExecutionId": "CP-DGM-{{$timestamp}}",
    "TaskCode": "TASK_DOWNGRADE_MEMBERSHIP",
    "TriggerBy": "CheckPointScheduler"
  }
  ```

---

## 5. KẾ HOẠCH KIỂM THỬ (TEST PLAN & MANUAL VERIFICATION)

### 5.1. File kịch bản kiểm thử nhanh (`CRM.Automation.Api.http`)
Nhà phát triển và đội DevOps có thể kiểm thử toàn bộ 4 task bằng file HTTP trực tiếp trên Visual Studio / VS Code:
1. `GET /api/tasks/health` $\rightarrow$ Đảm bảo service sống và DB kết nối tốt.
2. `POST /api/tasks/burn-point-reminder` $\rightarrow$ Test nhắc điểm.
3. `POST /api/tasks/reset-point` $\rightarrow$ Test trừ điểm.
4. `POST /api/tasks/up-class` $\rightarrow$ Test thăng hạng.
5. `POST /api/tasks/downgrade-membership` $\rightarrow$ Test hạ hạng.
6. `GET /api/reports/burn-point/summary` $\rightarrow$ Kiểm chứng số liệu sau khi chạy.

### 5.2. Tiêu chuẩn Hoàn thành (Definition of Done - DoD)
- [ ] Không còn phụ thuộc bất kỳ Console App nào của Phase 1 trong Windows Task Scheduler.
- [ ] Tất cả tham số nghiệp vụ được chuyển vào `appsettings.json`, CheckPoint chỉ truyền tham số phi nghiệp vụ.
- [ ] Toàn bộ logic DB được bao bọc Transaction, có xử lý ngoại lệ và ghi log lỗi đầy đủ.
- [ ] Endpoint bảo mật tuyệt đối bằng ApiKey Filter (`x-api-key`).
- [ ] 100% các task được định nghĩa và kiểm thử kích hoạt thành công trên Dashboard của CheckPoint.
- [ ] Mã nguồn biên dịch thành công 0 Warning, 0 Error trên nền tảng .NET 8.