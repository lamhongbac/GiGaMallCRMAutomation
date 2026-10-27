# ĐẶC TẢ DI TRÚ & CHUẨN HÓA CSDL HÓA ĐƠN OCR (DATABASE SCHEMA MIGRATION)
## BẢNG DỮ LIỆU MSSOCRTRANS, 4 CỜ TRẠNG THÁI VÀ NHÓM TRƯỜNG ĐIỀU CHỈNH

> 🏢 **Hệ sinh thái**: CRM Loyalty Gigamall - Sao Mai Solution  
> 🎯 **Mục tiêu**: Bóc tách cấu trúc dữ liệu bảng `mssOCRTrans`, chuẩn hóa 4 cờ trạng thái (`OCRStatus`, `Reviewed`/`ReviewStatus`, `Approved`, `CRMProcess`), nhóm trường điều chỉnh `...A` và liên kết sang giao dịch thẻ `mssCardTrans`.  
> 🏛️ **Dự án cũ tham khảo**: `D:\GIT PROJECT\CRM SAOMAI\CRMPROFILELogic\SourceCode\CRMProfileManagement`  
> 📁 **Tệp nguồn tham khảo**: `MSA/MSAEntity/mssOCRTran.cs` & `MSA/MSAEntity/MSAModel.edmx`

---

## 🔸 PHẦN 1: HIỆN TRẠNG HỆ THỐNG CŨ (AS-IS / LEGACY ANALYSIS)

### 1.1. Cấu trúc thực thể cũ trong Entity Framework 6 (`mssOCRTran.cs`)
Trong hệ thống cũ, bảng `mssOCRTrans` (được ánh xạ thành `mssOCRTran` trong EF6) lưu trữ các trường sau:

```csharp
// Trích xuất từ MSA/MSAEntity/mssOCRTran.cs
public partial class mssOCRTran
{
    public System.Guid ID { get; set; }                    // Khóa chính
    public string OCRText { get; set; }                   // Văn bản thô OCR
    public System.DateTime ScanDate { get; set; }         // Ngày scan OCR
    public string ImageFileName { get; set; }             // Tên file ảnh
    public string BrandNumber { get; set; }               // Mã Brand từ OCR
    public string OutletNumber { get; set; }              // Mã Gian hàng từ OCR
    public string BillNumber { get; set; }                // Số hóa đơn từ OCR
    public string BillDateTime { get; set; }              // Ngày giờ hóa đơn từ OCR
    public string BillDateTimeFormat { get; set; }        // Định dạng ngày (dd/MM/yyyy)
    public string CashierName { get; set; }               // Tên thu ngân từ OCR
    public string OutletAddress { get; set; }             // Địa chỉ in trên bill
    public Nullable<decimal> BillTotal { get; set; }      // Tổng tiền từ OCR
    public Nullable<bool> Reviewed { get; set; }          // Cờ đã review (true/false)
    public Nullable<System.DateTime> ReviewDate { get; set; } // Ngày review
    public string ReviewBy { get; set; }                  // Người review
    public string ReviewNotes { get; set; }               // Ghi chú review / Lý do từ chối
    public Nullable<System.Guid> TransDocID { get; set; } // Liên kết sang mssCardTrans.DocID
    public Nullable<bool> Approved { get; set; }          // Kết quả: true=Duyệt, false=Từ chối
    public Nullable<decimal> PointGain { get; set; }      // Điểm tích lũy được
    public string OutletName { get; set; }                // Tên gian hàng (tra cứu)
    public string BrandName { get; set; }                 // Tên thương hiệu (tra cứu)
    public string FloorName { get; set; }                 // Tên tầng (gán khi duyệt)
    public string MallInfo { get; set; }                  // Tên TTTM (gán khi duyệt)
    public string MemID { get; set; }                     // Mã hội viên upload
    public Nullable<System.DateTime> UploadImageDateTime { get; set; } // Ngày upload
    public string BillNumberA { get; set; }               // Số bill do Admin sửa
    public Nullable<System.DateTime> BillDateTimeA { get; set; } // Ngày giờ do Admin sửa
    public Nullable<decimal> BillTotalA { get; set; }     // Tổng tiền do Admin sửa
    public string ConfigAddress { get; set; }
    public string ConfigOutletNumber { get; set; }
    public virtual mssCardTran mssCardTran { get; set; }  // Navigation Property
}
```

