# CRM Automation Solution (.NET 8)

> **Giải pháp Tự động hóa CRM GigaMall** - Tách biệt độc lập khỏi legacy code, nâng cấp lên **.NET 8**, sẵn sàng tích hợp trực tiếp với hệ thống tự động hóa **CheckPoint Framework** (Task Type 1 WebAPI & Task Type 3 WebApiAsync).

---

## 1. Cấu trúc Giải pháp (Clean Architecture)

```
D:\GIT PROJECT\GIGAMallMigration\
├── CRMAutomationSln.sln
├── .gitignore
├── README.md
├── src\
│   ├── CRM.Automation.Core\            # .NET 8 Class Library (Models, Interfaces, Contracts, DTOs)
│   ├── CRM.Automation.Infrastructure\  # .NET 8 Class Library (Dapper, Repositories, Firebase Push, Workflows)
│   └── CRM.Automation.Api\             # .NET 8 ASP.NET Core WebAPI (Swagger, ApiKey Filter, Task Controllers)
└── tests\
    └── CRM.Automation.Tests\           # .NET 8 xUnit Test Suite
```

### Chi tiết các dự án:
- **`CRM.Automation.Core`**: Chứa toàn bộ Data Transfer Objects (DTOs), Process Results chuẩn hóa cho CheckPoint (`BOProcessResult`), Enums, và Interfaces nghiệp vụ (`IBurnPointService`, `IResetPointService`, `IMemberTierService`, `IFirebasePushService`).
- **`CRM.Automation.Infrastructure`**: Thực thi truy vấn dữ liệu hiệu năng cao với **Dapper** & **Microsoft.Data.SqlClient**, gọi Stored Procedures xử lý điểm theo lô (Batch Processing), tích hợp Firebase Cloud Messaging (hỗ trợ chế độ Simulation Mode).
- **`CRM.Automation.Api`**: Cổng WebAPI cung cấp các Endpoint chuẩn RESTful cho CheckPoint trigger qua HTTP/HTTPS. Bảo mật bằng Header `x-Api-Key`.
- **`CRM.Automation.Tests`**: Bộ unit test kiểm thử các hợp đồng tương thích CheckPoint.

---

## 2. Các Task API được cung cấp cho CheckPoint

Mọi API phản hồi theo chuẩn đối tượng `BOProcessResult` mà CheckPoint yêu cầu:
```json
{
  "Ok": true,
  "Message": "string",
  "Data": {},
  "ErrorCode": null
}
```

### Danh sách Endpoint:
| Tác vụ | HTTP Method | Endpoint | Mô tả |
| :--- | :---: | :--- | :--- |
| **Health Check** | `GET` | `/api/tasks/health` | Kiểm tra tình trạng hoạt động của service |
| **Nhắc nhở cháy điểm** | `POST` | `/api/tasks/burn-point-reminder` | Quét hội viên sắp hết hạn điểm và gửi Push Notification qua Firebase |
| **Chốt & Hủy điểm** | `POST` | `/api/tasks/reset-point` | Chốt và hủy số điểm hết hạn của hội viên (xét giao dịch phát sinh) |
| **Hạ hạng thành viên** | `POST` | `/api/tasks/downgrade-membership` | Đánh giá lại chi tiêu/điểm trong kỳ và hạ hạng hội viên nếu không đạt |

---

## 3. Cấu hình trên CheckPoint Automation UI

### Bước 1: Tạo Task trên giao diện CheckPoint
1. **Task Type**: Chọn `1 - WebAPI` (Đồng bộ) hoặc `3 - WebApiAsync` (Bất đồng bộ với Webhook).
2. **Method**: `POST` (hoặc `GET` cho Health check).
3. **Url**: `http://<API_HOST>:<PORT>/api/tasks/<endpoint>`
   *(Ví dụ: `http://localhost:5000/api/tasks/burn-point-reminder`)*.
4. **Headers**: Thêm dòng:
   - Key: `x-Api-Key`
   - Value: `<Khóa API cấu hình trong appsettings.json>`
5. **Body (JSON)**:
   - Task Nhắc nhở cháy điểm:
     ```json
     {
       "ResetPointYears": 2,
       "RemindDay": 30,
       "BatchSize": 500
     }
     ```
   - Task Chốt & Hủy điểm:
     ```json
     {
       "BatchSize": 500
     }
     ```

---

## 4. Cấu hình Ứng dụng (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=GigaMall_CRM_PROD;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
  },
  "Authentication": {
    "ApiKey": "GigaMall-CheckPoint-SecretKey-2026"
  },
  "Notification": {
    "GatewayUrl": "https://fcm.googleapis.com/fcm/send",
    "SimulatePush": true
  }
}
```

> **Lưu ý**: Khi `SimulatePush = true`, API sẽ ghi log mô phỏng việc gửi tin nhắn đến hội viên mà không thực hiện gửi thật ra Firebase FCM, rất an toàn cho việc kiểm thử môi trường UAT/Staging.

---

## 5. Hướng dẫn Khởi chạy & Kiểm thử

### Chạy kiểm thử:
```bash
dotnet test
```

### Chạy WebAPI:
```bash
dotnet run --project src/CRM.Automation.Api
```
Truy cập giao diện Swagger tại: `http://localhost:5000/` hoặc `https://localhost:5001/`

---

## 6. Hướng dẫn Liên kết Git Repository mới

Khi bạn có URL Git mới từ DevOps/Admin, thực hiện các lệnh sau tại thư mục này:

```bash
cd "D:\GIT PROJECT\GIGAMallMigration"
git init
git add .
git commit -m "Initial commit: CRM Automation .NET 8 Solution with CheckPoint integration"
git branch -M main
git remote add origin <URL_GIT_CUA_BAN>
git push -u origin main
```
