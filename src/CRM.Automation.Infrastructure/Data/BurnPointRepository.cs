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
    /// (Tương đương BaseMobileTracking.GetAppID(memID) ở code cũ)
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
    /// Lưu thông báo vào hộp thư tin nhắn của Member trong bảng [dbo].[mssNotification]
    /// (Tương đương Notification.Create(...) ở code cũ)
    /// </summary>
    public async Task<int> CreateInAppNotificationAsync(
        string memId, string subject, string content, 
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
                'RemindExpiredPoint', 'Information', GETDATE(), @ExpiresDate, 1
            )";

        return await db.ExecuteAsync(new CommandDefinition(sql, new
        {
            MemId = memId,
            Subject = subject,
            Description = content,
            Content = content,
            ScreenName = screenName,
            FunctionName = functionName,
            ExpiresDate = expiresDate ?? DateTime.Today.AddMonths(1)
        }, cancellationToken: ct));
    }

    /// <summary>
    /// Đọc cấu hình Gateway gửi notification đang lưu trong bảng [dbo].[G_Setting] (Group = 'Notification')
    /// (Tương đương SettingHandler.GetSetting ở code cũ)
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
    /// Gọi logic thực thi hủy điểm tương đương BaseCardTrans.BurnPoint(memID)
    /// </summary>
    public async Task ExecuteBurnPointsAsync(string memId, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@MemID", memId);

        await db.ExecuteAsync(
            new CommandDefinition("SP_BurnMemberPoints", parameters, commandType: CommandType.StoredProcedure, cancellationToken: ct)
        );
    }
}
