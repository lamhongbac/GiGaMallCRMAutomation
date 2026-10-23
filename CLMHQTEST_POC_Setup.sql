-- =================================================================================================
-- SCRIPT THỰC HIỆN POC (PROOF OF CONCEPT) TRÊN DATABASE: [CLMHQTEST]
-- DỰ ÁN: CRM GIGAMALL AUTOMATION MIGRATION (.NET 8 & CHECKPOINT FRAMEWORK)
-- =================================================================================================
-- Mục đích:
--   1. Kiểm tra tính tương thích cấu trúc bảng & Stored Procedures trên database test [CLMHQTEST].
--   2. Tạo dữ liệu mẫu giả lập cho 4 kịch bản cốt lõi:
--        - Kịch bản 1: Nhắc nhở cháy điểm (BurnPointReminder)
--        - Kịch bản 2: Reset Point - Khách ĐÃ mua sắm (Cancel Burn - Cứu điểm)
--        - Kịch bản 3: Reset Point - Khách KHÔNG mua sắm (Execute Burn - Hủy điểm thực tế)
--        - Kịch bản 4: Nâng hạng thẻ hội viên tự động (UpClass)
--   3. Cung cấp câu lệnh đối chiếu TRƯỚC và SAU khi kích hoạt API.
-- =================================================================================================

USE [CLMHQTEST];
GO

SET NOCOUNT ON;

PRINT '=================================================================================';
PRINT '>>> BẮT ĐẦU THIẾT LẬP DỮ LIỆU POC TRÊN DATABASE: ' + DB_NAME();
PRINT '=================================================================================';

-- -------------------------------------------------------------------------------------------------
-- PHẦN 1: BẢO ĐẢM CÁC MẪU THÔNG BÁO (NOTIFICATION TEMPLATES TRONG [dbo].[G_NotificationMessage])
-- -------------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.G_NotificationMessage WHERE Number = 'RemindExpiredPoint')
BEGIN
    INSERT INTO dbo.G_NotificationMessage (Number, Subject, Content, CreatedBy, CreatedOn)
    VALUES (
        'RemindExpiredPoint', 
        N'Thông báo điểm thưởng sắp hết hạn', 
        N'Quý khách có #Point# điểm tích lũy sắp hết hạn vào ngày #ExpiredDate#. Vui lòng sử dụng trước thời hạn!',
        'system', GETDATE()
    );
    PRINT '[SETUP] Đã thêm mẫu thông báo RemindExpiredPoint.';
END
ELSE
BEGIN
    PRINT '[OK] Mẫu thông báo RemindExpiredPoint đã tồn tại.';
END

IF NOT EXISTS (SELECT 1 FROM dbo.G_NotificationMessage WHERE Number = 'ResetRedeemPoint')
BEGIN
    INSERT INTO dbo.G_NotificationMessage (Number, Subject, Content, CreatedBy, CreatedOn)
    VALUES (
        'ResetRedeemPoint', 
        N'Thông báo điểm thưởng đã hết hạn', 
        N'Số điểm tích lũy (#Point# điểm) của Quý khách đã hết hạn sử dụng theo quy định của chương trình hội viên Gigamall.',
        'system', GETDATE()
    );
    PRINT '[SETUP] Đã thêm mẫu thông báo ResetRedeemPoint.';
END
ELSE
BEGIN
    PRINT '[OK] Mẫu thông báo ResetRedeemPoint đã tồn tại.';
END

IF NOT EXISTS (SELECT 1 FROM dbo.G_NotificationMessage WHERE Number = 'MemberUpClass')
BEGIN
    INSERT INTO dbo.G_NotificationMessage (Number, Subject, Content, CreatedBy, CreatedOn)
    VALUES (
        'MemberUpClass', 
        N'Chúc mừng Quý khách đã được nâng hạng thẻ!', 
        N'Chúc mừng Quý khách đã đạt điều kiện và được nâng lên hạng thẻ mới: #ClassName#. Hãy khám phá ngay các ưu đãi đặc quyền!',
        'system', GETDATE()
    );
    PRINT '[SETUP] Đã thêm mẫu thông báo MemberUpClass.';
END
ELSE
BEGIN
    PRINT '[OK] Mẫu thông báo MemberUpClass đã tồn tại.';
END