### 1.2. Hạn chế và Nút thắt của CSDL cũ:
1. **Thiếu cờ trạng thái bóc tách riêng biệt (`OCRStatus`)**: Hệ thống cũ chỉ dựa vào `DataStatus = 'Pending'` hoặc `ScanDate` để đoán xem bill đã được OCR hay chưa, dẫn đến việc khó phân loại hóa đơn chưa OCR (Tab 1) và hóa đơn đã OCR chờ duyệt (Tab 2).
2. **Thiếu cờ xác nhận xử lý Loyalty (`CRMProcess`)**: Khi admin bấm Phê duyệt, hệ thống cũ chạy đồng bộ ngay lập tức toàn bộ việc tích điểm, thăng hạng, xét quà. Nếu quá trình này gặp lỗi mạng hoặc timeout, trạng thái giữa bill và điểm bị lệch, không có cờ ghi nhận tiến trình.
3. **Các trường `...A` bị thiếu sót**: Hệ thống cũ có `BillNumberA`, `BillDateTimeA`, `BillTotalA` nhưng chưa hỗ trợ đầy đủ `BrandNumberA`, `OutletNumberA` cho Admin khi OCR nhận diện nhầm gian hàng.

---

## 🔹 PHẦN 2: THIẾT KẾ HỆ THỐNG MỚI (TO-BE / MODERN SPECIFICATION)

### 2.1. Chuẩn hóa Bảng dữ liệu `mssOCRTrans` trong .NET 8
Hệ thống mới tổ chức `mssOCRTrans` thành 5 nhóm trường rõ ràng:

| Nhóm dữ liệu | Tên trường | Kiểu dữ liệu | Nullable | Mô tả & Quy tắc nghiệp vụ |
| :--- | :--- | :--- | :---: | :--- |
| **1. Upload Data** | `ID` | `uniqueidentifier` | Không | Khóa chính duy nhất (GUID). |
| | `MemID` | `nvarchar(50)` | Không | Mã định danh khách hàng (SĐT/CardNo). |
| | `UploadImageDateTime`| `datetime` | Có | Thời điểm upload thành công từ Mobile. |
| | `ImagePath` | `nvarchar(500)` | Có | Thư mục con: `Transaction/ddMMyy`. |
| | `ImageFileName` | `nvarchar(250)` | Có | Tên file vật lý: `{MemID}_{Timestamp}.png`. |
| | `DataStatus` | `nvarchar(50)` | Có | Trạng thái kỹ thuật: `'Pending'`, `'OCR'`, `'Approved'`, `'Rejected'`. |
| **2. OCR Data** | `OCRText` | `nvarchar(max)` | Có | Toàn bộ chuỗi JSON trả về từ OCR Server. |
| | `ScanDate` | `datetime` | Có | Thời điểm AI OCR hoàn tất bóc tách. |
| | `BrandNumber` | `nvarchar(50)` | Có | Mã thương hiệu bóc tách bởi AI. |
| | `OutletNumber` | `nvarchar(50)` | Có | Mã gian hàng bóc tách bởi AI. |
| | `BillNumber` | `nvarchar(50)` | Có | Số hóa đơn bóc tách bởi AI. |
| | `BillDateTime` | `nvarchar(50)` | Có | Chuỗi ngày giờ in trên bill từ AI. |
| | `BillTotal` | `decimal(18,2)` | Có | Tổng tiền hóa đơn bóc tách bởi AI. |
| | `CashierName` | `nvarchar(100)` | Có | Tên nhân viên thu ngân bóc tách bởi AI. |
| | `OutletAddress` | `nvarchar(250)` | Có | Địa chỉ cửa hàng bóc tách bởi AI. |
| **3. Admin Data** | `BrandNumberA` | `nvarchar(50)` | Có | **[MỚI]** Mã thương hiệu do Admin điều chỉnh. |
| | `OutletNumberA` | `nvarchar(50)` | Có | **[MỚI]** Mã gian hàng do Admin điều chỉnh. |
| | `BillNumberA` | `nvarchar(50)` | Có | Số hóa đơn do Admin điều chỉnh. |
| | `BillDateTimeA` | `datetime` | Có | Ngày giờ hóa đơn do Admin điều chỉnh. |
| | `BillTotalA` | `decimal(18,2)` | Có | Tổng tiền hóa đơn do Admin điều chỉnh. |
| **4. Review Data** | `ReviewStatus` / `Reviewed` | `bit` | Có | `0` = Chưa review (Mặc định); `1` = Đã review. |
| | `Approved` | `bit` | Có | `1` = Phê duyệt (Đồng ý); `0` = Từ chối (Reject). |
| | `ReviewBy` | `nvarchar(50)` | Có | Username/LoginID của Admin thực hiện review. |
| | `ReviewDate` | `datetime` | Có | Thời điểm Admin chốt duyệt hóa đơn. |
| | `ReviewNotes` | `nvarchar(500)` | Có | Ghi chú thẩm định / Lý do từ chối. |
| **5. Post-Review & Loyalty**| `OCRStatus` | `bit` | Có | **[MỚI]** `0/null` = Chưa OCR (Tab 1); `1` = Đã OCR (Tab 2). |
| | `CRMProcess` | `bit` | Có | **[MỚI]** `0/null` = Chưa chạy loyalty; `1` = Đã chạy điểm, quà, vé số. |
| | `TransDocID` | `uniqueidentifier` | Có | Khóa ngoại liên kết sang bảng `mssCardTrans.DocID`. |
| | `PointGain` | `decimal(18,2)` | Có | Số điểm thưởng tích lũy thực tế nhận được. |
| | `OutletName` | `nvarchar(250)` | Có | Tên gian hàng (tra cứu từ bảng `Outlet`). |
| | `BrandName` | `nvarchar(250)` | Có | Tên thương hiệu (tra cứu từ bảng `Brand`). |
| | `FloorName` | `nvarchar(250)` | Có | Tên tầng (tra cứu từ `Outlet.FloorID -> Floor.Name`). |
| | `MallInfo` | `nvarchar(250)` | Có | Tên chi nhánh TTTM (tra cứu từ `Outlet.BranchID -> Branch.Name`). |

