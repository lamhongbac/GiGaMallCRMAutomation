# CRM Automation Solution (.NET 8)

> **Giải pháp Tự động hóa CRM GigaMall** - Tách biệt độc lập khỏi legacy code, nâng cấp lên **.NET 8**, sẵn sàng tích hợp trực tiếp với hệ thống tự động hóa **CheckPoint Framework** (Task Type 1 WebAPI & Task Type 3 WebApiAsync).

---

## 1. Cấu trúc Giải pháp (Clean Architecture)

```
D:\GIT PROJECT\GIGAMallMigration\
├── CRMAutomationSln.sln
├── .gitignore                          # Cấu hình chuẩn Git cho .NET
├── README.md                           # Tài liệu kiến trúc và hướng dẫn vận hành
├── src\
│   ├── CRM.Automation.Core\            # .NET 8 Class Library (Models, Interfaces, Contracts, DTOs, Settings)
│   ├── CRM.Automation.Infrastructure\  # .NET 8 Class Library (Dapper, Repositories, Firebase Push, Workflows)
│   └── CRM.Automation.Api\             # .NET 8 ASP.NET Core WebAPI (Swagger, ApiKey Filter, Task Controllers)
└── tests\
    └── CRM.Automation.Tests\           # .NET 8 xUnit Test Suite
```

---

## 2. Triết lý Thiết kế: Tách biệt Nghiệp vụ & Điều phối Lập lịch

### ① Phía CheckPoint (Scheduler / Orchestrator):
- **Bản chất**: Hệ thống điều phối lập lịch phi nghiệp vụ (Non-business).
- **Trách nhiệm**: Chỉ gửi các tham số kỹ thuật tĩnh (bất biến) để định danh và theo vết phiên chạy:
  ```json
  {
    "ExecutionId": "CHECKPOINT-RUN-2026-001",
    "TaskCode": "TASK_BURN_POINT_REMINDER",
    "TriggerTime": "2026-10-20T01:00:00Z",
    "TriggerBy": "CheckPointScheduler"
  }
  ```
  *(CheckPoint hoàn toàn không mang vác bất kỳ tham số nghiệp vụ nào như số năm hay số ngày).*

### ② Phía API Layer (`CRM.Automation.Api`):
- Inject `IOptions<BurnPointSettings>` đọc các giá trị mặc định cấu hình trong `appsettings.json`.
- Truyền trực tiếp các tham số nghiệp vụ này xuống tầng Service khi nhận trigger từ CheckPoint.

### ③ Phía Domain Service (`CRM.Automation.Infrastructure`):
- Khai báo method với các **default parameters**:
  ```csharp
  Task<BOProcessResult> ExecuteReminderAsync(
      string executionId = "",
      int resetPointYears = 2,
      int remindDay = 30,
      int batchSize = 500,
      CancellationToken ct = default);
  ```
- **Sử dụng trực tiếp** các tham số `resetPointYears`, `remindDay`, `batchSize` trong luồng xử lý mà không cần phải kiểm tra `null` hay `<= 0`.

---

## 3. Cấu hình Nghiệp vụ Tập trung (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=GigaMall_CRM_PROD;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
  },
  "Authentication": {
    "ApiKey": "GigaMall-CheckPoint-SecretKey-2026"
  },
  "BurnPointSettings": {
    "ResetPointYears": 2,
    "RemindDay": 30,
    "BatchSize": 500
  },
  "ResetPointSettings": {
    "BatchSize": 500
  },
  "MemberTierSettings": {
    "EvaluationPeriodMonths": 12
  },
  "Notification": {
    "GatewayUrl": "https://fcm.googleapis.com/fcm/send",
    "SimulatePush": true
  }
}
```

---

## 4. Cấu hình trên CheckPoint Automation UI

1. **Task Type**: Chọn `1 - WebAPI` (Đồng bộ) hoặc `3 - WebApiAsync` (Bất đồng bộ với Webhook).
2. **Method**: `POST` (hoặc `GET` cho Health check).
3. **Url**: `http://<API_HOST>:<PORT>/api/tasks/<endpoint>`
   *(Ví dụ: `http://localhost:5000/api/tasks/burn-point-reminder`)*.
4. **Headers**: Thêm dòng:
   - Key: `x-Api-Key`
   - Value: `<Khóa API cấu hình trong appsettings.json>`
5. **Body**: Truyền thông tin ngữ cảnh phiên chạy từ CheckPoint (hoặc để trống `{}`):
   ```json
   {
     "ExecutionId": "{TaskExecutionId}",
     "TaskCode": "BURN_POINT_REMINDER",
     "TriggerBy": "CheckPointScheduler"
   }
   ```

---

## 5. Danh sách Endpoints Sẵn sàng

| Tác vụ | HTTP Method | Endpoint | Mô tả |
| :--- | :---: | :--- | :--- |
| **Health Check** | `GET` | `/api/tasks/health` | Kiểm tra tình trạng hoạt động của service |
| **Nhắc nhở cháy điểm** | `POST` | `/api/tasks/burn-point-reminder` | Quét hội viên sắp hết hạn điểm và gửi Push Notification qua Firebase |
| **Chốt & Hủy điểm** | `POST` | `/api/tasks/reset-point` | Chốt và hủy số điểm hết hạn của hội viên (xét giao dịch phát sinh) |
| **Hạ hạng thành viên** | `POST` | `/api/tasks/downgrade-membership` | Đánh giá lại chi tiêu/điểm trong kỳ và hạ hạng hội viên nếu không đạt |

---

## 6. Hướng dẫn Liên kết Git Repository mới

Khi bạn có URL Git mới từ DevOps/Admin, thực hiện các lệnh sau tại thư mục này:

```bash
cd "D:\GIT PROJECT\GIGAMallMigration"
git init
git add .
git commit -m "feat: Pure separation between CheckPoint static params and CRM appsettings defaults"
git branch -M main
git remote add origin <URL_GIT_CUA_BAN>
git push -u origin main
```
