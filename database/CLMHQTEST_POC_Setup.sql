-- =================================================================================================
-- SCRIPT THỰC HIỆN POC (PROOF OF CONCEPT) TRÊN DATABASE: [CLMHQTEST]
-- DỰ ÁN: CRM GIGAMALL AUTOMATION MIGRATION (.NET 8 & CHECKPOINT FRAMEWORK)
-- =================================================================================================
-- Mục tiêu theo yêu cầu:
--   1. KIỂM TRA SỰ TỒN TẠI VÀ TỰ ĐỘNG TẠO CÁC BẢNG & STORED PROCEDURES (DDL)
--   2. CHUẨN BỊ DỮ LIỆU POC: SAO CHÉP DANH MỤC/CẤU HÌNH TỪ [CLMHQ4GIGA] SANG [CLMHQTEST] & TẠO 4 KỊCH BẢN POC
--   3. CUNG CẤP BỘ TRUY VẤN ĐỐI CHIẾU TRƯỚC VÀ SAU KHI GỌI API
-- =================================================================================================

USE [CLMHQTEST];
GO

SET NOCOUNT ON;

PRINT '=================================================================================';
PRINT '>>> BẮT ĐẦU KHỞI TẠO MÔI TRƯỜNG POC TRÊN DATABASE: ' + DB_NAME();
PRINT '=================================================================================';

-- =================================================================================================
-- MỤC TIÊU 1: KIỂM TRA VÀ TẠO BẢNG CƠ SỞ DỮ LIỆU (DDL SCHEMA CREATION)
-- =================================================================================================

