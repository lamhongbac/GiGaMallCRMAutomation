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

    public async Task<List<MemberBurnPointDto>> GetMembersToRemindBurnPointAsync(int year, int days, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@year", year);
        parameters.Add("@days", days);

        var result = await db.QueryAsync<MemberBurnPointDto>(
            new CommandDefinition("SP_GetMembersToRemindBurnPoint", parameters, commandType: CommandType.StoredProcedure, cancellationToken: ct)
        );
        return result.AsList();
    }

    public async Task<int> BulkCreateBurnPointsAsync(IEnumerable<BurnPointRecordDto> records, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO BurnPoint (
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

    public async Task<List<BurnPointRecordDto>> GetPendingBurnPointsAsync(DateTime expiredThreshold, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT ID, MemID, LastTransDate, ExpectedBurnDate, ExpiredDate, Point, BurnType, IsReminderSend, ReminderSendDate, BurnStatus, IsBurn, BurnDate, IsCancel
            FROM BurnPoint WITH (NOLOCK)
            WHERE ExpiredDate <= @ExpiredThreshold AND BurnStatus = 'Pending'";

        var result = await db.QueryAsync<BurnPointRecordDto>(
            new CommandDefinition(sql, new { ExpiredThreshold = expiredThreshold }, cancellationToken: ct)
        );
        return result.AsList();
    }

    public async Task<CardTransDto?> GetLastTransactionAsync(string memId, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT TOP 1 MemID, TransDate, Amount 
            FROM CardTrans WITH (NOLOCK)
            WHERE MemID = @MemId 
            ORDER BY TransDate DESC";

        return await db.QueryFirstOrDefaultAsync<CardTransDto>(
            new CommandDefinition(sql, new { MemId = memId }, cancellationToken: ct)
        );
    }

    public async Task UpdateReminderSentAsync(Guid id, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE BurnPoint 
            SET IsReminderSend = 1, ReminderSendDate = GETDATE(), ModifiedOn = GETDATE(), ModifiedBy = 'system'
            WHERE ID = @Id";

        await db.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task UpdateBurnStatusAsync(Guid id, string status, bool isBurn, bool isCancel, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE BurnPoint 
            SET BurnStatus = @Status, 
                IsBurn = @IsBurn, 
                BurnDate = CASE WHEN @IsBurn = 1 THEN GETDATE() ELSE BurnDate END,
                IsCancel = @IsCancel,
                ModifiedOn = GETDATE(), 
                ModifiedBy = 'system'
            WHERE ID = @Id";

        await db.ExecuteAsync(new CommandDefinition(sql, new { Id = id, Status = status, IsBurn = isBurn, IsCancel = isCancel }, cancellationToken: ct));
    }

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