-- -------------------------------------------------------------------------------------------------
-- PHẦN 2: DỌN DẸP DỮ LIỆU TEST POC CŨ (CÔ LẬP THEO TIỀN TỐ 'POC_MEM_%')
-- -------------------------------------------------------------------------------------------------
PRINT '>>> Dọn dẹp dữ liệu test cũ tiền tố POC_MEM_%...';

DELETE FROM dbo.mssNotification WHERE MemID LIKE 'POC_MEM_%';
DELETE FROM dbo.mssCLMGiftPoint WHERE MemID LIKE 'POC_MEM_%';
DELETE FROM dbo.mssBurnPoint WHERE MemID LIKE 'POC_MEM_%';
DELETE FROM dbo.mssCardTrans WHERE MemID LIKE 'POC_MEM_%';
DELETE FROM dbo.mssCLMLedger WHERE MemID LIKE 'POC_MEM_%';
DELETE FROM dbo.mssMemClassLedger WHERE MemID LIKE 'POC_MEM_%';
DELETE FROM dbo.mssMobileTracking WHERE MemID LIKE 'POC_MEM_%';
DELETE FROM dbo.mssCustomer WHERE MemID LIKE 'POC_MEM_%';

PRINT '[OK] Đã làm sạch dữ liệu POC cũ an toàn.';

-- -------------------------------------------------------------------------------------------------
-- PHẦN 3: TẠO DỮ LIỆU GIẢ LẬP 4 KỊCH BẢN POC
-- -------------------------------------------------------------------------------------------------

-- >>> KỊCH BẢN 1: HỘI VIÊN NHẬN THÔNG BÁO NHẮC NHỞ CHÁY ĐIỂM (Burn Point Reminder)
-- Hội viên: POC_MEM_REMIND_01 (Có 1,500 điểm sắp hết hạn trong vòng 20 ngày tới)
INSERT INTO dbo.mssCustomer (MemID, CardNo, FullName, Mobile, Email, Status, CreatedOn)
VALUES ('POC_MEM_REMIND_01', 'POC_CARD_01', N'NGUYỄN VĂN REMIND', '0901000001', 'remind01@test.com', 'L', GETDATE());

INSERT INTO dbo.mssMobileTracking (MemID, AppID, CreatedOn)
VALUES ('POC_MEM_REMIND_01', 'FCM_TOKEN_DEVICE_REMIND_01', GETDATE());

INSERT INTO dbo.mssCLMLedger (CLPID, MemID, AccPoints, RedeemPoints, LastSaleDate, CreatedOn)
VALUES (1, 'POC_MEM_REMIND_01', 1500, 0, DATEADD(year, -2, GETDATE()), GETDATE());

INSERT INTO dbo.mssBurnPoint (
    ID, MemID, LastTransDate, ExpectedBurnDate, ExpiredDate, Point, BurnType, 
    IsReminderSend, BurnStatus, IsBurn, IsCancel, CreatedOn, CreatedBy
)
VALUES (
    NEWID(), 'POC_MEM_REMIND_01', DATEADD(year, -2, GETDATE()), 
    DATEADD(day, 20, CAST(GETDATE() AS DATE)), 
    DATEADD(day, 20, CAST(GETDATE() AS DATE)), 
    1500, 'NO_TRANSACTION', 0, 'Pending', 0, 0, GETDATE(), 'system'
);


-- >>> KỊCH BẢN 2: HỘI VIÊN ĐÃ MUA SẮM TRƯỚC HẠN -> HỦY TRỪ ĐIỂM (Cancel Burn - Cứu điểm)
-- Hội viên: POC_MEM_CANCEL_02 (Có 800 điểm đến hạn ngày mai, NHƯNG đã mua sắm cách đây 3 ngày)
INSERT INTO dbo.mssCustomer (MemID, CardNo, FullName, Mobile, Email, Status, CreatedOn)
VALUES ('POC_MEM_CANCEL_02', 'POC_CARD_02', N'TRẦN THỊ CỨU ĐIỂM', '0901000002', 'cancel02@test.com', 'L', GETDATE());

INSERT INTO dbo.mssMobileTracking (MemID, AppID, CreatedOn)
VALUES ('POC_MEM_CANCEL_02', 'FCM_TOKEN_DEVICE_CANCEL_02', GETDATE());