-- 1. Bảng cấu hình hệ thống: G_Setting
IF OBJECT_ID('dbo.G_Setting', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.G_Setting (
        [SettingKey] [nvarchar](100) NOT NULL PRIMARY KEY,
        [SettingValue] [nvarchar](max) NULL,
        [Description] [nvarchar](500) NULL,
        [CreatedBy] [nvarchar](100) NULL,
        [CreatedOn] [datetime] NULL DEFAULT (GETDATE()),
        [ModifiedBy] [nvarchar](100) NULL,
        [ModifiedOn] [datetime] NULL
    );
    PRINT '[DDL] Đã tạo bảng dbo.G_Setting.';
END
ELSE
    PRINT '[DDL] Bảng dbo.G_Setting đã tồn tại.';

-- 2. Bảng chương trình hội viên: G_CLP
IF OBJECT_ID('dbo.G_CLP', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.G_CLP (
        [ID] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ProgramCode] [nvarchar](50) NOT NULL,
        [ProgramName] [nvarchar](250) NULL,
        [IsActive] [bit] NULL DEFAULT (1),
        [CreatedOn] [datetime] NULL DEFAULT (GETDATE())
    );
    PRINT '[DDL] Đã tạo bảng dbo.G_CLP.';
END
ELSE
    PRINT '[DDL] Bảng dbo.G_CLP đã tồn tại.';

-- 3. Bảng danh mục hạng thành viên: G_MemberClass
IF OBJECT_ID('dbo.G_MemberClass', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.G_MemberClass (
        [ID] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [CLPID] [int] NOT NULL,
        [ClassNo] [nvarchar](50) NOT NULL,
        [ClassName] [nvarchar](250) NOT NULL,
        [ClassOrder] [int] NOT NULL DEFAULT (0),
        [RewardRate] [decimal](18, 2) NULL DEFAULT (0),
        [ReviewRate] [decimal](18, 2) NULL DEFAULT (0),
        [IsMember] [bit] NULL DEFAULT (1),
        [CreatedOn] [datetime] NULL DEFAULT (GETDATE()),
        [ModifiedOn] [datetime] NULL
    );
    PRINT '[DDL] Đã tạo bảng dbo.G_MemberClass.';
END
ELSE
    PRINT '[DDL] Bảng dbo.G_MemberClass đã tồn tại.';

-- 4. Bảng thang điểm/doanh số nâng hạng: G_MemRangeClass
IF OBJECT_ID('dbo.G_MemRangeClass', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.G_MemRangeClass (
        [ID] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [CLPID] [int] NOT NULL,
        [ClassNo] [nvarchar](50) NOT NULL,
        [FromAmt] [decimal](18, 2) NOT NULL DEFAULT (0),
        [ToAmt] [decimal](18, 2) NOT NULL DEFAULT (0),
        [UpgradeClass] [nvarchar](50) NULL,
        [ValidMonths] [int] NULL DEFAULT (12),
        [CreatedOn] [datetime] NULL DEFAULT (GETDATE())
    );
    PRINT '[DDL] Đã tạo bảng dbo.G_MemRangeClass.';
END
ELSE
    PRINT '[DDL] Bảng dbo.G_MemRangeClass đã tồn tại.';

-- 5. Bảng mẫu thông báo: G_NotificationMessage
IF OBJECT_ID('dbo.G_NotificationMessage', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.G_NotificationMessage (
        [ID] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Number] [nvarchar](100) NOT NULL,
        [Subject] [nvarchar](500) NULL,
        [Content] [nvarchar](max) NULL,
        [CreatedBy] [nvarchar](100) NULL,
        [CreatedOn] [datetime] NULL DEFAULT (GETDATE()),
        [ModifiedBy] [nvarchar](100) NULL,
        [ModifiedOn] [datetime] NULL
    );
    PRINT '[DDL] Đã tạo bảng dbo.G_NotificationMessage.';
END
ELSE
    PRINT '[DDL] Bảng dbo.G_NotificationMessage đã tồn tại.';

-- 6. Bảng khách hàng / hội viên: mssCustomer
IF OBJECT_ID('dbo.mssCustomer', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.mssCustomer (
        [MemID] [nvarchar](50) NOT NULL PRIMARY KEY,
        [CardNo] [nvarchar](50) NULL,
        [FullName] [nvarchar](250) NULL,
        [Mobile] [nvarchar](50) NULL,
        [Email] [nvarchar](150) NULL,
        [Status] [nvarchar](10) NULL DEFAULT ('L'),
        [CreatedOn] [datetime] NULL DEFAULT (GETDATE()),
        [ModifiedOn] [datetime] NULL,
        [ModifiedBy] [nvarchar](100) NULL
    );
    PRINT '[DDL] Đã tạo bảng dbo.mssCustomer.';
END
ELSE
    PRINT '[DDL] Bảng dbo.mssCustomer đã tồn tại.';

-- 7. Bảng thiết bị Mobile App (FCM Push Token): mssMobileTracking
IF OBJECT_ID('dbo.mssMobileTracking', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.mssMobileTracking (
        [ID] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [MemID] [nvarchar](50) NOT NULL,
        [AppID] [nvarchar](500) NOT NULL,
        [DeviceType] [nvarchar](50) NULL,
        [CreatedOn] [datetime] NULL DEFAULT (GETDATE()),
        [ModifiedOn] [datetime] NULL
    );
    PRINT '[DDL] Đã tạo bảng dbo.mssMobileTracking.';
END
ELSE
    PRINT '[DDL] Bảng dbo.mssMobileTracking đã tồn tại.';

-- 8. Bảng ví điểm hội viên: mssCLMLedger
IF OBJECT_ID('dbo.mssCLMLedger', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.mssCLMLedger (
        [ID] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [CLPID] [int] NOT NULL DEFAULT (1),
        [MemID] [nvarchar](50) NOT NULL,
        [AccSales] [decimal](18, 2) NOT NULL DEFAULT (0),
        [AccPoints] [decimal](18, 2) NOT NULL DEFAULT (0),
        [RedeemPoints] [decimal](18, 2) NOT NULL DEFAULT (0),
        [AccTC] [int] NULL DEFAULT (0),
        [LastSaleDate] [datetime] NULL,
        [CreatedOn] [datetime] NULL DEFAULT (GETDATE()),
        [CreatedBy] [nvarchar](100) NULL,
        [ModifiedOn] [datetime] NULL,
        [ModifiedBy] [nvarchar](100) NULL
    );
    CREATE UNIQUE NONCLUSTERED INDEX IX_mssCLMLedger_MemID ON dbo.mssCLMLedger(MemID);
    PRINT '[DDL] Đã tạo bảng dbo.mssCLMLedger.';
END
ELSE
    PRINT '[DDL] Bảng dbo.mssCLMLedger đã tồn tại.';

-- 9. Bảng điểm thưởng / tặng điểm nâng hạng: mssCLMGiftPoint
IF OBJECT_ID('dbo.mssCLMGiftPoint', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.mssCLMGiftPoint (
        [ID] [uniqueidentifier] NOT NULL PRIMARY KEY DEFAULT (NEWID()),
        [CLPID] [int] NOT NULL DEFAULT (1),
        [MemID] [nvarchar](50) NOT NULL,
        [RewardPoint] [decimal](18, 2) NOT NULL DEFAULT (0),
        [Description] [nvarchar](500) NULL,
        [CreatedOn] [datetime] NULL DEFAULT (GETDATE()),
        [CreatedBy] [nvarchar](100) NULL DEFAULT ('system')
    );
    PRINT '[DDL] Đã tạo bảng dbo.mssCLMGiftPoint.';
END
ELSE
    PRINT '[DDL] Bảng dbo.mssCLMGiftPoint đã tồn tại.';

-- 10. Bảng quản lý điểm hết hạn / cháy điểm: mssBurnPoint
IF OBJECT_ID('dbo.mssBurnPoint', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.mssBurnPoint (
        [ID] [uniqueidentifier] NOT NULL PRIMARY KEY DEFAULT (NEWID()),
        [MemID] [nvarchar](50) NOT NULL,
        [LastTransDate] [datetime] NULL,
        [ExpectedBurnDate] [date] NOT NULL,
        [ExpiredDate] [date] NOT NULL,
        [Point] [decimal](18, 2) NOT NULL DEFAULT (0),
        [BurnType] [nvarchar](50) NULL DEFAULT ('NO_TRANSACTION'),
        [IsReminderSend] [bit] NOT NULL DEFAULT (0),
        [ReminderSendDate] [datetime] NULL,
        [BurnStatus] [nvarchar](50) NOT NULL DEFAULT ('Pending'),
        [IsBurn] [bit] NOT NULL DEFAULT (0),
        [BurnDate] [datetime] NULL,
        [IsCancel] [bit] NOT NULL DEFAULT (0),
        [CancelDate] [datetime] NULL,
        [CreatedOn] [datetime] NULL DEFAULT (GETDATE()),
        [CreatedBy] [nvarchar](100) NULL DEFAULT ('system'),
        [ModifiedOn] [datetime] NULL,
        [ModifiedBy] [nvarchar](100) NULL
    );
    CREATE NONCLUSTERED INDEX IX_mssBurnPoint_Status ON dbo.mssBurnPoint(BurnStatus, ExpectedBurnDate, IsReminderSend);
    PRINT '[DDL] Đã tạo bảng dbo.mssBurnPoint.';
END
ELSE
    PRINT '[DDL] Bảng dbo.mssBurnPoint đã tồn tại.';

-- 11. Bảng lịch sử thay đổi / đánh giá hạng hội viên: mssMemClassLedger
IF OBJECT_ID('dbo.mssMemClassLedger', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.mssMemClassLedger (
        [ID] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [EventDate] [datetime] NOT NULL DEFAULT (GETDATE()),
        [CLPID] [int] NOT NULL DEFAULT (1),
        [MemID] [nvarchar](50) NOT NULL,
        [ClassNo] [nvarchar](50) NOT NULL,
        [OpenAmt] [decimal](18, 2) NOT NULL DEFAULT (0),
        [DebitAmt] [decimal](18, 2) NOT NULL DEFAULT (0),
        [CreditAmt] [decimal](18, 2) NOT NULL DEFAULT (0),
        [CloseAmt] [decimal](18, 2) NOT NULL DEFAULT (0),
        [Unit] [nvarchar](10) NULL DEFAULT ('P'),
        [Notes] [nvarchar](500) NULL,
        [DocType] [nvarchar](50) NULL DEFAULT ('S'),
        [ReviewFlag] [nvarchar](10) NULL DEFAULT ('N'),
        [Status] [nvarchar](10) NOT NULL DEFAULT ('O'),
        [LastSaleDocID] [uniqueidentifier] NULL,
        [CreatedBy] [nvarchar](100) NULL DEFAULT ('system'),
        [CreatedOn] [datetime] NULL DEFAULT (GETDATE()),
        [ModifiedBy] [nvarchar](100) NULL,
        [ModifiedOn] [datetime] NULL
    );
    CREATE NONCLUSTERED INDEX IX_mssMemClassLedger_MemID ON dbo.mssMemClassLedger(MemID, Status);
    PRINT '[DDL] Đã tạo bảng dbo.mssMemClassLedger.';
END
ELSE
    PRINT '[DDL] Bảng dbo.mssMemClassLedger đã tồn tại.';

-- 12. Bảng hộp thư thông báo gửi App: mssNotification
IF OBJECT_ID('dbo.mssNotification', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.mssNotification (
        [ID] [uniqueidentifier] NOT NULL PRIMARY KEY DEFAULT (NEWID()),
        [MemID] [nvarchar](50) NOT NULL,
        [AppID] [nvarchar](500) NULL,
        [Title] [nvarchar](250) NULL,
        [EventName] [nvarchar](100) NULL,
        [Subject] [nvarchar](500) NULL,
        [Body] [nvarchar](max) NULL,
        [Status] [nvarchar](50) NULL DEFAULT ('Sent'),
        [CreatedOn] [datetime] NULL DEFAULT (GETDATE()),
        [CreatedBy] [nvarchar](100) NULL DEFAULT ('system')
    );
    PRINT '[DDL] Đã tạo bảng dbo.mssNotification.';
END
ELSE
    PRINT '[DDL] Bảng dbo.mssNotification đã tồn tại.';

-- 13. Bảng giao dịch quẹt thẻ / hóa đơn mua sắm: mssCardTrans
IF OBJECT_ID('dbo.mssCardTrans', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.mssCardTrans (
        [ID] [bigint] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [DocID] [uniqueidentifier] NOT NULL DEFAULT (NEWID()),
        [CLPID] [int] NOT NULL DEFAULT (1),
        [MemID] [nvarchar](50) NOT NULL,
        [CardNo] [nvarchar](50) NULL,
        [ClassNo] [nvarchar](50) NULL,
        [OrderID] [uniqueidentifier] NULL,
        [BillNo] [nvarchar](50) NULL,
        [CheckNo] [nvarchar](50) NULL,
        [OutletNumber] [nvarchar](50) NULL,
        [TransDate] [datetime] NOT NULL DEFAULT (GETDATE()),
        [POSBusinessDate] [datetime] NULL,
        [TransType] [int] NULL DEFAULT (1),
        [SubTotal] [decimal](18, 2) NOT NULL DEFAULT (0),
        [TotalBillValue] [decimal](18, 2) NOT NULL DEFAULT (0),
        [TotalAmount] [decimal](18, 2) NOT NULL DEFAULT (0),
        [RPointGain] [decimal](18, 2) NULL DEFAULT (0),
        [RPointRate] [decimal](18, 2) NULL DEFAULT (10000),
        [ReviewPointGain] [decimal](18, 2) NULL DEFAULT (0),
        [ReviewPointRate] [decimal](18, 2) NULL DEFAULT (10000),
        [CashierName] [nvarchar](100) NULL,
        [UseMobileApp] [bit] NULL DEFAULT (1),
        [BProcess] [nvarchar](10) NULL DEFAULT ('Y'),
        [Notes] [nvarchar](500) NULL,
        [CreatedBy] [nvarchar](100) NULL DEFAULT ('Operator'),
        [CreatedOn] [datetime] NULL DEFAULT (GETDATE())
    );
    CREATE NONCLUSTERED INDEX IX_mssCardTrans_MemID ON dbo.mssCardTrans(MemID, TransDate);
    PRINT '[DDL] Đã tạo bảng dbo.mssCardTrans.';
END
ELSE
    PRINT '[DDL] Bảng dbo.mssCardTrans đã tồn tại.';
GO


-- 14. Bảng hóa đơn OCR tải lên: mssOCRTrialTrans
IF OBJECT_ID('dbo.mssOCRTrialTrans', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.mssOCRTrialTrans (
        [ID] [uniqueidentifier] NOT NULL PRIMARY KEY DEFAULT (NEWID()),
        [MemID] [nvarchar](50) NOT NULL,
        [UploadImageDateTime] [datetime] NOT NULL DEFAULT (GETDATE()),
        [ImagePath] [nvarchar](500) NULL,
        [ImageFileName] [nvarchar](250) NULL,
        [OutletNumber] [nvarchar](50) NULL,
        [BrandNumber] [nvarchar](50) NULL,
        [OutletName] [nvarchar](250) NULL,
        [BrandName] [nvarchar](250) NULL,
        [BillNumber] [nvarchar](50) NULL,
        [BillDateTime] [nvarchar](50) NULL,
        [BillTotal] [nvarchar](50) NULL,
        [CashierName] [nvarchar](100) NULL,
        [ScanDate] [datetime] NULL DEFAULT (GETDATE()),
        [Approved] [bit] NULL,
        [Reviewed] [bit] NOT NULL DEFAULT (0),
        [ReviewDate] [datetime] NULL,
        [ReviewBy] [nvarchar](100) NULL,
        [ReviewNotes] [nvarchar](500) NULL,
        [PointGain] [decimal](18, 2) NULL DEFAULT (0),
        [OutletNumberA] [nvarchar](50) NULL,
        [BrandNumberA] [nvarchar](50) NULL,
        [BillNumberA] [nvarchar](50) NULL,
        [BillDateTimeA] [datetime] NULL,
        [BillTotalA] [decimal](18, 2) NULL,
        [DataStatus] [nvarchar](50) NOT NULL DEFAULT ('Pending')
    );
    CREATE NONCLUSTERED INDEX IX_mssOCRTrialTrans_MemID ON dbo.mssOCRTrialTrans(MemID);
    CREATE NONCLUSTERED INDEX IX_mssOCRTrialTrans_DataStatus ON dbo.mssOCRTrialTrans(DataStatus);
    PRINT '[DDL] Đã tạo bảng dbo.mssOCRTrialTrans.';
END
ELSE
    PRINT '[DDL] Bảng dbo.mssOCRTrialTrans đã tồn tại.';
GO

-- =================================================================================================
-- TẠO CÁC STORED PROCEDURES HỖ TRỢ BÁO CÁO & XỬ LÝ (NẾU CHƯA CÓ)
-- =================================================================================================

-- Stored Procedure: GetBurnPointReport (Hỗ trợ màn hình báo cáo trừ điểm)
IF OBJECT_ID('dbo.GetBurnPointReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.GetBurnPointReport;
GO

CREATE PROCEDURE dbo.GetBurnPointReport
    @FromDate DATE = NULL,
    @ToDate DATE = NULL,
    @BurnStatus NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        b.ID,
        b.MemID,
        c.FullName,
        c.Mobile,
        c.CardNo,
        b.LastTransDate,
        b.ExpectedBurnDate,
        b.ExpiredDate,
        b.Point,
        b.BurnType,
        b.IsReminderSend,
        b.ReminderSendDate,
        b.BurnStatus,
        b.IsBurn,
        b.BurnDate,
        b.IsCancel,
        b.CancelDate,
        b.CreatedOn
    FROM dbo.mssBurnPoint b WITH (NOLOCK)
    LEFT JOIN dbo.mssCustomer c WITH (NOLOCK) ON b.MemID = c.MemID
    WHERE (@FromDate IS NULL OR b.ExpectedBurnDate >= @FromDate)
      AND (@ToDate IS NULL OR b.ExpectedBurnDate <= @ToDate)
      AND (@BurnStatus IS NULL OR @BurnStatus = '' OR b.BurnStatus = @BurnStatus)
    ORDER BY b.ExpectedBurnDate DESC, b.CreatedOn DESC;
END
GO
PRINT '[SP] Đã khởi tạo Stored Procedure: dbo.GetBurnPointReport.';
GO

-- =================================================================================================
-- MỤC TIÊU 2: CHUẨN BỊ DỮ LIỆU POC (SAO CHÉP TỪ CLMHQ4GIGA SANG CLMHQTEST)
-- =================================================================================================

PRINT '---------------------------------------------------------------------------------';
PRINT '>>> BẮT ĐẦU ĐỒNG BỘ DỮ LIỆU CẤU HÌNH & DANH MỤC TỪ [CLMHQ4GIGA]...';
PRINT '---------------------------------------------------------------------------------';

-- 1. Sao chép cấu hình: G_Setting
IF DB_ID('CLMHQ4GIGA') IS NOT NULL
BEGIN
    INSERT INTO dbo.G_Setting (SettingKey, SettingValue, Description, CreatedBy, CreatedOn)
    SELECT src.SettingKey, src.SettingValue, src.Description, src.CreatedBy, src.CreatedOn
    FROM [CLMHQ4GIGA].dbo.G_Setting src WITH (NOLOCK)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.G_Setting dest WHERE dest.SettingKey = src.SettingKey);
    PRINT '[DATA COPY] Đã đồng bộ G_Setting từ CLMHQ4GIGA.';
END

-- 2. Sao chép chương trình khách hàng: G_CLP
IF DB_ID('CLMHQ4GIGA') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT dbo.G_CLP ON;
    INSERT INTO dbo.G_CLP (ID, ProgramCode, ProgramName, IsActive, CreatedOn)
    SELECT src.ID, src.ProgramCode, src.ProgramName, src.IsActive, src.CreatedOn
    FROM [CLMHQ4GIGA].dbo.G_CLP src WITH (NOLOCK)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.G_CLP dest WHERE dest.ID = src.ID);
    SET IDENTITY_INSERT dbo.G_CLP OFF;
    PRINT '[DATA COPY] Đã đồng bộ G_CLP từ CLMHQ4GIGA.';
END
ELSE IF NOT EXISTS (SELECT 1 FROM dbo.G_CLP WHERE ID = 1)
BEGIN
    SET IDENTITY_INSERT dbo.G_CLP ON;
    INSERT INTO dbo.G_CLP (ID, ProgramCode, ProgramName, IsActive, CreatedOn)
    VALUES (1, 'GIGAMALL_LOYALTY', N'Chương trình khách hàng thân thiết Gigamall', 1, GETDATE());
    SET IDENTITY_INSERT dbo.G_CLP OFF;
    PRINT '[DATA SEED] Đã chèn G_CLP mặc định (ID=1).';
END

-- 3. Sao chép danh mục hạng thẻ: G_MemberClass
IF DB_ID('CLMHQ4GIGA') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT dbo.G_MemberClass ON;
    INSERT INTO dbo.G_MemberClass (ID, CLPID, ClassNo, ClassName, ClassOrder, RewardRate, ReviewRate, IsMember, CreatedOn)
    SELECT src.ID, src.CLPID, src.ClassNo, src.ClassName, src.ClassOrder, src.RewardRate, src.ReviewRate, src.IsMember, src.CreatedOn
    FROM [CLMHQ4GIGA].dbo.G_MemberClass src WITH (NOLOCK)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.G_MemberClass dest WHERE dest.ID = src.ID);
    SET IDENTITY_INSERT dbo.G_MemberClass OFF;
    PRINT '[DATA COPY] Đã đồng bộ G_MemberClass từ CLMHQ4GIGA.';
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.G_MemberClass WHERE ClassNo = 'SILVER')
        INSERT INTO dbo.G_MemberClass (CLPID, ClassNo, ClassName, ClassOrder, RewardRate, ReviewRate, IsMember, CreatedOn)
        VALUES (1, 'SILVER', N'Thành viên Bạc', 1, 0, 0, 1, GETDATE());

    IF NOT EXISTS (SELECT 1 FROM dbo.G_MemberClass WHERE ClassNo = 'GOLD')
        INSERT INTO dbo.G_MemberClass (CLPID, ClassNo, ClassName, ClassOrder, RewardRate, ReviewRate, IsMember, CreatedOn)
        VALUES (1, 'GOLD', N'Thành viên Vàng', 2, 500, 0, 1, GETDATE());

    IF NOT EXISTS (SELECT 1 FROM dbo.G_MemberClass WHERE ClassNo = 'PLATINUM')
        INSERT INTO dbo.G_MemberClass (CLPID, ClassNo, ClassName, ClassOrder, RewardRate, ReviewRate, IsMember, CreatedOn)
        VALUES (1, 'PLATINUM', N'Thành viên Bạch Kim', 3, 1000, 0, 1, GETDATE());
    PRINT '[DATA SEED] Đã chèn G_MemberClass mẫu (SILVER, GOLD, PLATINUM).';
END

-- 4. Sao chép quy định nâng hạng: G_MemRangeClass
IF DB_ID('CLMHQ4GIGA') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT dbo.G_MemRangeClass ON;
    INSERT INTO dbo.G_MemRangeClass (ID, CLPID, ClassNo, FromAmt, ToAmt, UpgradeClass, ValidMonths, CreatedOn)
    SELECT src.ID, src.CLPID, src.ClassNo, src.FromAmt, src.ToAmt, src.UpgradeClass, src.ValidMonths, src.CreatedOn
    FROM [CLMHQ4GIGA].dbo.G_MemRangeClass src WITH (NOLOCK)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.G_MemRangeClass dest WHERE dest.ID = src.ID);
    SET IDENTITY_INSERT dbo.G_MemRangeClass OFF;
    PRINT '[DATA COPY] Đã đồng bộ G_MemRangeClass từ CLMHQ4GIGA.';
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.G_MemRangeClass WHERE ClassNo = 'SILVER' AND UpgradeClass = 'GOLD')
        INSERT INTO dbo.G_MemRangeClass (CLPID, ClassNo, FromAmt, ToAmt, UpgradeClass, ValidMonths, CreatedOn)
        VALUES (1, 'SILVER', 10000000, 29999999, 'GOLD', 12, GETDATE());

    IF NOT EXISTS (SELECT 1 FROM dbo.G_MemRangeClass WHERE ClassNo = 'GOLD' AND UpgradeClass = 'PLATINUM')
        INSERT INTO dbo.G_MemRangeClass (CLPID, ClassNo, FromAmt, ToAmt, UpgradeClass, ValidMonths, CreatedOn)
        VALUES (1, 'GOLD', 30000000, 999999999, 'PLATINUM', 12, GETDATE());
    PRINT '[DATA SEED] Đã chèn G_MemRangeClass mẫu.';
END

-- 5. Sao chép mẫu tin nhắn thông báo: G_NotificationMessage
IF DB_ID('CLMHQ4GIGA') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT dbo.G_NotificationMessage ON;
    INSERT INTO dbo.G_NotificationMessage (ID, Number, Subject, Content, CreatedBy, CreatedOn)
    SELECT src.ID, src.Number, src.Subject, src.Content, src.CreatedBy, src.CreatedOn
    FROM [CLMHQ4GIGA].dbo.G_NotificationMessage src WITH (NOLOCK)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.G_NotificationMessage dest WHERE dest.Number = src.Number);
    SET IDENTITY_INSERT dbo.G_NotificationMessage OFF;
    PRINT '[DATA COPY] Đã đồng bộ G_NotificationMessage từ CLMHQ4GIGA.';
END

-- Đảm bảo có đủ 3 mẫu thông báo cốt lõi của Phase 1
IF NOT EXISTS (SELECT 1 FROM dbo.G_NotificationMessage WHERE Number = 'RemindExpiredPoint')
    INSERT INTO dbo.G_NotificationMessage (Number, Subject, Content, CreatedBy, CreatedOn)
    VALUES ('RemindExpiredPoint', N'Thông báo điểm thưởng sắp hết hạn', N'Quý khách có #Point# điểm tích lũy sắp hết hạn vào ngày #ExpiredDate#. Vui lòng sử dụng trước thời hạn!', 'system', GETDATE());

IF NOT EXISTS (SELECT 1 FROM dbo.G_NotificationMessage WHERE Number = 'ResetRedeemPoint')
    INSERT INTO dbo.G_NotificationMessage (Number, Subject, Content, CreatedBy, CreatedOn)
    VALUES ('ResetRedeemPoint', N'Thông báo điểm thưởng đã hết hạn', N'Số điểm tích lũy (#Point# điểm) của Quý khách đã hết hạn sử dụng theo quy định của chương trình hội viên Gigamall.', 'system', GETDATE());

IF NOT EXISTS (SELECT 1 FROM dbo.G_NotificationMessage WHERE Number = 'MemberUpClass')
    INSERT INTO dbo.G_NotificationMessage (Number, Subject, Content, CreatedBy, CreatedOn)
    VALUES ('MemberUpClass', N'Chúc mừng Quý khách đã được nâng hạng thẻ!', N'Chúc mừng Quý khách đã đạt điều kiện và được nâng lên hạng thẻ mới: #ClassName#. Hãy khám phá ngay các ưu đãi đặc quyền!', 'system', GETDATE());

-- 6. Sao chép 50 khách hàng mẫu từ CLMHQ4GIGA sang CLMHQTEST (để làm phong phú dữ liệu test)
IF DB_ID('CLMHQ4GIGA') IS NOT NULL
BEGIN
    INSERT INTO dbo.mssCustomer (MemID, CardNo, FullName, Mobile, Email, Status, CreatedOn)
    SELECT TOP 50 src.MemID, src.CardNo, src.FullName, src.Mobile, src.Email, src.Status, src.CreatedOn
    FROM [CLMHQ4GIGA].dbo.mssCustomer src WITH (NOLOCK)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.mssCustomer dest WHERE dest.MemID = src.MemID);

    INSERT INTO dbo.mssMobileTracking (MemID, AppID, DeviceType, CreatedOn)
    SELECT src.MemID, src.AppID, src.DeviceType, src.CreatedOn
    FROM [CLMHQ4GIGA].dbo.mssMobileTracking src WITH (NOLOCK)
    INNER JOIN dbo.mssCustomer c ON src.MemID = c.MemID
    WHERE NOT EXISTS (SELECT 1 FROM dbo.mssMobileTracking dest WHERE dest.MemID = src.MemID AND dest.AppID = src.AppID);

    INSERT INTO dbo.mssCLMLedger (CLPID, MemID, AccPoints, RedeemPoints, LastSaleDate, CreatedOn)
    SELECT src.CLPID, src.MemID, src.AccPoints, src.RedeemPoints, src.LastSaleDate, src.CreatedOn
    FROM [CLMHQ4GIGA].dbo.mssCLMLedger src WITH (NOLOCK)
    INNER JOIN dbo.mssCustomer c ON src.MemID = c.MemID
    WHERE NOT EXISTS (SELECT 1 FROM dbo.mssCLMLedger dest WHERE dest.MemID = src.MemID);

    PRINT '[DATA COPY] Đã sao chép 50 khách hàng mẫu từ CLMHQ4GIGA sang CLMHQTEST.';
END
GO

-- =================================================================================================
-- THIẾT LẬP 4 KỊCH BẢN POC ĐẶC TRƯNG (TIỀN TỐ POC_MEM_%)
-- =================================================================================================

PRINT '---------------------------------------------------------------------------------';
PRINT '>>> KHỞI TẠO DỮ LIỆU ĐẶC BIỆT CHO 4 KỊCH BẢN TEST POC...';
PRINT '---------------------------------------------------------------------------------';

-- Dọn dẹp dữ liệu cũ của 4 kịch bản POC
DELETE FROM dbo.mssNotification WHERE MemID LIKE 'POC_MEM_%';
DELETE FROM dbo.mssCLMGiftPoint WHERE MemID LIKE 'POC_MEM_%';
DELETE FROM dbo.mssBurnPoint WHERE MemID LIKE 'POC_MEM_%';
DELETE FROM dbo.mssCardTrans WHERE MemID LIKE 'POC_MEM_%';
DELETE FROM dbo.mssCLMLedger WHERE MemID LIKE 'POC_MEM_%';
DELETE FROM dbo.mssMemClassLedger WHERE MemID LIKE 'POC_MEM_%';
DELETE FROM dbo.mssMobileTracking WHERE MemID LIKE 'POC_MEM_%';
DELETE FROM dbo.mssCustomer WHERE MemID LIKE 'POC_MEM_%';

-- >>> KỊCH BẢN 1: HỘI VIÊN NHẬN THÔNG BÁO NHẮC NHỞ CHÁY ĐIỂM (BurnPointReminder)
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
-- Hội viên: POC_MEM_CANCEL_02 (Có 800 điểm đến hạn hôm nay, NHƯNG đã mua sắm cách đây 3 ngày)
INSERT INTO dbo.mssCustomer (MemID, CardNo, FullName, Mobile, Email, Status, CreatedOn)
VALUES ('POC_MEM_CANCEL_02', 'POC_CARD_02', N'TRẦN THỊ CỨU ĐIỂM', '0901000002', 'cancel02@test.com', 'L', GETDATE());

INSERT INTO dbo.mssMobileTracking (MemID, AppID, CreatedOn)
VALUES ('POC_MEM_CANCEL_02', 'FCM_TOKEN_DEVICE_CANCEL_02', GETDATE());

INSERT INTO dbo.mssCLMLedger (CLPID, MemID, AccPoints, RedeemPoints, LastSaleDate, CreatedOn)
VALUES (1, 'POC_MEM_CANCEL_02', 800, 0, DATEADD(day, -3, GETDATE()), GETDATE());

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

INSERT INTO dbo.mssCardTrans (MemID, CardNo, TransDate, TotalAmount, CreatedOn)
VALUES ('POC_MEM_CANCEL_02', 'POC_CARD_02', DATEADD(day, -3, GETDATE()), 250000, GETDATE());

-- >>> KỊCH BẢN 3: HỘI VIÊN KHÔNG MUA SẮM -> THỰC THI TRỪ ĐIỂM (Execute Burned)
-- Hội viên: POC_MEM_BURN_03 (Có 2,500 điểm đến hạn hôm nay, không mua sắm -> Bị trừ 2,500 điểm)
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

UPDATE dbo.G_MemberClass SET RewardRate = 500 WHERE ClassNo = 'GOLD';

INSERT INTO dbo.mssMemClassLedger (
    EventDate, CLPID, MemID, ClassNo, OpenAmt, DebitAmt, CreditAmt, CloseAmt,
    Unit, Notes, DocType, ReviewFlag, Status, CreatedBy, CreatedOn
)
VALUES (
    DATEADD(month, -6, GETDATE()), 1, 'POC_MEM_UPCLASS_04', 'SILVER', 
    15000000, 0, 0, 15000000, 'P', 'InitialMember', 'R', 'Y', 'O', 'System', GETDATE()
);

PRINT '=================================================================================';
PRINT '>>> HOÀN TẤT THIẾT LẬP MỤC TIÊU 1 & MỤC TIÊU 2 CHO [CLMHQTEST]!';
PRINT '=================================================================================';
GO

-- =================================================================================================
-- PHẦN 3: CÁC TRUY VẤN ĐỐI CHIẾU TRƯỚC VÀ SAU KHI GỌI API (VERIFICATION QUERIES)
-- =================================================================================================

PRINT '>>> [TRẠNG THÁI HIỆN TẠI] 1. BẢNG THEO DÕI ĐIỂM HẾT HẠN (dbo.mssBurnPoint):';
SELECT 
    MemID, Point, ExpiredDate, ExpectedBurnDate, 
    BurnStatus, IsReminderSend, ReminderSendDate, IsBurn, BurnDate, IsCancel, CancelDate
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


-- =================================================================================================
-- PHẦN 11: BẢNG PHẠM VI GIAN HÀNG & QUAY SỐ / QUÀ TẶNG KHUYẾN MÃI (LOTTERY & PROMOTION)
-- =================================================================================================

-- 11.1. Bảng G_Scope (Phạm vi gian hàng áp dụng)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'G_Scope')
BEGIN
    PRINT N'  -> Đang tạo bảng G_Scope...';
    CREATE TABLE dbo.G_Scope (
        ID INT IDENTITY(1,1) PRIMARY KEY,
        Number NVARCHAR(50) NULL,
        Name NVARCHAR(255) NULL,
        RegionList NVARCHAR(MAX) NULL,
        BranchList NVARCHAR(MAX) NULL,
        BrandList NVARCHAR(MAX) NULL,
        OutletList NVARCHAR(MAX) NULL, -- Danh sách mã gian hàng phân cách bởi dấu chấm phẩy ';'
        CreatedOn DATETIME DEFAULT GETDATE(),
        CreatedBy NVARCHAR(50) DEFAULT 'SYSTEM'
    );
END
GO

-- 11.2. Bảng mssLotteryProgram (Danh mục chương trình quay số & quà tặng)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'mssLotteryProgram')
BEGIN
    PRINT N'  -> Đang tạo bảng mssLotteryProgram...';
    CREATE TABLE dbo.mssLotteryProgram (
        ID INT IDENTITY(1,1) PRIMARY KEY,
        Type NVARCHAR(50) NOT NULL, -- Lottery, FirstTrans, Gift
        Number NVARCHAR(50) NULL,
        Name NVARCHAR(255) NOT NULL,
        FromDate DATETIME NULL,
        ToDate DATETIME NULL,
        ExtendExpiredDay INT NULL DEFAULT 0,
        MinBillValue DECIMAL(18,2) NULL DEFAULT 0,
        ScopeID INT NULL,
        JoinFrom DATETIME NULL,
        JoinTo DATETIME NULL,
        MaxLotteryCount INT NULL,
        MaxLotteryMemberCount INT NULL,
        MaxLotteryCountByDay INT NULL,
        MaxLotteryMemberCountByDay INT NULL,
        RankPoint INT NULL,
        RewardPoint INT NULL,
        TakeExpiredDay INT NULL DEFAULT 30,
        TakeByBillDate BIT NULL DEFAULT 0,
        TermCondition NVARCHAR(MAX) NULL,
        CreatedOn DATETIME DEFAULT GETDATE(),
        CreatedBy NVARCHAR(50) DEFAULT 'SYSTEM'
    );
END
GO

-- 11.3. Bảng mssLotteryGift (Kho vé số may mắn / Lucky Draw)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'mssLotteryGift')
BEGIN
    PRINT N'  -> Đang tạo bảng mssLotteryGift...';
    CREATE TABLE dbo.mssLotteryGift (
        ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        ProgramID INT NULL,
        LotteryCode NVARCHAR(50) NOT NULL,
        Name NVARCHAR(255) NULL,
        ItemCode NVARCHAR(50) NULL,
        ItemType NVARCHAR(50) NULL,
        MemID NVARCHAR(50) NULL, -- Gán khi hội viên trúng / được phát vé
        BillNo NVARCHAR(50) NULL,
        TransDate DATETIME NULL,
        SubTotal DECIMAL(18,2) NULL,
        GetCodeDate DATETIME NULL,
        GiftValue DECIMAL(18,2) NULL DEFAULT 0,
        IsEnable BIT DEFAULT 1,
        FromDate DATETIME NULL,
        ToDate DATETIME NULL,
        CreatedOn DATETIME DEFAULT GETDATE(),
        CreatedBy NVARCHAR(50) DEFAULT 'SYSTEM'
    );
    CREATE INDEX IX_mssLotteryGift_Program_Mem ON dbo.mssLotteryGift (ProgramID, MemID);
    CREATE INDEX IX_mssLotteryGift_TransDate ON dbo.mssLotteryGift (TransDate);
END
GO

-- 11.4. Bảng mssPromotionGift (Kho quà tặng khuyến mãi / Voucher ưu đãi)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'mssPromotionGift')
BEGIN
    PRINT N'  -> Đang tạo bảng mssPromotionGift...';
    CREATE TABLE dbo.mssPromotionGift (
        ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        ProgramID INT NULL,
        PromotionCode NVARCHAR(50) NOT NULL,
        Name NVARCHAR(255) NULL,
        ItemCode NVARCHAR(50) NULL,
        ItemType NVARCHAR(50) NULL,
        MemID NVARCHAR(50) NULL, -- Gán khi hội viên nhận quà
        BillNo NVARCHAR(50) NULL,
        TransDate DATETIME NULL,
        SubTotal DECIMAL(18,2) NULL,
        GetCodeDate DATETIME NULL,
        ToDate DATETIME NULL,
        GiftValue DECIMAL(18,2) NULL DEFAULT 0,
        IsEnable BIT DEFAULT 1,
        CreatedOn DATETIME DEFAULT GETDATE(),
        CreatedBy NVARCHAR(50) DEFAULT 'SYSTEM'
    );
    CREATE INDEX IX_mssPromotionGift_Program_Mem ON dbo.mssPromotionGift (ProgramID, MemID);
    CREATE INDEX IX_mssPromotionGift_TransDate ON dbo.mssPromotionGift (TransDate);
END
GO

-- 11.5. Dữ liệu mẫu POC cho Scope, Lottery & Promotion
IF NOT EXISTS (SELECT 1 FROM dbo.G_Scope WHERE ID = 1)
BEGIN
    SET IDENTITY_INSERT dbo.G_Scope ON;
    INSERT INTO dbo.G_Scope (ID, Number, Name, OutletList, CreatedOn, CreatedBy)
    VALUES (1, 'SCOPE_ALL', N'Toàn bộ gian hàng Gigamall', 'HIGHLANDS_L1;KFC_L3;LOTTERIA_B1;STARBUCKS_G;PHUC_LONG_G;CGV_L5', GETDATE(), 'POC_SEED');
    SET IDENTITY_INSERT dbo.G_Scope OFF;
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.mssLotteryProgram WHERE ID = 1)
BEGIN
    SET IDENTITY_INSERT dbo.mssLotteryProgram ON;
    INSERT INTO dbo.mssLotteryProgram (
        ID, Type, Number, Name, FromDate, ToDate, ExtendExpiredDay, MinBillValue, ScopeID,
        MaxLotteryCount, MaxLotteryMemberCount, MaxLotteryCountByDay, MaxLotteryMemberCountByDay, TakeExpiredDay
    ) VALUES (
        1, 'Lottery', 'LOTT2026', N'Vòng quay may mắn Mùa Mua Sắm Gigamall 2026',
        DATEADD(month, -1, GETDATE()), DATEADD(month, 3, GETDATE()), 7, 500000.00, 1,
        10000, 10, 500, 3, 30
    );

    INSERT INTO dbo.mssLotteryProgram (
        ID, Type, Number, Name, FromDate, ToDate, ExtendExpiredDay, MinBillValue, ScopeID,
        MaxLotteryCount, MaxLotteryMemberCount, MaxLotteryCountByDay, MaxLotteryMemberCountByDay, TakeExpiredDay
    ) VALUES (
        2, 'FirstTrans', 'PROMO_FIRST', N'Quà tặng chào mừng giao dịch đầu tiên',
        DATEADD(month, -1, GETDATE()), DATEADD(month, 3, GETDATE()), 0, 100000.00, 1,
        5000, 1, 200, 1, 30
    );

    INSERT INTO dbo.mssLotteryProgram (
        ID, Type, Number, Name, FromDate, ToDate, ExtendExpiredDay, MinBillValue, ScopeID,
        MaxLotteryCount, MaxLotteryMemberCount, MaxLotteryCountByDay, MaxLotteryMemberCountByDay, TakeExpiredDay
    ) VALUES (
        3, 'Gift', 'PROMO_REGULAR', N'Quà tặng tri ân mua sắm Gigamall',
        DATEADD(month, -1, GETDATE()), DATEADD(month, 3, GETDATE()), 0, 300000.00, 1,
        5000, 5, 200, 2, 30
    );
    SET IDENTITY_INSERT dbo.mssLotteryProgram OFF;
