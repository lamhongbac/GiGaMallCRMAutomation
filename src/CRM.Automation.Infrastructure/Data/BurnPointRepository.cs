using System.Data;
using CRM.Automation.Core.Interfaces;
using CRM.Automation.Core.Models.DTOs;
using Dapper;

namespace CRM.Automation.Infrastructure.Data;

public class BurnPointRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public BurnPointRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Gọi đúng Stored Procedure gốc trong CSDL SQL Server: [GetMembersToRemindBurnPoint]
    /// </summary>
    public async Task<List<MemberBurnPointDto>> GetMembersToRemindBurnPointAsync(int year, int days, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@Year", year, DbType.Int32);
        parameters.Add("@Days", days, DbType.Int32);

        var result = await db.QueryAsync<MemberBurnPointDto>(
            new CommandDefinition("GetMembersToRemindBurnPoint", parameters, commandType: CommandType.StoredProcedure, cancellationToken: ct)
        );
        return result.AsList();
    }

    /// <summary>
    /// Lưu vào đúng bảng CSDL gốc: [dbo].[mssBurnPoint]
    /// </summary>
    public async Task<int> BulkCreateBurnPointsAsync(IEnumerable<BurnPointRecordDto> records, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO dbo.mssBurnPoint (
                ID, MemID, LastTransDate, ExpectedBurnDate, ExpiredDate, 
                Point, BurnType, IsReminderSend, BurnStatus, IsBurn, IsCancel, 
                CreatedOn, CreatedBy, ModifiedOn, ModifiedBy
            ) VALUES (
                @ID, @MemID, @LastTransDate, @ExpectedBurnDate, @ExpiredDate, 
                @Point, @BurnType, @IsReminderSend, @BurnStatus, @IsBurn, @IsCancel, 
                GETDATE(), 'system', GETDATE(), 'system'
            )";

        return await db.ExecuteAsync(new CommandDefinition(sql, records, cancellationToken: ct));
    }

    /// <summary>
    /// Đọc dữ liệu từ bảng [dbo].[mssBurnPoint]
    /// </summary>
    public async Task<List<BurnPointRecordDto>> GetPendingBurnPointsAsync(DateTime expiredThreshold, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT ID, MemID, LastTransDate, ExpectedBurnDate, ExpiredDate, Point, BurnType, IsReminderSend, ReminderSendDate, BurnStatus, IsBurn, BurnDate, IsCancel, Description
            FROM dbo.mssBurnPoint WITH (NOLOCK)
            WHERE ExpiredDate <= @ExpiredThreshold AND BurnStatus = 'Pending'";

        var result = await db.QueryAsync<BurnPointRecordDto>(
            new CommandDefinition(sql, new { ExpiredThreshold = expiredThreshold }, cancellationToken: ct)
        );
        return result.AsList();
    }

    /// <summary>
    /// Lấy AppID (FCM Device Token) của Member từ [dbo].[mssMobileTracking]
    /// </summary>
    public async Task<string?> GetMemberDeviceTokenAsync(string memId, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT TOP 1 AppID 
            FROM dbo.mssMobileTracking WITH (NOLOCK) 
            WHERE MemID = @MemId AND AppID IS NOT NULL AND AppID <> ''";

        return await db.QueryFirstOrDefaultAsync<string>(
            new CommandDefinition(sql, new { MemId = memId }, cancellationToken: ct)
        );
    }

    /// <summary>
    /// Đọc mẫu tin thông báo từ bảng [dbo].[G_NotificationMessage] theo Event Name (Number)
    /// </summary>
    public async Task<(string? Subject, string? Content)?> GetNotificationTemplateAsync(string number, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT TOP 1 Subject, Content 
            FROM dbo.G_NotificationMessage WITH (NOLOCK) 
            WHERE Number = @Number";

        return await db.QueryFirstOrDefaultAsync<(string? Subject, string? Content)?>(
            new CommandDefinition(sql, new { Number = number }, cancellationToken: ct)
        );
    }

    /// <summary>
    /// Lưu thông báo vào hộp thư tin nhắn của Member trong bảng [dbo].[mssNotification]
    /// </summary>
    public async Task<int> CreateInAppNotificationAsync(
        string memId, string subject, string content, 
        string eventName = "RemindExpiredPoint",
        string screenName = "RewardHistoryDetail", 
        string functionName = "GetRewardHistories", 
        DateTime? expiresDate = null, 
        CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO dbo.mssNotification (
                MemID, Subject, Description, Content, ScreenName, FunctionName,
                EventName, DisplayStyle, CreatedOn, ExpiresDate, IsSendNotification
            ) VALUES (
                @MemId, @Subject, @Description, @Content, @ScreenName, @FunctionName,
                @EventName, 'Information', GETDATE(), @ExpiresDate, 1
            )";

        return await db.ExecuteAsync(new CommandDefinition(sql, new
        {
            MemId = memId,
            Subject = subject,
            Description = content,
            Content = content,
            EventName = eventName,
            ScreenName = screenName,
            FunctionName = functionName,
            ExpiresDate = expiresDate ?? DateTime.Today.AddMonths(1)
        }, cancellationToken: ct));
    }

    /// <summary>
    /// Đọc cấu hình Gateway gửi notification đang lưu trong bảng [dbo].[G_Setting] (Group = 'Notification')
    /// </summary>
    public async Task<Dictionary<string, string>> GetNotificationGatewaySettingsAsync(CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT [Key], [Value] 
            FROM dbo.G_Setting WITH (NOLOCK) 
            WHERE [Group] = 'Notification'";

        var rows = await db.QueryAsync<(string Key, string Value)>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Truy vấn giao dịch thẻ từ bảng [dbo].[mssCardTrans]
    /// </summary>
    public async Task<CardTransDto?> GetLastTransactionAsync(string memId, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT TOP 1 MemID, TransDate, TotalAmount AS Amount 
            FROM dbo.mssCardTrans WITH (NOLOCK)
            WHERE MemID = @MemId 
            ORDER BY TransDate DESC";

        return await db.QueryFirstOrDefaultAsync<CardTransDto>(
            new CommandDefinition(sql, new { MemId = memId }, cancellationToken: ct)
        );
    }

    /// <summary>
    /// Cập nhật trạng thái thông báo trong bảng [dbo].[mssBurnPoint]
    /// </summary>
    public async Task UpdateReminderSentAsync(Guid id, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE dbo.mssBurnPoint 
            SET IsReminderSend = 1, ReminderSendDate = GETDATE(), ModifiedOn = GETDATE(), ModifiedBy = 'system'
            WHERE ID = @Id";

        await db.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    /// <summary>
    /// Cập nhật trạng thái hủy hoặc thực thi điểm trong bảng [dbo].[mssBurnPoint]
    /// </summary>
    public async Task UpdateBurnStatusAsync(Guid id, string status, bool isBurn, bool isCancel, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE dbo.mssBurnPoint 
            SET BurnStatus = @Status, 
                IsBurn = @IsBurn, 
                BurnDate = CASE WHEN @IsBurn = 1 THEN GETDATE() ELSE BurnDate END,
                IsCancel = @IsCancel,
                ModifiedOn = GETDATE(), 
                ModifiedBy = 'system'
            WHERE ID = @Id";

        await db.ExecuteAsync(new CommandDefinition(sql, new { Id = id, Status = status, IsBurn = isBurn, IsCancel = isCancel }, cancellationToken: ct));
    }

    /// <summary>
    /// Thực thi trừ điểm hết hạn trong [dbo].[mssCLMLedger] và tạo bản ghi lịch sử [dbo].[mssCLMGiftPoint]
    /// Tái hiện 100% logic nghiệp vụ gốc của BaseCardTrans.BurnPoint(memID)
    /// </summary>
    public async Task<decimal> ExecuteBurnPointsAsync(string memId, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        if (db.State != ConnectionState.Open)
            db.Open();

        using var trans = db.BeginTransaction();
        try
        {
            // 1. Đọc số dư AccPoints và RedeemPoints hiện tại trong mssCLMLedger
            const string checkSql = @"
                SELECT TOP 1 AccPoints, RedeemPoints 
                FROM dbo.mssCLMLedger WITH (UPDLOCK, ROWLOCK) 
                WHERE MemID = @MemId";

            var ledger = await db.QueryFirstOrDefaultAsync<(decimal AccPoints, decimal RedeemPoints)?>(
                new CommandDefinition(checkSql, new { MemId = memId }, transaction: trans, cancellationToken: ct)
            );

            if (ledger == null)
            {
                trans.Commit();
                return 0;
            }

            decimal expiredPoint = ledger.Value.AccPoints - ledger.Value.RedeemPoints;
            if (expiredPoint > 0)
            {
                // 2. Trừ điểm tích lũy AccPoints
                const string updateLedgerSql = @"
                    UPDATE dbo.mssCLMLedger 
                    SET AccPoints = AccPoints - @ExpiredPoint, 
                        ModifiedBy = 'system', 
                        ModifiedOn = GETDATE() 
                    WHERE MemID = @MemId";

                await db.ExecuteAsync(
                    new CommandDefinition(updateLedgerSql, new { MemId = memId, ExpiredPoint = expiredPoint }, transaction: trans, cancellationToken: ct)
                );

                // 3. Ghi nhận giao dịch giảm điểm âm trong mssCLMGiftPoint
                const string insertGiftSql = @"
                    INSERT INTO dbo.mssCLMGiftPoint (
                        ID, MemID, RewardPoint, CreatedBy, CreatedOn, Description
                    ) VALUES (
                        NEWID(), @MemId, -@ExpiredPoint, 'system', GETDATE(), N'Điểm hết hạn do không phát sinh giao dịch'
                    )";

                await db.ExecuteAsync(
                    new CommandDefinition(insertGiftSql, new { MemId = memId, ExpiredPoint = expiredPoint }, transaction: trans, cancellationToken: ct)
                );
            }

            trans.Commit();
            return expiredPoint;
        }
        catch
        {
            trans.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Gọi Stored Procedure báo cáo gốc trong DB: [GetBurnPointReport] @FromDate, @ToDate
    /// </summary>
    public async Task<List<BurnPointReportDto>> GetBurnPointReportAsync(DateTime? fromDate, DateTime? toDate, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@FromDate", fromDate, DbType.DateTime);
        parameters.Add("@ToDate", toDate, DbType.DateTime);

        var result = await db.QueryAsync<BurnPointReportDto>(
            new CommandDefinition("GetBurnPointReport", parameters, commandType: CommandType.StoredProcedure, cancellationToken: ct)
        );
        return result.AsList();
    }

    /// <summary>
    /// Báo cáo thống kê tổng hợp hiệu quả chiến dịch cứu điểm vs hủy điểm
    /// </summary>
    public async Task<BurnPointSummaryDto> GetBurnPointSummaryAsync(DateTime? fromDate, DateTime? toDate, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT 
                COUNT(*) AS TotalRemindedMembers,
                ISNULL(SUM(Point), 0) AS TotalRemindedPoints,
                ISNULL(SUM(CASE WHEN IsCancel = 1 OR BurnStatus = 'Cancel' THEN 1 ELSE 0 END), 0) AS TotalSavedMembers,
                ISNULL(SUM(CASE WHEN IsCancel = 1 OR BurnStatus = 'Cancel' THEN Point ELSE 0 END), 0) AS TotalSavedPoints,
                ISNULL(SUM(CASE WHEN IsBurn = 1 OR BurnStatus = 'Burned' THEN 1 ELSE 0 END), 0) AS TotalBurnedMembers,
                ISNULL(SUM(CASE WHEN IsBurn = 1 OR BurnStatus = 'Burned' THEN Point ELSE 0 END), 0) AS TotalBurnedPoints
            FROM dbo.mssBurnPoint WITH (NOLOCK)
            WHERE (@FromDate IS NULL OR CreatedOn >= @FromDate)
              AND (@ToDate IS NULL OR CreatedOn <= @ToDate)";

        var summary = await db.QueryFirstOrDefaultAsync<BurnPointSummaryDto>(
            new CommandDefinition(sql, new { FromDate = fromDate, ToDate = toDate }, cancellationToken: ct)
        );

        summary ??= new BurnPointSummaryDto();
        summary.FromDate = fromDate;
        summary.ToDate = toDate;
        return summary;
    }

    /// <summary>
    /// Báo cáo chi tiết các giao dịch trừ điểm hết hạn thực tế của hội viên (BurnStatus = 'Burned' / IsBurn = 1)
    /// </summary>
    public async Task<List<ResetPointDetailReportDto>> GetResetPointReportAsync(
        DateTime? fromDate, 
        DateTime? toDate, 
        string? memId = null, 
        string? mobile = null, 
        CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT 
                ROW_NUMBER() OVER (ORDER BY bp.BurnDate DESC, bp.CreatedOn DESC) AS OrderNo,
                bp.MemID,
                c.CardNo,
                c.FullName,
                c.Mobile,
                c.Email,
                bp.Point AS BurnedPoints,
                bp.BurnDate,
                bp.ExpiredDate,
                bp.LastTransDate,
                bp.ReminderSendDate,
                bp.BurnStatus,
                bp.CreatedOn AS ReminderBatchDate
            FROM dbo.mssBurnPoint bp WITH (NOLOCK)
            LEFT JOIN dbo.mssCustomer c WITH (NOLOCK) ON bp.MemID = c.MemID
            WHERE (bp.BurnStatus = 'Burned' OR bp.IsBurn = 1)
              AND (@FromDate IS NULL OR bp.BurnDate >= @FromDate)
              AND (@ToDate IS NULL OR bp.BurnDate <= @ToDate)
              AND (@MemId IS NULL OR bp.MemID = @MemId)
              AND (@Mobile IS NULL OR c.Mobile LIKE '%' + @Mobile + '%')
            ORDER BY bp.BurnDate DESC";

        var result = await db.QueryAsync<ResetPointDetailReportDto>(
            new CommandDefinition(sql, new { FromDate = fromDate, ToDate = toDate, MemId = memId, Mobile = mobile }, cancellationToken: ct)
        );
        return result.AsList();
    }

    /// <summary>
    /// Báo cáo thống kê tổng hợp số liệu trừ điểm hết hạn
    /// </summary>
    public async Task<ResetPointSummaryDto> GetResetPointSummaryAsync(
        DateTime? fromDate, 
        DateTime? toDate, 
        CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT 
                COUNT(*) AS TotalMembersBurned,
                ISNULL(SUM(Point), 0) AS TotalPointsBurned,
                ISNULL(AVG(Point), 0) AS AveragePointsBurned,
                ISNULL(MAX(Point), 0) AS MaxPointsBurned,
                ISNULL(MIN(Point), 0) AS MinPointsBurned
            FROM dbo.mssBurnPoint WITH (NOLOCK)
            WHERE (BurnStatus = 'Burned' OR IsBurn = 1)
              AND (@FromDate IS NULL OR BurnDate >= @FromDate)
              AND (@ToDate IS NULL OR BurnDate <= @ToDate)";

        var summary = await db.QueryFirstOrDefaultAsync<ResetPointSummaryDto>(
            new CommandDefinition(sql, new { FromDate = fromDate, ToDate = toDate }, cancellationToken: ct)
        );

        summary ??= new ResetPointSummaryDto();
        summary.FromDate = fromDate;
        summary.ToDate = toDate;
        return summary;
    }
}