INSERT INTO dbo.mssCLMLedger (CLPID, MemID, AccPoints, RedeemPoints, LastSaleDate, CreatedOn)
VALUES (1, 'POC_MEM_CANCEL_02', 800, 0, DATEADD(day, -3, GETDATE()), GETDATE());

-- Bản ghi Pending đến hạn
INSERT INTO dbo.mssBurnPoint (
    ID, MemID, LastTransDate, ExpectedBurnDate, ExpiredDate, Point, BurnType, 
    IsReminderSend, BurnStatus, IsBurn, IsCancel, CreatedOn, CreatedBy
)
VALUES (
    NEWID(), 'POC_MEM_CANCEL_02', DATEADD(year, -2, GETDATE()), 
    CAST(GETDATE() AS DATE), 
    DATEADD(day, 1, CAST(GETDATE() AS DATE)), 
    800, 'NO_TRANSACTION', 1, 'Pending', 0, 0, DATEADD(day, -30, GETDATE()), 'system'
);

-- Giao dịch hợp lệ phát sinh trước ExpiredDate
INSERT INTO dbo.mssCardTrans (MemID, CardNo, TransDate, TotalAmount, CreatedOn)
VALUES ('POC_MEM_CANCEL_02', 'POC_CARD_02', DATEADD(day, -3, GETDATE()), 250000, GETDATE());


-- >>> KỊCH BẢN 3: HỘI VIÊN KHÔNG MUA SẮM -> THỰC THI TRỪ ĐIỂM (Execute Burned)
-- Hội viên: POC_MEM_BURN_03 (Có 2,500 điểm, đến hạn hôm nay, không phát sinh GD -> Trừ 2,500 điểm)
INSERT INTO dbo.mssCustomer (MemID, CardNo, FullName, Mobile, Email, Status, CreatedOn)
VALUES ('POC_MEM_BURN_03', 'POC_CARD_03', N'LÊ VĂN HỦY ĐIỂM', '0901000003', 'burn03@test.com', 'L', GETDATE());

INSERT INTO dbo.mssMobileTracking (MemID, AppID, CreatedOn)
VALUES ('POC_MEM_BURN_03', 'FCM_TOKEN_DEVICE_BURN_03', GETDATE());

INSERT INTO dbo.mssCLMLedger (CLPID, MemID, AccPoints, RedeemPoints, LastSaleDate, CreatedOn)
VALUES (1, 'POC_MEM_BURN_03', 2500, 0, DATEADD(year, -2, GETDATE()), GETDATE());

INSERT INTO dbo.mssBurnPoint (
    ID, MemID, LastTransDate, ExpectedBurnDate, ExpiredDate, Point, BurnType, 
    IsReminderSend, BurnStatus, IsBurn, IsCancel, CreatedOn, CreatedBy
)
VALUES (
    NEWID(), 'POC_MEM_BURN_03', DATEADD(year, -2, GETDATE()), 
    CAST(GETDATE() AS DATE), 
    CAST(GETDATE() AS DATE), 
    2500, 'NO_TRANSACTION', 1, 'Pending', 0, 0, DATEADD(day, -30, GETDATE()), 'system'
);


-- >>> KỊCH BẢN 4: HỘI VIÊN ĐỦ ĐIỀU KIỆN THĂNG HẠNG THẺ (UpClass)
-- Hội viên: POC_MEM_UPCLASS_04 (Đang ở hạng SILVER, tích lũy 15,000,000đ -> Thăng hạng GOLD + Tặng 500 điểm)
INSERT INTO dbo.mssCustomer (MemID, CardNo, FullName, Mobile, Email, Status, CreatedOn)
VALUES ('POC_MEM_UPCLASS_04', 'POC_CARD_04', N'PHẠM THỊ LÊN HẠNG', '0901000004', 'upclass04@test.com', 'L', GETDATE());

INSERT INTO dbo.mssMobileTracking (MemID, AppID, CreatedOn)
VALUES ('POC_MEM_UPCLASS_04', 'FCM_TOKEN_DEVICE_UPCLASS_04', GETDATE());

INSERT INTO dbo.mssCLMLedger (CLPID, MemID, AccPoints, RedeemPoints, LastSaleDate, CreatedOn)
VALUES (1, 'POC_MEM_UPCLASS_04', 100, 0, GETDATE(), GETDATE());

