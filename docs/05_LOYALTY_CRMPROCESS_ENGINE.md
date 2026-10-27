# ĐẶC TẢ NGHIỆP VỤ LOYALTY CRMPROCESS ENGINE (POINTS, GIFTS & LOTTERY)
## BƯỚC 4: TÍCH ĐIỂM, THĂNG HẠNG, XÉT QUÀ TẶNG VÀ CẤP VÉ SỐ MAY MẮN

> 🏢 **Hệ sinh thái**: CRM Loyalty Gigamall - Sao Mai Solution  
> 🎯 **Mục tiêu**: Chuyển đổi toàn bộ khối nghiệp vụ Loyalty monolithic từ `GigaBillProcess.cs` sang dịch vụ chuẩn hóa `LoyaltyProcessService` trong `CRMMall.BillReview.BLL`, thực thi có kiểm soát qua nút [CRMProcess] kèm cơ chế ghi log chi tiết.  
> 🏛️ **Dự án cũ tham khảo**: `D:\GIT PROJECT\CRM SAOMAI\CRMPROFILELogic\SourceCode\CRMProfileManagement`  
> 📁 **Tệp nguồn tham khảo**: `CRMProfileManagement/OCRBillPROCESS/GigaBillProcess.cs` & `CRMBase/OCRBillPROCESS/BillProcess.cs`

---

## 🔸 PHẦN 1: HIỆN TRẠNG HỆ THỐNG CŨ (AS-IS / LEGACY ANALYSIS)

### 1.1. Luồng xử lý nguyên khối trong `GigaBillProcess.BillReview`
Trong mã nguồn cũ tại `GigaBillProcess.cs` (từ dòng 672 đến dòng 870), khi admin duyệt hóa đơn, chuỗi nghiệp vụ sau bị bó cứng đồng bộ:

```csharp
// Trích xuất từ CRMProfileManagement/OCRBillPROCESS/GigaBillProcess.cs
public BODataProcessResult BillReview(CRMGiGaBill bill, bool isApproved, string reviewby, ...)
{
    // 1. Kiểm tra trùng hóa đơn (IsDuplicateOCRBill)
    bool isDuplicate = IsDuplicateOCRBill(bill);
    if (isDuplicate) { return Error("Duplicate bill number"); }

    // 2. Tạo bản ghi giao dịch thẻ mssCardTrans
    CardTransUI cardtransdata = new CardTransUI();
    cardtransdata.DocID = Guid.NewGuid();
    cardtransdata.TotalBillValue = bill.BillTotalA; // LẤY SỐ ADMIN
    cardtransdata.CheckNo = bill.BillNumberA;
    cardtransdata.OutletNumber = bill.OutletNumberA;
    cardtransdata.POSBusinessDate = bill.BillDateTimeA;
    cardtransdata.CardNo = GetCardNo(ocrtrialtrandata.MemID);

    // 3. Tính điểm theo phân hạng hội viên (MemClassLedger)
    MemClassLedgerBaseUI memclass = BaseMemClassLedger.GetMemClassLedgerData(ocrtrialtrandata.MemID);

    // 4. Xét trao quà tặng (GetPromCode)
    if (promotionID.HasValue) { GetPromCode(promotionID.Value, bill); }

    // 5. Xét cấp mã quay số trúng thưởng (LotteryProgramUI)
    // ...
}
```

### 1.2. Hạn chế và Rủi ro của mô hình cũ:
1. **Khối monolithic khó kiểm soát**: Việc duyệt hóa đơn (Review) và chạy tính điểm (Loyalty) bị gộp làm một. Nếu hệ thống quay số bị lỗi hoặc nghẽn mạng, toàn bộ giao dịch duyệt bill bị rollback thất bại.
2. **Thiếu cơ chế ghi log độc lập**: Không có log chi tiết từng chặng (Điểm cơ bản là bao nhiêu, quà gì được cấp, vé số nào được sinh ra), dẫn đến khó khăn khi khách hàng khiếu nại.
3. **Không có khả năng tái xử lý (Re-try)**: Nếu tính điểm thất bại, hóa đơn bị kẹt ở trạng thái lơ lửng, nhân viên phải can thiệp SQL bằng tay.

---

## 🔹 PHẦN 2: THIẾT KẾ HỆ THỐNG MỚI (TO-BE / MODERN SPECIFICATION)

