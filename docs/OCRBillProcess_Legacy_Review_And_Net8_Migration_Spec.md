# TÀI LIỆU KHẢO SÁT DỰ ÁN CŨ "OCRBILLPROCESS" & ĐẶC TẢ THIẾT KẾ VIẾT LẠI TRÊN NỀN TẢNG MỚI (.NET 8 CORE)
## (LEGACY ARCHITECTURE REVIEW & MODERN .NET 8 MIGRATION SPECIFICATION)

> 📌 **Tài liệu phục vụ**: Đội ngũ Kiến trúc sư Phần mềm & Kỹ sư Lập trình Backend/Frontend  
> 🏢 **Hệ sinh thái**: CRM Loyalty Gigamall - Sao Mai Solution  
> 🎯 **Mục tiêu**: Bóc tách toàn diện mã nguồn, luồng dữ liệu, nghiệp vụ loyalty và các nút thắt cổ chai của dự án cũ `OCRBillProcess`, từ đó định hình bản thiết kế kiến trúc chuẩn mực để chuyển đổi (rewrite) sang **.NET 8 LTS** hiện đại, kết hợp AI VLM tự động hóa và tích hợp **CheckPoint Automation Platform**.

---

## 1. TỔNG QUAN HIỆN TRẠNG DỰ ÁN CŨ (EXECUTIVE SUMMARY)

### 1.1. Mục đích ra đời & Vị trí trong hệ thống
`OCRBillProcess` là phân hệ điều hành nghiệp vụ (Operator Console) của hệ thống CRM Loyalty Gigamall, đóng vai trò cầu nối giữa:
- **Khách hàng (Mobile App)**: Chụp ảnh hóa đơn mua sắm tại các gian hàng trong TTTM Gigamall gửi lên hệ thống để tích điểm.
- **Kiểm toán viên / Nhân viên CSKH (Desktop Operator)**: Đối soát hình ảnh hóa đơn, kiểm tra tính hợp lệ, bóc tách thông tin mua sắm và ra quyết định **Phê duyệt (Approve)** hoặc **Từ chối (Reject)**.
- **Hệ thống Loyalty Core**: Tích lũy điểm thưởng (Reward Points), điểm xếp hạng (Rank Points), xét nâng hạng thẻ thành viên, cấp mã quay số trúng thưởng (Lottery/Lucky Draw) và gửi thông báo đẩy (Push Notification) qua Firebase.