-- Đảm bảo hạng GOLD có cấu hình RewardPoint = 500
IF NOT EXISTS (SELECT 1 FROM dbo.G_MemberClass WHERE ClassNo = 'GOLD')
BEGIN
    INSERT INTO dbo.G_MemberClass (CLPID, ClassNo, ClassName, ClassOrder, RewardRate, ReviewRate, IsMember, CreatedOn)
    VALUES (1, 'GOLD', N'Thành viên Vàng', 2, 500, 0, 1, GETDATE());
END
ELSE
BEGIN
    UPDATE dbo.G_MemberClass SET RewardRate = 500 WHERE ClassNo = 'GOLD';
END

-- Bản ghi hạng SILVER hiện tại đang hoạt động (Status = 'O')
INSERT INTO dbo.mssMemClassLedger (
    EventDate, CLPID, MemID, ClassNo, OpenAmt, DebitAmt, CreditAmt, CloseAmt,
    Unit, Notes, DocType, ReviewFlag, Status, CreatedBy, CreatedOn
)
VALUES (
    DATEADD(month, -6, GETDATE()), 1, 'POC_MEM_UPCLASS_04', 'SILVER', 
    15000000, 0, 0, 15000000, 'P', 'InitialMember', 'R', 'Y', 'O', 'System', GETDATE()
);

PRINT '=================================================================================';
PRINT '>>> THIẾT LẬP DỮ LIỆU POC THÀNH CÔNG CHO 4 HỘI VIÊN:';
PRINT '    1. POC_MEM_REMIND_01  -> Test BurnPointReminder';
PRINT '    2. POC_MEM_CANCEL_02  -> Test ResetPoint (Kỳ vọng: Cancel Burn, giữ điểm)';
PRINT '    3. POC_MEM_BURN_03    -> Test ResetPoint (Kỳ vọng: Trừ điểm ví và đổi sang Burned)';
PRINT '    4. POC_MEM_UPCLASS_04 -> Test UpClass (Kỳ vọng: Lên hạng GOLD, cộng 500 điểm)';
PRINT '=================================================================================';
GO

-- -------------------------------------------------------------------------------------------------
-- PHẦN 4: TRUY VẤN ĐỐI CHIẾU TRƯỚC VÀ SAU KHI GỌI API (VERIFICATION QUERIES)
-- -------------------------------------------------------------------------------------------------

PRINT '>>> [TRẠNG THÁI HIỆN TẠI] 1. BẢNG THEO DÕI ĐIỂM HẾT HẠN (dbo.mssBurnPoint):';
SELECT 
    MemID, Point, ExpiredDate, ExpectedBurnDate, 
    BurnStatus, IsReminderSend, ReminderSendDate, IsBurn, BurnDate, IsCancel
FROM dbo.mssBurnPoint WITH (NOLOCK)
WHERE MemID LIKE 'POC_MEM_%'
ORDER BY MemID;

PRINT '>>> [TRẠNG THÁI HIỆN TẠI] 2. VÍ ĐIỂM HỘI VIÊN (dbo.mssCLMLedger):';
SELECT 
    MemID, AccPoints, RedeemPoints, LastSaleDate, ModifiedOn, ModifiedBy
FROM dbo.mssCLMLedger WITH (NOLOCK)
WHERE MemID LIKE 'POC_MEM_%'
ORDER BY MemID;

PRINT '>>> [TRẠNG THÁI HIỆN TẠI] 3. HẠNG THẺ HỘI VIÊN (dbo.mssMemClassLedger):';
SELECT 
    ID, MemID, ClassNo, OpenAmt, CloseAmt, Status, Notes, EventDate
FROM dbo.mssMemClassLedger WITH (NOLOCK)
WHERE MemID LIKE 'POC_MEM_%'
ORDER BY MemID, ID;

PRINT '>>> [TRẠNG THÁI HIỆN TẠI] 4. HỘP THƯ APP & GIAO DỊCH ĐIỂM:';
SELECT MemID, EventName, Subject, CreatedOn FROM dbo.mssNotification WITH (NOLOCK) WHERE MemID LIKE 'POC_MEM_%';
SELECT MemID, RewardPoint, Description, CreatedOn FROM dbo.mssCLMGiftPoint WITH (NOLOCK) WHERE MemID LIKE 'POC_MEM_%';
GO