### 2.1. Tách biệt Khâu Thẩm định (Bước 3) và Khâu Xử lý Loyalty (Bước 4)
* **Bước 3 (Thẩm định)**: Admin chỉ tập trung đối chiếu số liệu và ấn **[Đồng ý]** $\rightarrow$ Hệ thống ghi nhận `Reviewed = 1`, `Approved = 1`, khóa hóa đơn.
* **Bước 4 (Nghiệp vụ Loyalty)**: Được thực thi thông qua **Nút [CRMProcess]** (Giai đoạn kiểm nghiệm thủ công) hoặc Service chạy ngầm:
  - Điều kiện lọc: `Reviewed = 1 AND Approved = 1 AND (CRMProcess = 0 OR CRMProcess IS NULL)`.

### 2.2. Ba Mục tiêu Nghiệp vụ Cốt lõi của [CRMProcess] (Kèm Gửi Notification Cuối Từng Bước & Logging)

```
NÚT [CRMPROCESS]
       │
       ├──► 🎯 MỤC TIÊU 1: TÍNH ĐIỂM CƠ BẢN & ĐIỂM THĂNG HẠNG (BASE & RANK POINTS)
       │    - Đọc số Final (BillTotalA ?? BillTotal)
       │    - Tra cứu tỷ lệ tích điểm theo ClassNo hội viên (Bronze, Silver, Gold, Diamond)
       │    - Tạo bản ghi mssCardTrans (DocID = Guid.NewGuid())
       │    - Cập nhật số dư điểm 3 sổ cái (CLMLedger, MemClassLedger, CardTrans)
       │    - Gửi Notification tích điểm thành công: Event GetPointBySale
       │    - Thẩm định điều kiện UpClass (Thăng hạng thẻ)
       │    - 📝 GHI LOG: Log.WriteLog("CRMProcess_Point", MemID, DocID, Points, OldClass, NewClass)
       │    - 🔔 GỬI NOTIFICATION THĂNG HẠNG: NẾU CÓ LÊN HẠNG -> Bắn Notification Event MemberUpClass
       │
       ├──► 🎯 MỤC TIÊU 2: XÉT THƯỞNG QUÀ TẶNG KHUYẾN MÃI (GIFT PROMOTIONS)
       │    - Tra cứu các chiến dịch khuyến mãi đang kích hoạt tại TTTM theo Outlet/Brand
       │    - Kiểm tra điều kiện doanh số hóa đơn (FinalTotal >= Ngưỡng quà)
       │    - Sinh mã Coupon / Gift Voucher cho hội viên
       │    - 📝 GHI LOG: Log.WriteLog("CRMProcess_Gift", MemID, PromotionID, PromoCode, Status)
       │    - 🔔 GỬI NOTIFICATION QUÀ TẶNG: NẾU ĐƯỢC NHẬN QUÀ -> Cuối bước bắn Notification Event SendPromotion (mã quà, hạn dùng)
       │
       └──► 🎯 MỤC TIÊU 3: CẤP MÃ VÉ SỐ MAY MẮN (LOTTERY TICKETS / LUCKY DRAW)
            - Áp dụng công thức quy đổi: Số lượng vé = Floor(FinalTotal / Giá trị 1 vé)
            - Sinh các dãy số may mắn ngẫu nhiên duy nhất (LotteryCode) gắn với hóa đơn
            - 📝 GHI LOG: Log.WriteLog("CRMProcess_Lottery", MemID, BillNumber, TotalTickets, TicketCodes)
            - 🔔 GỬI NOTIFICATION VÉ SỐ: NẾU CÓ VÉ SỐ -> Cuối bước bắn Notification Event SendLottery (danh sách các mã vé dự thưởng)
```

### 2.3. Hoàn tất chu trình & Lưu vết dữ liệu
1. **Cập nhật CSDL `mssOCRTrialTrans`**:
   - `CRMProcess = 1`: Xác nhận bill đã được tính toán đầy đủ.
   - `CRMProcessDate = GETDATE()`: Ghi nhận thời điểm hoàn tất CRM Process.
   - `TransDocID = cardtransdata.DocID`: Lưu liên kết tới giao dịch thẻ.
   - `PointGain = reviewpoints`: Ghi nhận số điểm thực tế khách nhận được.
   - `DataStatus = 'Completed'`.
2. **Ghi nhận lịch sử gửi Notification**:
   - Lưu trữ các bản ghi thông báo tương ứng vào bảng `mssNotification` (với `Status = 'Sent'`) để Mobile App hiển thị trong Hộp thư thông báo của Hội viên.