END
GO

-- Mẫu 5 vé số may mắn sẵn sàng cấp
IF NOT EXISTS (SELECT 1 FROM dbo.mssLotteryGift WHERE ProgramID = 1)
BEGIN
    INSERT INTO dbo.mssLotteryGift (ID, ProgramID, LotteryCode, Name, GiftValue, IsEnable, CreatedOn, CreatedBy)
    VALUES 
        (NEWID(), 1, 'LOTT-998811', N'Vé quay số may mắn giải đặc biệt', 50000000, 1, GETDATE(), 'POC_SEED'),
        (NEWID(), 1, 'LOTT-998812', N'Vé quay số may mắn giải nhất', 10000000, 1, GETDATE(), 'POC_SEED'),
        (NEWID(), 1, 'LOTT-998813', N'Vé quay số may mắn giải nhì', 5000000, 1, GETDATE(), 'POC_SEED'),
        (NEWID(), 1, 'LOTT-998814', N'Vé quay số may mắn giải khuyến khích', 500000, 1, GETDATE(), 'POC_SEED'),
        (NEWID(), 1, 'LOTT-998815', N'Vé quay số may mắn giải khuyến khích', 500000, 1, GETDATE(), 'POC_SEED');
END
GO

-- Mẫu 5 voucher khuyến mãi sẵn sàng cấp
IF NOT EXISTS (SELECT 1 FROM dbo.mssPromotionGift WHERE ProgramID IN (2, 3))
BEGIN
    INSERT INTO dbo.mssPromotionGift (ID, ProgramID, PromotionCode, Name, ItemType, GiftValue, IsEnable, CreatedOn, CreatedBy)
    VALUES 
        (NEWID(), 2, 'FIRST-VOUCHER-50K', N'Voucher 50.000đ giao dịch đầu tiên', 'Voucher', 50000, 1, GETDATE(), 'POC_SEED'),
        (NEWID(), 2, 'FIRST-VOUCHER-50K-2', N'Voucher 50.000đ giao dịch đầu tiên', 'Voucher', 50000, 1, GETDATE(), 'POC_SEED'),
        (NEWID(), 3, 'GIFT-VOUCHER-20K', N'Voucher mua sắm 20.000đ', 'Voucher', 20000, 1, GETDATE(), 'POC_SEED'),
        (NEWID(), 3, 'GIFT-VOUCHER-20K-2', N'Voucher mua sắm 20.000đ', 'Voucher', 20000, 1, GETDATE(), 'POC_SEED'),
        (NEWID(), 3, 'GIFT-VOUCHER-100K', N'Voucher mua sắm 100.000đ', 'Voucher', 100000, 1, GETDATE(), 'POC_SEED');
END
GO