### 1.2. Nền tảng công nghệ cũ (Legacy Technical Stack)
| Tiêu chí | Công nghệ hiện tại (Legacy) | Đánh giá hiện trạng |
| :--- | :--- | :--- |
| **Framework / Runtime** | .NET Framework 4.5.2 / 4.7.2 | Lạc hậu, không tối ưu bộ nhớ, không đa nền tảng, dừng hỗ trợ chính thức. |
| **Dạng ứng dụng** | Windows Forms (WinForms Desktop MDI) | Phải cài đặt trực tiếp trên từng máy trạm của nhân viên; khó cập nhật phiên bản mới. |
| **Truy cập CSDL** | EntityFramework 6.4 + CSLA DataPortal (`CRMBaseDataPortal`) | Code monolithic, phụ thuộc vào các chuỗi SQL ghép tay và lớp trung gian cũ. |
| **Cơ chế bóc tách OCR** | Google Cloud Vision API (`Google.Cloud.Vision.V1` v1.2.0) | Tốn chi phí API bản quyền ($1.5/1000 lượt); phụ thuộc mạng Internet quốc tế. |
| **Cơ chế phân tích bill** | Template tọa độ cứng (Line, LeftPosition, Length) | Cực kỳ mỏng manh: Hóa đơn bị nhăn, chụp nghiêng hoặc cửa hàng đổi máy in là sai lệch toàn bộ. |
| **Xử lý bất đồng bộ** | Chạy đồng bộ (Blocking) trên UI Thread | Khi mạng chập chờn hoặc tải ảnh lớn, giao diện WinForms bị treo (Not Responding). |
| **Lưu trữ hình ảnh** | Server nội bộ qua HTTP Web API và Windows Network Share | Tồn tại nhiều đường dẫn cứng (`D:\TEMP\`, `F:\TEMP\`). |

---

## 2. PHÂN TÍCH KIẾN TRÚC & PHÂN BỐ MÃ NGUỒN CŨ (AS-IS ARCHITECTURE)

Mã nguồn nghiệp vụ bóc tách và duyệt hóa đơn hiện đang bị **phân mảnh trên 3 phân hệ**:

```
CRM Profile Solution
├── OCRBillProcess/ (Dự án Desktop WinForms)
│   ├── frmLogin1.cs                # Đăng nhập hệ thống & khởi tạo Role
│   ├── frmMain.cs                  # MDI Container, kiểm tra quyền BoName
│   ├── frmOCRBillReview.cs         # Form chính đối soát, xem ảnh, OCR & Duyệt/Từ chối
│   ├── frmOCRBillConfigV2.cs       # Form cấu hình tọa độ template (Dấu hiệu & Chi tiết)
│   ├── frmGetSpecialGift.cs        # Vòng quay số may mắn trực tiếp cho sự kiện TTTM
│   └── frmZoomPic.cs               # Cửa sổ phóng to ảnh hóa đơn
│
├── CRMProfileManagement/OCRBillPROCESS/ (Thư viện nghiệp vụ cốt lõi)
│   ├── GigaBillProcess.cs          # [QUAN TRỌNG] Toàn bộ logic xét duyệt, tích điểm, tặng quà
│   ├── BillProcess.cs              # Quy tắc quét chương trình khuyến mãi & quay số
│   ├── OCR_Helper.cs               # Đóng gói gọi Google Cloud Vision API & xử lý ảnh
│   ├── InvoiceOCRTrans.cs          # Thuật toán tìm kiếm giá trị dựa trên Config
│   └── OCRInvoiceConfig.cs         # Cấu trúc dữ liệu cấu hình Template hóa đơn
│
└── CRMBase / CRMProfileShared/
    ├── BaseCustomer.cs             # Thông tin hội viên & kiểm tra trạng thái thẻ
    ├── BaseOutlet.cs / BaseBrand.cs# Danh mục cửa hàng / thương hiệu tại Gigamall
    └── CommonExt.cs                # Bắn thông báo đẩy Push Notification Firebase
```

---

## 3. LUỒNG DỮ LIỆU & CHU TRÌNH NGHIỆP VỤ CHI TIẾT (BUSINESS WORKFLOW)

### 3.1. Sơ đồ chu trình hóa đơn (End-to-End Invoice Lifecycle)

```
[Khách hàng chụp ảnh bill]
           │ (1. Upload qua Mobile App)
           ▼
[CSDL: Bảng OCRTrialTrans / CRMGiGaBill] (Trạng thái: Chưa duyệt, ReviewBy = NULL)
           │
           │ (2. Nhân viên CSKH chọn ngày & mở form frmOCRBillReview)
           ▼
[Tải ảnh hóa đơn từ Image Server về máy trạm] (Lưu đệm vào D:\TEMP\)
           │
           │ (3. Bấm [Nhận dạng OCR])
           ▼
[Gửi ảnh lên Google Cloud Vision API] ──► [Nhận văn bản thô (fullTextAnnotation)]
           │
           │ (4. Phân tích Dấu hiệu & Bóc tách)
           ├── Bước 4.1: Khớp Tên/Địa chỉ cửa hàng qua SignalConfig
           ├── Bước 4.2: Bóc tách Số hóa đơn, Ngày giờ, Tổng tiền qua InvoiceConfig
           └── Bước 4.3: Kiểm tra gian lận (IsFakeBill: đối chiếu địa chỉ in trên bill)
           │
           │ (5. Kiểm toán viên kiểm tra mắt & chỉnh sửa nếu OCR điền sai)
           ▼
┌──────────────────────────────┴──────────────────────────────┐
▼ [BẤM TỪ CHỐI - btnReject]                                   ▼ [BẤM PHÊ DUYỆT - btnApproved]
- Nhập lý do từ chối (txtReviewNotes)                          - Kiểm tra hội viên (Status == 'L')
- Cập nhật trạng thái 'Reject'                                 - Kiểm tra giá trị bất thường (> 99tr)
- Bắn Push Notification báo lý do                              - Tính điểm thưởng & điểm hạng
- Chuyển ảnh vào thư mục /OCR/Rejected/                        - Quét chương trình Quay số / Quà tặng
                                                               - Xét nâng hạng thẻ thành viên
                                                               - Bắn Push Notification cộng điểm/quà
                                                               - Chuyển ảnh vào thư mục /OCR/Approved/
```

### 3.2. Chi tiết logic Phê duyệt (`GigaBillProcess.BillReview`)
Khi nhân viên bấm **[Phê duyệt]**, chuỗi nghiệp vụ sau được kích hoạt tuần tự qua 5 bước nghiêm ngặt:
1. **BƯỚC 1: Kiểm tra trạng thái hội viên & tính hợp lệ (Validation)**: 
   - Trạng thái hội viên: `customer.Status == "L"` (Life / Đang hoạt động, không bị khóa 'S' - Suspend).
   - Kiểm tra trùng lặp: `IsBillNumberExists(OutletNumber, BillNumber)` tránh gian lận tích điểm trùng.
   - Kiểm tra số tiền: `BillTotalA > 0`.
2. **BƯỚC 2: Tính toán điểm Loyalty & Cập nhật đa sổ cái (Points Calculation & Multi-Ledger Update)**:
   - Tra cứu hạng thẻ hiện tại của hội viên từ sổ cái xếp hạng `mssMemClassLedger` (`Status = 'O'`). Nếu không tồn tại sẽ throw ngoại lệ `exp_MemclassLedger_can_not_find_memID`.
   - Tra cứu cặp tỷ lệ điểm `BasePointRate` từ `BaseMemberClass.GetPointRate`:
     - `RewardPointRate`: Tỷ lệ quy đổi điểm thưởng (VND/điểm) lấy từ cấu hình hạng `G_MemberClass.RewardRate` (mặc định 10.000đ/điểm nếu chưa có).
     - `RankingPointRate`: Tỷ lệ quy đổi điểm xếp hạng (VND/điểm) lấy từ cấu hình hạng `G_MemberClass.ReviewRate` (mặc định 10.000đ/điểm nếu chưa có).
     - Có kiểm tra chính sách sự kiện theo Gian hàng (Outlet Scope) > Toàn Gigamall (Non-scope) > Bình thường (Normal).
   - Công thức tính điểm kế toán:
     $$\text{getpoints (Reward Points)} = \text{Math.Round}\left(\frac{\text{BillTotalA}}{\text{RewardPointRate}}, \text{MidpointRounding.AwayFromZero}\right)$$
     $$\text{reviewpoints (Ranking Points)} = \text{Math.Round}\left(\frac{\text{BillTotalA}}{\text{RankingPointRate}}, \text{MidpointRounding.AwayFromZero}\right)$$
     *Lưu ý: Bắt buộc dùng `MidpointRounding.AwayFromZero` (nguyên tắc $\ge 0.5$ thành 1, ví dụ $0.6 \rightarrow 1$), tuyệt đối không làm tròn cắt cụt `Floor`.*
   - Cập nhật đồng bộ 3 sổ cái trong Transaction nguyên tử (`DataAddOCRSaleTrans`):
     - **Ví điểm hội viên (`mssCLMLedger`)**:
       - `AccSales += BillTotalA` (Cộng dồn doanh số chi tiêu)
       - `AccPoints += getpoints` (Cộng dồn điểm thưởng kiếm được)
       - `AccTC += 1` (Tăng số lần giao dịch)
       - `LastSaleDate = BillDateTimeA` (Ngày giờ phát sinh giao dịch)
       - *Cực kỳ quan trọng*: `RedeemPoints` là tổng điểm đã tiêu/đổi quà, **KHÔNG TĂNG** khi tích điểm bán hàng! Số dư điểm khả dụng: $\text{RewardBalance} = \text{AccPoints} - \text{RedeemPoints}$.
     - **Sổ cái xếp hạng (`mssMemClassLedger`)**:
       - Cập nhật bản ghi `Status = 'O'` đang mở của hội viên.
       - `DebitAmt += reviewpoints` (Cộng dồn điểm xếp hạng phát sinh tăng)
       - `CloseAmt = ISNULL(OpenAmt, 0) + DebitAmt - ISNULL(CreditAmt, 0)` (Chốt điểm xếp hạng lũy kế)
       - `DocType = 'S'` (Giao dịch bán hàng - Sale)
       - `ReviewFlag = 'N'` (Đánh dấu chưa chạy Upgrade để UpClass Engine tiến hành kiểm tra)
       - `LastSaleDocID = cardtrans.DocID`
     - **Sổ cái giao dịch thẻ (`mssCardTrans`)**:
       - Tạo bản ghi mới ghi nhận chi tiết: `DocID = Guid.NewGuid()`, `OrderID = bill.ID`, `SubTotal = BillTotalA`, `TotalBillValue = BillTotalA`, `RPointGain = getpoints`, `RPointRate = RewardPointRate`, `ReviewPointGain = reviewpoints`, `ReviewPointRate = RankingPointRate`, `TransType = 1`, `UseMobileApp = true`, `BProcess = 'Y'`.
3. **BƯỚC 3: Tự động kiểm tra Thăng hạng thành viên (UpClass Engine)**:
   - Đọc điểm xếp hạng lũy kế (`CloseAmt` trên `mssMemClassLedger` hoặc `AccPoints` trên `mssCLMLedger`).
   - Đối chiếu dải quy định thăng hạng trong `G_MemRangeClass`.
   - Nếu đạt ngưỡng thăng hạng: Đóng bản ghi cũ (`Status = 'C'`), mở bản ghi mới (`Status = 'O'`, `DocType = 'R'`, `ReviewFlag = 'Y'`), tặng điểm thưởng thăng hạng vào `AccPoints` và bảng `mssCLMGiftPoint`.
4. **BƯỚC 4: Đánh giá & Phân phối vé quay số may mắn (Lottery) và Quà tặng khuyến mãi (Promotion Engine)**:
   - Đánh giá điều kiện vé số may mắn `ELotteryProgram.Lottery`: Kiểm tra khung giờ hiệu lực, giá trị bill tối thiểu `MinBillValue`, ngày gia nhập `DateJoined`, phạm vi gian hàng `ScopeID` (`G_Scope.OutletList`), và hạn ngạch 3 tầng (`mssLotteryGift`). Nếu đạt, bốc thăm ngẫu nhiên có trọng số và phát vé `LotteryCode`.
   - Đánh giá điều kiện quà tặng khuyến mãi: Ưu tiên `ELotteryProgram.FirstTrans` (nếu là giao dịch đầu tiên) hoặc fallback sang `ELotteryProgram.Gift`. Kiểm tra hạn ngạch 3 tầng trên `mssPromotionGift`, bốc thăm phát quà/voucher `PromotionCode`.
5. **BƯỚC 5: Phản hồi tức thì qua Thông báo đẩy (Firebase Push Notification)**:
   - Gửi thông báo tích điểm `GetPointBySale`: Báo số điểm thưởng `getpoints` vừa được cộng.
   - Nếu có nâng hạng: Gửi thêm thông báo chúc mừng thăng hạng `MemberUpClass`.
   - Nếu được cấp vé quay số: Gửi thông báo `SendLottery` hiển thị mã vé dự thưởng.
   - Nếu được cấp quà khuyến mãi: Gửi thông báo `SendPromotion` hiển thị mã voucher ưu đãi.
6. **BƯỚC 6: Cập nhật trạng thái hóa đơn & Lưu trữ (Final State Persistence)**:
   - Cập nhật `mssOCRTrialTrans`: `PointGain = getpoints`, `Approved = 1`, `Reviewed = 1`, `ReviewDate = GETDATE()`, `DataStatus = 'Approved'`.
   - Di chuyển ảnh sang thư mục chính thức `/OCR/Approved/`.


### 3.3. CHI TIẾT CƠ CHẾ QUAY SỐ MAY MẮN (LOTTERY) & QUÀ TẶNG KHUYẾN MÃI (PROMOTION/GIFT) TRONG `btnApproved_Click`

Trong hàm duyệt hóa đơn của mã nguồn cũ (`btnApproved_Click` tại `frmOCRBillReview.cs` dòng 780 – 1110 kết hợp `GigaBillProcess.cs`), ngoài chuỗi tính điểm Loyalty và thăng hạng thành viên, hệ thống tích hợp một **Động cơ phân phối Vé số may mắn (Lottery Engine)** và **Quà tặng khuyến mãi (Promotion/Gift Engine)** hoàn toàn tự động.

Dưới đây là đặc tả chi tiết toàn bộ cơ chế, cấu trúc dữ liệu và thuật toán phân bổ:

```
┌─────────────────────────────────────────────────────────────────────────────────────────────┐
│ QUY TRÌNH ĐÁNH GIÁ VÉ SỐ MAY MẮN (LOTTERY) & QUÀ TẶNG KHUYẾN MÃI (PROMOTION)                 │
└──────────────────────────────────────────────┬──────────────────────────────────────────────┘
                                               │
                      ┌────────────────────────┴────────────────────────┐
                      ▼                                                 ▼
        [1. KHỐI VÉ SỐ MAY MẮN - LOTTERY]               [2. KHỐI QUÀ TẶNG KHUYẾN MÃI - PROMOTION]
                      │                                                 │
    - Loại chương trình: Type = 'Lottery'             - Ưu tiên 1: Type = 'FirstTrans' (Giao dịch đầu)
    - BillTotalA >= MinBillValue                      - Dự phòng 2: Type = 'Gift' (Quà tặng thông thường)
    - FromDate <= TransDate <= ToDate (+ExtendDays)   - Kiểm tra lịch sử giao dịch mssPromotionGift
    - JoinFrom <= DateJoined <= JoinTo (Ngày vào CLB)   (Nếu MemID đã có quà FirstTrans -> chuyển sang Gift)
                      │                                                 │
                      ├────────────────────────┬────────────────────────┤
                      │                        │                        │
                      ▼                        ▼                        ▼
         [3. KIỂM TRA PHẠM VI ÁP DỤNG CỬA HÀNG / GIAN HÀNG (SCOPE VALIDATION - G_Scope)]
         - Nếu Program có ScopeID: Tra cứu danh sách Outlet áp dụng (G_Scope.OutletList)
         - Phân tách chuỗi danh sách bởi dấu chấm phẩy ';' (OutletList.Split(';'))
         - Bắt buộc bill.OutletNumberA phải nằm trong danh sách được phép
                      │
                      ▼
         [4. BỘ KIỂM SOÁT HẠN NGẠCH 3 TẦNG (3-TIER QUOTA ENFORCEMENT)]
         ┌─────────────────────────────────────────────────────────────────────────────┐
         │ • Tầng 1: Hạn ngạch toàn chiến dịch / 1 hội viên (MaxLotteryMemberCount)    │
         │   COUNT(Gift) WHERE MemID = @MemID AND ProgramID = @ProgramID < Quota       │
         │ • Tầng 2: Hạn ngạch trong 1 ngày / 1 hội viên (MaxLotteryMemberCountByDay) │
         │   COUNT(Gift) WHERE MemID = @MemID AND ProgramID = @ProgramID               │
         │                     AND TransDate = @TransDate < Quota                      │
         │ • Tầng 3: Hạn ngạch toàn chương trình trong 1 ngày (MaxLotteryCountByDay)  │
         │   COUNT(Gift) WHERE TransDate = @TransDate < Quota                          │
         └─────────────────────────────────────┬───────────────────────────────────────┘
                                               │
                                               ▼ Đạt đủ điều kiện & Còn hạn ngạch
         [5. THUẬT TOÁN BỐC THĂM & PHÂN BỔ MÃ (WEIGHTED RANDOM ALLOCATION)]
         - Đọc danh sách quà chưa phát: MemID IS NULL AND IsEnable = 1
         - Phân bổ trọng số ngẫu nhiên theo giá trị phần quà (GiftValue) trong tập mẫu 1000 slot
         - Trường hợp quà là Điểm (RankPoint/RewardPoint): Tự sinh mã 'P' + yyyyMMddHHmmss
         - Chọn ngẫu nhiên: Index = Random.Next(rdList.Count)
                      │
                      ▼
         [6. CẬP NHẬT DỮ LIỆU & BẮN THÔNG BÁO ĐẨY (PERSISTENCE & PUSH NOTIFICATION)]
         - Cập nhật bản ghi mssLotteryGift / mssPromotionGift:
           Gán MemID, BillNo, TransDate, SubTotal, GetCodeDate = GETDATE(), ToDate
         - Nếu là quà Point: Cộng trực tiếp vào mssMemClassLedger / mssCLMLedger & Xét thăng hạng
         - Nếu là quà GrabCode: Tự động khởi tạo Offer (OfferDetailUI, GRedeemTranUI)
         - Bắn Firebase Push Notification: Sự kiện SendLottery / SendPromotion tới Mobile App
```

---

#### 3.3.1. Phân biệt 2 loại hình chiến dịch: Lottery vs Promotion
Hệ thống quản lý 2 nhóm chương trình độc lập nhưng cùng chia sẻ bảng danh mục `mssLotteryProgram`:

| Tiêu chí | Vé số may mắn (Lottery) | Quà tặng khuyến mãi (Promotion / Gift) |
| :--- | :--- | :--- |
| **Bảng lưu trữ vé/quà** | `mssLotteryGift` | `mssPromotionGift` |
| **Enum loại hình** | `ELotteryProgram.Lottery` | `ELotteryProgram.FirstTrans`, `ELotteryProgram.Gift` |
| **Mục đích nghiệp vụ** | Phát mã quay số trúng thưởng định kỳ (Lucky Draw). | Tặng voucher/quà điện tử tức thì sau khi mua sắm. |
| **Logic ưu tiên** | Tìm chương trình `Lottery` thỏa mãn hạn mức hóa đơn và thời gian. | **Ưu tiên quét `FirstTrans`** (quà giao dịch đầu tiên). Nếu hội viên đã từng nhận quà thì tự động fallback sang `Gift`. |
| **Thời hạn vé/quà** | Hết hạn theo `ToDate` của chương trình + `ExtendExpiredDay`. | Tính từ ngày nhận + `TakeExpiredDay` (mặc định 30 ngày nếu null). |
| **Sự kiện Push Noti** | `ENotificationEvent.SendLottery` | `ENotificationEvent.SendPromotion` |

---

#### 3.3.2. Quy tắc thẩm định điều kiện (Eligibility Evaluation Rules)
Để một hóa đơn được phát hành vé quay số hoặc quà tặng, phải vượt qua toàn bộ 4 lớp lọc nghiêm ngặt:

1. **Hiệu lực thời gian (Time Window Validity)**:
   - Đối với Lottery: Ngày giao dịch hóa đơn (`TransDate`) phải thỏa:
     $$\text{FromDate} \le \text{TransDate} \le \text{ToDate} + \text{ExtendExpiredDay}$$
   - Đối với Promotion: $\text{FromDate} \le \text{TransDate} \le \text{ToDate}$.
2. **Ngưỡng chi tiêu tối thiểu (Min Bill Value)**:
   $$\text{BillTotalA} \ge \text{MinBillValue}$$
   Hóa đơn có giá trị lớn hơn hoặc bằng mức quy định của chương trình mới được cấp vé/quà.
3. **Phạm vi ngày gia nhập hội viên (Member Join Date Window)**:
   Áp dụng riêng cho Lottery:
   $$(\text{JoinFrom} = \text{null} \lor \text{JoinFrom} \le \text{customer.DateJoined}) \land (\text{JoinTo} = \text{null} \lor \text{JoinTo} \ge \text{customer.DateJoined})$$
   Cho phép ban tổ chức chỉ phát vé cho các hội viên mới gia nhập trong đợt chiến dịch hoặc hội viên thân thiết lâu năm.
4. **Phạm vi Gian hàng áp dụng (Outlet Scope Enforcement)**:
   - Nếu chương trình có gán mã phạm vi `ScopeID` (liên kết bảng `G_Scope`):
   - Đọc danh sách gian hàng hợp lệ `G_Scope.OutletList` (các mã gian hàng được ngăn cách bằng dấu `;`, ví dụ `"F01;F02;R05;S12"`).
   - Kiểm tra mã gian hàng trên hóa đơn: `scope.OutletList.Split(';').Any(x => x.Contains(bill.OutletNumberA))`. Nếu không thuộc danh sách, hủy quyền nhận vé/quà ngay lập tức (`isValidOutlet = false`).

---

#### 3.3.3. Cơ chế kiểm soát hạn ngạch 3 tầng (3-Tier Quota Engine)
Nhằm chống gian lận và kiểm soát chi phí ngân sách quà tặng, hệ thống áp dụng cơ chế đếm số lượng vé/quà đã phát:

1. **Tầng 1 - Hạn ngạch toàn chiến dịch cho 1 hội viên (`MaxLotteryMemberCount`)**:
   ```sql
   SELECT COUNT(1) FROM dbo.mssLotteryGift (hoặc mssPromotionGift)
   WHERE MemID = @MemID AND ProgramID = @ProgramID
   -- Điều kiện hợp lệ: Count < MaxLotteryMemberCount
   ```
2. **Tầng 2 - Hạn ngạch trong 1 ngày cho 1 hội viên (`MaxLotteryMemberCountByDay`)**:
   ```sql
   SELECT COUNT(1) FROM dbo.mssLotteryGift (hoặc mssPromotionGift)
   WHERE MemID = @MemID AND ProgramID = @ProgramID AND TransDate = @TransDate
   -- Điều kiện hợp lệ: Count < MaxLotteryMemberCountByDay
   ```
3. **Tầng 3 - Hạn ngạch toàn chương trình trong 1 ngày (`MaxLotteryCountByDay`)**:
   ```sql
   SELECT COUNT(1) FROM dbo.mssLotteryGift (hoặc mssPromotionGift)
   WHERE TransDate = @TransDate
   -- Điều kiện hợp lệ: Count < MaxLotteryCountByDay
   ```
*Chỉ khi thỏa mãn đồng thời cả 3 tầng hạn ngạch trên, hệ thống mới tiến hành bốc thăm phát mã.*

---

#### 3.3.4. Thuật toán bốc thăm ngẫu nhiên có trọng số (Weighted Random Distribution)
Hệ thống không chỉ lấy ngẫu nhiên đơn thuần, mà áp dụng cơ chế xác suất theo giá trị phần quà (`GiftValue`):

1. **Truy vấn kho mã khả dụng**:
   ```sql
   SELECT * FROM dbo.mssLotteryGift (hoặc mssPromotionGift)
   WHERE ProgramID = @ProgramID 
     AND MemID IS NULL 
     AND IsEnable = 1 
     AND (FromDate IS NULL OR FromDate <= @NowTo)
     AND (ToDate IS NULL OR ToDate >= @NowFrom)
   ```
2. **Tính toán trọng số bốc thăm**:
   - Tìm giá trị quà lớn nhất trong kho: $\text{giftValueMax} = \max(\text{GiftValue})$.
   - Với mỗi phần quà có giá trị $\text{GiftValue} > 0$:
     $$\text{slotCount} = 1000 - \left(\frac{\text{GiftValue} \times 1000}{\text{giftValueMax}}\right)$$
   - Phần quà có giá trị càng lớn (ví dụ: Tivi, Xe máy) thì số slot xuất hiện trong danh sách bốc thăm càng ít $\rightarrow$ Tỷ lệ trúng càng thấp.
   - Phần quà giá trị nhỏ (voucher 10k, móc khóa) có số slot xuất hiện nhiều $\rightarrow$ Tỷ lệ trúng cao.
3. **Phần quà Điểm thưởng đặc biệt (`ItemCode = 'Point'`)**:
   - Nếu chương trình cấu hình `RankPoint > 0` hoặc `RewardPoint > 0`, hệ thống tự sinh mã quà dạng `P + yyyyMMddHHmmss` với 1000 slot vào danh sách bốc thăm.
   - Nếu trúng quà Point: Hệ thống cập nhật thẳng vào sổ cái điểm `mssMemClassLedger` và `mssCLMLedger`, kích hoạt kiểm tra nâng hạng `UpgradeClass(memid)` và gửi thông báo `PushAddPointNotification`.
4. **Phần quà điện tử liên kết (`ItemType = 'GrabCode'`)**:
   - Tự động tạo bản ghi ưu đãi `OfferDetailUI` và giao dịch quy đổi `GRedeemTranUI` để hội viên nhận mã qua ứng dụng Grab/đối tác.

---

#### 3.3.5. Bảng tham chiếu dữ liệu (Database Schema Dictionary)

| Bảng dữ liệu | Khóa chính | Ý nghĩa & Các cột quan trọng |
| :--- | :--- | :--- |
| **`mssLotteryProgram`** | `ID` (int) | Danh mục chương trình: `Type` (Lottery/FirstTrans/Gift), `Name`, `FromDate`, `ToDate`, `ExtendExpiredDay`, `MinBillValue`, `ScopeID`, `MaxLotteryMemberCount`, `MaxLotteryMemberCountByDay`, `MaxLotteryCountByDay`, `RankPoint`, `RewardPoint`, `TakeExpiredDay`. |
| **`mssLotteryGift`** | `ID` (uniqueidentifier) | Kho vé số may mắn: `ProgramID`, `LotteryCode`, `Name`, `GiftValue`, `MemID` (null nếu chưa phát), `BillNo`, `TransDate`, `SubTotal`, `GetCodeDate`, `IsEnable`. |
| **`mssPromotionGift`** | `ID` (uniqueidentifier) | Kho quà tặng/voucher khuyến mãi: `ProgramID`, `PromotionCode`, `Name`, `ItemType`, `GiftValue`, `MemID` (null nếu chưa phát), `BillNo`, `TransDate`, `SubTotal`, `GetCodeDate`, `ToDate`, `IsEnable`. |
| **`G_Scope`** | `ID` (int) | Cấu hình phạm vi gian hàng: `Number`, `Name`, `OutletList` (danh sách mã gian hàng phân tách bằng dấu chấm phẩy, ví dụ `OUT01;OUT02;OUT05`). |

---

## 4. CÁC NÚT THẮT CỔ CHAI & NHƯỢC ĐIỂM CHÍ MẠNG CẦN KHẮC PHỤC

1. **Phụ thuộc 100% vào duyệt thủ công (Human Bottleneck)**:
   - 100% hóa đơn do khách hàng chụp gửi lên đều phải chờ nhân viên mở máy tính, click xem ảnh và bấm duyệt.
   - Giờ cao điểm (cuối tuần, lễ tết), hàng ngàn hóa đơn bị dồn ứ hàng giờ, khiến khách hàng sốt ruột và khiếu nại.
2. **Chi phí Google Vision API đắt đỏ**:
   - Mỗi hóa đơn đều gửi request lên Google Vision, phát sinh chi phí hàng tháng lớn mà không tận dụng lại kết quả cache.
3. **Cơ chế cấu hình Template tọa độ lỗi thời**:
   - Màn hình `frmOCRBillConfigV2` đòi hỏi nhân viên phải đo từng dòng, từng ký tự. Nhưng thực tế:
     - Cửa hàng thay cuộn giấy in hoặc đổi format POS $\rightarrow$ Template hỏng.
     - Khách hàng chụp ảnh bị nghiêng, xoay ngang, dính bóng mờ $\rightarrow$ Tọa độ sai lệch.
4. **Trải nghiệm Desktop WinForms phân mảnh**:
   - Nhân viên chỉ duyệt được khi ngồi tại máy trạm đã cài app WinForms và kết nối VPN vào hệ thống. Không thể duyệt từ xa hoặc trên thiết bị di động/máy tính bảng.
5. **Rủi ro sập ứng dụng và mất kết nối**:
   - Gọi mạng đồng bộ trên giao diện, không có cơ chế Retry (Polly), không có cơ chế Background Queue để tự động phục hồi khi rớt mạng.

---

## 5. ĐẶC TẢ KIẾN TRÚC MỚI TRÊN NỀN TẢNG .NET 8 (TARGET ARCHITECTURE)

Nhằm giải quyết triệt để các nhược điểm trên, hệ thống mới sẽ được xây dựng trên nền tảng **.NET 8 LTS**, tách biệt hoàn toàn giữa Backend Service, AI Worker và Giao diện Web.

### 5.1. Sơ đồ Kiến trúc Đích (Target .NET 8 Architecture)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ 1. NGƯỜI DÙNG & THIẾT BỊ ĐẦU CUỐI                                          │
│   - Khách hàng: Gigamall Mobile App (iOS / Android)                         │
│   - Kiểm toán viên: Web Portal (Modern Web Browser: Chrome / Edge)          │
└──────────────────────────────────────┬──────────────────────────────────────┘
                                       │
                                       ▼ HTTP REST / HTTPS
┌─────────────────────────────────────────────────────────────────────────────┐
│ 2. API GATEWAY & BACKEND CORE (.NET 8 WEB API)                              │
│   - Dịch vụ: CRM.OCR.Api                                                    │
│   - Authentication: JWT Bearer + API Key (CheckPoint Integration)           │
│   - Endpoints:                                                              │
│       • POST /api/bills/upload          (Khách hàng upload bill)            │
│       • GET  /api/bills/pending         (Lấy danh sách chờ duyệt)           │
│       • POST /api/bills/{id}/approve    (Kiểm toán viên duyệt)              │
│       • POST /api/bills/{id}/reject     (Kiểm toán viên từ chối)            │
│       • GET  /api/bills/{id}/image      (Lấy ảnh hóa đơn tối ưu)            │
└──────────────────────────────────────┬──────────────────────────────────────┘
                                       │
                   ┌───────────────────┴───────────────────┐
                   ▼                                       ▼
┌──────────────────────────────────────┐ ┌────────────────────────────────────┐
│ 3. HÀNG ĐỢI XỬ LÝ (BACKGROUND QUEUE)  │ │ 4. ĐIỀU PHỐI CHECKPOINT PLATFORM   │
│   - Channel / RabbitMQ / Hangfire    │ │   - Tự động hóa lịch quét nền 24/7 │
│   - Đẩy bill vào hàng đợi bóc tách   │ │   - Quản lý Retry & Dead-letter    │
└──────────────────┬───────────────────┘ └─────────────────┬──────────────────┘
                   │                                       │
                   ▼                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ 5. AI OCR & AUTO-APPROVAL WORKER (.NET 8 BACKGROUND WORKER)                 │
│   - Dịch vụ: CRM.OCR.Worker                                                 │
│                                                                             │
│   ┌─────────────────────────────────┐   ┌─────────────────────────────────┐ │
│   │ Sub-engine 1: AI Connector      │   │ Sub-engine 2: Auto-Approval     │ │
│   │ - Chiến lược Đa nhà cung cấp    │   │ - Bộ quy tắc duyệt tự động:     │ │
│   │   • Local VLM (Qwen2-VL)        │   │   • Bill hợp lệ (Valid = true)  │ │
│   │   • Cloud Fallback (Azure/Google│   │   • Giá trị <= Ngưỡng an toàn   │ │
│   │ - Polly Resilience: Retry 3 lần │   │   • Khớp đúng mã Outlet Active  │ │
│   │ - Timeout: 20 giây              │   │   • Không trùng lặp Bill Number │ │
│   └─────────────────────────────────┘   └─────────────────────────────────┘ │
│                                                          │                  │
│                                  ┌───────────────────────┴──────────────┐   │
│                                  ▼ Thỏa mãn điều kiện                   ▼   │
│                         [TỰ ĐỘNG PHÊ DUYỆT]                [CHUYỂN DUYỆT TAY│
│                         - Tích điểm tức thì 3s             - Đẩy sang Web   │
│                         - Bắn Push Notification              cho kiểm toán  │
│                         (Đạt tỷ lệ 70% - 80%)                viên đối soát  │
└──────────────────────────────────────┬──────────────────────────────────────┘
                                       │
                                       ▼ Entity Framework Core 8 / Dapper
┌─────────────────────────────────────────────────────────────────────────────┐
│ 6. CƠ SỞ DỮ LIỆU & LƯU TRỮ TẬP TRUNG                                        │
│   - SQL Server (CLMHQ4GIGA / CLMHQTEST): Bảng hóa đơn, ví điểm, hội viên    │
│   - S3-Compatible / MinIO Object Storage: Lưu trữ ảnh hóa đơn an toàn       │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 6. THIẾT KẾ MÔ ĐUN & HỢP NHẤT TOÀN DIỆN TRONG SOLUTION `CRMMall.sln` (.NET 8)

Để đảm bảo khả năng mở rộng, bảo trì và **tái sử dụng tối đa logic nghiệp vụ** giữa các dự án trong hệ sinh thái CRM (Duyệt hóa đơn OCR, POS trực tiếp, Bán hàng E-Commerce, Mini-app Mobile, Tự động hóa CheckPoint Platform), toàn bộ các dự án đã được **hợp nhất vào một Solution duy nhất `CRMMall.sln`** (xóa bỏ giải pháp độc lập cũ `CRMAutomationSln.sln`).

Trong đó, toàn bộ các khối logic nghiệp vụ trong hàm phê duyệt hóa đơn đã được bóc tách thành **các Domain Services độc lập trong BLL**, cho phép cả kiểm toán viên duyệt thủ công trên Web GUI và Background Worker tự động hóa ngầm cùng gọi chung một bộ quy tắc duy nhất (Single Source of Truth):

```
D:\GIT PROJECT\GIGAMallMigration\
├── CRMMall.sln                              # Solution tổng thể duy nhất hợp nhất toàn bộ hệ thống
│
├── src/
│   ├── CRMMall.BillReview.DAL/              # [1] DATA ACCESS LAYER (Dapper + SQL Server)
│   │   ├── Data/
│   │   │   ├── IDbConnectionFactory.cs      # Factory kết nối đa môi trường (CLMHQTEST / CLMHQ4GIGA)
│   │   │   └── DbConnectionFactory.cs
│   │   ├── Entities/
│   │   │   ├── OcrTrialTrans.cs             # Hóa đơn bóc tách (mssOCRTrialTrans)
│   │   │   ├── CustomerInfo.cs              # Khách hàng & trạng thái 'N'/'A'/'L' (mssCustomer)
│   │   │   └── LoyaltyEntities.cs           # Sổ cái & danh mục (mssCLMLedger, mssMemClassLedger,
│   │   │                                    # mssCardTrans, mssLotteryGift, mssPromotionGift, G_Scope)
│   │   └── Repositories/
│   │       ├── IBillRepository.cs           # Truy vấn hóa đơn, chống trùng lặp, cập nhật trạng thái
│   │       ├── ICustomerRepository.cs       # Đọc trạng thái hội viên, tra cứu CardNo, FCM Token
│   │       └── ILoyaltyRepository.cs        # Thực thi đa sổ cái nguyên tử, tra cứu hạn ngạch & kho vé/quà
│   │
│   ├── CRMMall.BillReview.BLL/              # [2] BUSINESS LOGIC LAYER (Reusable Domain Services)
│   │   ├── DTOs/
│   │   │   ├── BillDtos.cs                  # DTOs yêu cầu phê duyệt, kết quả duyệt, webhook OCR
│   │   │   └── LoyaltyDtos.cs               # DTOs tích điểm, kết quả thăng hạng, vé quay số, quà tặng
│   │   ├── Policies/
│   │   │   ├── IAutoApprovalPolicy.cs       # Bộ quy tắc thẩm định 5 điều kiện duyệt tự động
│   │   │   └── AutoApprovalPolicy.cs
│   │   └── Services/                        # CÁC THƯ VIỆN LOGIC NGHIỆP VỤ ĐỘC LẬP TÁI SỬ DỤNG:
│   │       ├── ILoyaltyPointsService.cs     # 1. TÍCH ĐIỂM: Tính toán điểm chuẩn kế toán & ghi 3 sổ cái
│   │       │   └── LoyaltyPointsService.cs  #    (Dùng lại cho: Duyệt bill, POS trực tiếp, Đơn online)
│   │       ├── IMembershipTierService.cs    # 2. THĂNG HẠNG: Thẩm định điểm tích lũy & tự động nâng hạng
│   │       │   └── MembershipTierService.cs #    (Dùng lại cho: Duyệt bill, Batch rà soát định kỳ cuối tháng)
│   │       ├── ILotteryPromotionService.cs  # 3. QUAY SỐ & QUÀ: Đánh giá điều kiện, hạn ngạch 3 tầng & phát vé/quà
│   │       │   └── LotteryPromotionService.cs #    (Dùng lại cho: Duyệt bill, Vòng quay may mắn App, CSKH tặng quà)
│   │       ├── INotificationService.cs      # 4. THÔNG BÁO ĐẨY: Bắn Firebase Push Notification đa sự kiện
│   │       │   └── NotificationService.cs   #    (GetPointBySale, MemberUpClass, SendLottery, SendPromotion)
│   │       ├── IBillReviewService.cs        # 5. ORCHESTRATOR: Điều phối quy trình duyệt hóa đơn 6 bước
│   │       └── BillReviewService.cs         #    (Tích hợp 4 dịch vụ trên vào luồng xử lý hoàn chỉnh)
│   │
│   ├── CRMMall.BillReview.GUI/              # [3] PRESENTATION LAYER (Web API + Web Management Console)
│   │   ├── Controllers/
│   │   │   ├── BillsController.cs           # REST API: Danh sách chờ duyệt, Duyệt, Từ chối, Webhook, Auto-review
│   │   │   └── HealthController.cs          # Health check endpoint
│   │   ├── wwwroot/                         # Web Portal kiểm toán viên (Thay thế 100% WinForms cũ)
│   │   └── Program.cs                       # Đăng ký Dependency Injection chuẩn Clean Architecture
│   │
│   ├── CRM.Automation.Core/                 # [4] AUTOMATION CORE (Domain Entities, DTOs, Task Contracts)
│   │   ├── Enums/                           # Task Types, Trigger Types
│   │   ├── Interfaces/                      # Contract các tác vụ CheckPoint (IBurnPointReminderService,...)
│   │   ├── Models/                          # DTOs request/response của điều phối CheckPoint
│   │   └── Settings/                        # Cấu hình ngưỡng chạy tự động
│   │
│   ├── CRM.Automation.Infrastructure/       # [5] AUTOMATION INFRASTRUCTURE (Tham chiếu BLL/DAL)
│   │   ├── Extensions/                      # ServiceCollectionExtensions (Nạp BLL/DAL Services vào DI)
│   │   ├── Repositories/                    # Triển khai truy vấn tác vụ lập lịch CheckPoint
│   │   └── Services/                        # Worker thực thi: Nhắc điểm hết hạn, Reset điểm, UpClass định kỳ
│   │
│   └── CRM.Automation.Api/                  # [6] AUTOMATION API & BACKGROUND WORKER
│       ├── Controllers/                     # Endpoint kích hoạt từ CheckPoint Platform (ApiKey Filter)
│       ├── Middlewares/                     # ApiKey Middleware, Global Exception Handler
│       └── Program.cs                       # Khởi động dịch vụ tự động hóa
│
├── tests/
│   └── CRM.Automation.Tests/                # [7] KIỂM THỬ TỰ ĐỘNG (xUnit Test Suite)
│       └── UnitTests/                       # Kiểm thử AutoApprovalPolicy, Quota Rules, UpClass (7/7 Passed)
│
└── database/
    └── CLMHQTEST_POC_Setup.sql              # Script khởi tạo toàn bộ schema & dữ liệu mẫu phục vụ POC
```

---

## 7. BỘ QUY TẮC DUYỆT TỰ ĐỘNG HÓA ĐƠN (AUTO-APPROVAL RULES ENGINE)

Để chuyển đổi từ **100% duyệt tay** sang **70% - 80% duyệt tự động**, Worker sẽ thẩm định từng hóa đơn qua 5 cổng kiểm soát nghiêm ngặt:

```csharp
public class AutoApprovalPolicy : IAutoApprovalPolicy
{
    public AutoApprovalDecision Evaluate(OcrBillTransaction bill, OcrBillExtractResult aiResult)
    {
        // Điều kiện 1: AI xác nhận đây là ảnh hóa đơn hợp lệ (không phải ảnh rác, mờ câm)
        if (!aiResult.IsValidBill || aiResult.ConfidenceScore < 0.85f)
            return AutoApprovalDecision.RejectToManual("Độ tin cậy nhận diện thấp hoặc ảnh không rõ ràng");

        // Điều kiện 2: Nhận diện chính xác gian hàng đang hoạt động tại Gigamall
        if (string.IsNullOrEmpty(aiResult.MatchedOutletNumber) || !aiResult.IsOutletActive)
            return AutoApprovalDecision.RejectToManual("Không khớp được gian hàng hợp lệ tại Gigamall");

        // Điều kiện 3: Giá trị hóa đơn nằm trong ngưỡng an toàn tự duyệt (ví dụ <= 500,000đ ở Pha 1)
        if (aiResult.BillTotal <= 0 || aiResult.BillTotal > MaxAutoApproveAmount)
            return AutoApprovalDecision.RejectToManual($"Giá trị hóa đơn ({aiResult.BillTotal:N0}đ) vượt ngưỡng tự duyệt");

        // Điều kiện 4: Hóa đơn còn trong thời hạn hợp lệ (ví dụ phát hành trong vòng 48 giờ)
        if (aiResult.BillDateTime < DateTime.Now.AddHours(-48) || aiResult.BillDateTime > DateTime.Now.AddHours(2))
            return AutoApprovalDecision.RejectToManual("Thời gian trên hóa đơn không nằm trong khoảng cho phép (48h)");

        // Điều kiện 5: Chống gian lận - Hóa đơn chưa từng được tích điểm trước đó
        if (IsDuplicateBill(aiResult.MatchedOutletNumber, aiResult.BillNumber))
            return AutoApprovalDecision.RejectToManual("Số hóa đơn này đã tồn tại trong hệ thống (Nghi vấn trùng lặp)");

        // Thỏa mãn 100% điều kiện -> Cho phép Tự động Phê duyệt
        return AutoApprovalDecision.ApproveAutomatically();
    }
}
```

---

## 8. LỘ TRÌNH TRIỂN KHAI VIẾT LẠI (EXECUTION ROADMAP)

Quá trình chuyển đổi được chia thành **4 giai đoạn khoa học**, đảm bảo không làm gián đoạn vận hành của TTTM:

```
[GIAI ĐOẠN 1: TÁCH LÕI & CHUẨN HÓA .NET 8]
├── Xây dựng Core & Infrastructure trên .NET 8
├── Chuyển đổi toàn bộ logic tính điểm, nâng hạng, quà tặng từ GigaBillProcess sang Clean Services
└── Đóng gói IOcrService với 2 Engine: Local VLM & Google Vision Fallback
       │
       ▼
[GIAI ĐOẠN 2: TRIỂN KHAI AUTO-APPROVAL WORKER (CHẾ ĐỘ CHẠY THỬ / SHADOW RUN)]
├── Cài đặt Auto-Approval Worker chạy song song
├── Chưa tự duyệt thật: Worker ghi log đánh giá kết quả so với quyết định của kiểm toán viên
└── Tinh chỉnh ngưỡng an toàn cho đến khi tỷ lệ chính xác đạt > 99.5%
       │
       ▼
[GIAI ĐOẠN 3: BẬT TỰ DUYỆT THẬT & WEB PORTAL]
├── Kích hoạt tự duyệt chính thức cho các hóa đơn <= 500.000đ (Chiếm ~60% lượng bill)
├── Đưa Web Portal vào hoạt động cho kiểm toán viên duyệt các bill còn lại
└── Giảm thời gian tích điểm của khách hàng xuống còn 3 - 5 giây
       │
       ▼
[GIAI ĐOẠN 4: KHAI TỬ HOÀN TOÀN WINFORMS & GOOGLE VISION]
├── Nâng ngưỡng tự duyệt lên <= 2.000.000đ (Đạt tỷ lệ tự động 80%)
├── Tắt hoàn toàn việc gọi Google Cloud Vision (Chi phí API về 0đ)
└── Ngừng sử dụng ứng dụng Desktop WinForms cũ
```

---

## 9. KẾT LUẬN & HIỆN TRẠNG TRIỂN KHAI THỰC TẾ

Dự án chuyển đổi `OCRBillProcess` và hệ thống tự động hóa CRM Gigamall sang **.NET 8 LTS** đã hoàn thành các mốc kỹ thuật then chốt:

### 9.1. Các hạng mục đã hoàn thành (Achieved Milestones)
1. **Hợp nhất Kiến trúc Đột phá (`CRMMall.sln`)**:
   - Gộp thành công 7 dự án (3 dự án Duyệt bill + 4 dự án Automation & Tests) vào một giải pháp duy nhất.
   - Loại bỏ hoàn toàn solution phân mảnh cũ `CRMAutomationSln.sln`.
   - Kết nối liên thông: `CRM.Automation.Infrastructure` dùng chung trực tiếp `CRMMall.BillReview.BLL` và `CRMMall.BillReview.DAL`.
2. **Module hóa BLL thành công (Single Source of Truth)**:
   - Tách rời triệt để các khối code monolithic cũ thành 5 dịch vụ độc lập:
     - `ILoyaltyPointsService`: Tích lũy điểm theo cấu hình tỷ lệ `mssCLMCardTransRule` và cập nhật đa sổ cái.
     - `IMembershipTierService`: Tự động xét duyệt và thăng hạng thẻ (`UpClass`), đóng thẻ 'C', mở thẻ mới 'O', thưởng điểm lên hạng.
     - `ILotteryPromotionService`: Quản lý 2 nhánh Lucky Draw & Promotion Gift với bộ kiểm soát hạn ngạch 3 tầng và bốc thăm trọng số.
     - `INotificationService`: Bắn thông báo đẩy tức thì Firebase theo chuẩn từng sự kiện.
     - `IBillReviewService`: Nhạc trưởng điều phối chu trình duyệt bill 6 bước nguyên tử.
   - 100% các hàm, phương thức trong BLL đều được viết chú thích tài liệu chuẩn XML Doc (`<summary>`, `<param>`, `<returns>`, vai trò, mục đích, cách dùng).
3. **Độ tin cậy & Kiểm thử**:
   - Toàn bộ solution biên dịch thành công: **0 Warning, 0 Error**.
   - Bộ kiểm thử tự động xUnit: **7/7 tests pass (100%)**.

### 9.2. Kế hoạch hành động tiếp theo
1. **Giai đoạn tiếp tục**:
   - Triển khai Worker tự động duyệt ngầm (Auto-Approval Worker) kết nối với AI VLM Server nội bộ.
   - Hoàn thiện giao diện Web Portal (Blazor / ASP.NET Core) thay thế hoàn toàn giao diện WinForms cũ.
   - Đưa kịch bản tự duyệt vào chạy thử nghiệm đối soát ngầm (Shadow Run).