### 2.2. Vòng đời chi tiết của 4 Cờ Trạng Thái (Status Flags Transition)

```
[Khách upload B1]  ==> OCRStatus = 0/null, ReviewStatus = 0, Approved = null, CRMProcess = null
          │
          ▼ (Qua Tab 1 bấm [Save OCR])
[Đã OCR B2]        ==> OCRStatus = 1, ReviewStatus = 0, Approved = null, CRMProcess = null
          │
          ▼ (Qua Tab 2 Admin Thẩm định)
┌─────────┴────────────────────────────────────────┐
▼ [BẤM TỪ CHỐI]                                    ▼ [BẤM ĐỒNG Ý]
OCRStatus = 1                                      OCRStatus = 1
ReviewStatus = 1                                   ReviewStatus = 1
Approved = 0 (Từ chối)                             Approved = 1 (Đồng ý)
CRMProcess = null (Không xử lý)                    CRMProcess = 0/null (Chờ xử lý)
(Bắn Push Noti lý do)                              │
                                                   ▼ (Bấm nút [CRMProcess])
                                                   [HOÀN TẤT LOYALTY]
                                                   CRMProcess = 1
                                                   Lưu TransDocID, PointGain, Vé số, Quà
```

### 2.3. Quy tắc Số Final & Tính Bất biến của Audit Trail
- **Công thức Số Final**:
  $$\text{FinalBillTotal} = \begin{cases} \text{BillTotalA} & \text{nếu } \text{BillTotalA} \text{ có giá trị} \\ \text{BillTotal} & \text{nếu } \text{BillTotalA IS NULL} \end{cases}$$
  Áp dụng tương tự cho `BillNumber`, `BillDateTime`, `OutletNumber`, `BrandNumber`.
- **Ngăn ngừa ghi đè**: Giao diện cập nhật chỉ được UPDATE vào các cột có hậu tố `...A`. Tuyệt đối không UPDATE vào cột gốc của OCR để luôn giữ nguyên vẹn dữ liệu máy đọc ban đầu.
