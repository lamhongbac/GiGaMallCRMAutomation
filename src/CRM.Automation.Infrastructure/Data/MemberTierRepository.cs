using System.Data;
using CRM.Automation.Core.Interfaces;
using CRM.Automation.Core.Models.DTOs;
using Dapper;

namespace CRM.Automation.Infrastructure.Data;

public class MemberTierRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public MemberTierRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Gọi đúng Stored Procedure gốc trong DB: [GetMembersToUpClass]
    /// </summary>
    public async Task<List<MemberUpClassCandidateDto>> GetMembersToUpClassAsync(CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        var result = await db.QueryAsync<MemberUpClassCandidateDto>(
            new CommandDefinition("GetMembersToUpClass", commandType: CommandType.StoredProcedure, cancellationToken: ct)
        );
        return result.AsList();
    }

    /// <summary>
    /// Lấy thông tin chi tiết hạng thẻ từ bảng [dbo].[G_MemberClass]
    /// </summary>
    public async Task<MemberClassInfoDto?> GetMemberClassInfoAsync(string classNo, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT TOP 1 
                ClassNo, 
                ClassName, 
                NextClassNo, 
                PreClassNo, 
                RewardRate AS RewardPoint, 
                ReviewRate AS RankingPoint
            FROM dbo.G_MemberClass WITH (NOLOCK)
            WHERE ClassNo = @ClassNo";

        return await db.QueryFirstOrDefaultAsync<MemberClassInfoDto>(
            new CommandDefinition(sql, new { ClassNo = classNo }, cancellationToken: ct)
        );
    }

    /// <summary>
    /// Thực thi nâng hạng thẻ trong Database bọc trong Transaction (ACID)
    /// Tái hiện 100% logic gốc của BaseCardTrans.Upgrade và MemClassLedgerDataPortal.Upgrade
    /// </summary>
    public async Task<UpClassResultDto> ExecuteUpgradeTierAsync(
        MemberUpClassCandidateDto candidate, 
        MemberClassInfoDto targetClass, 
        CancellationToken ct = default)
    {
        var result = new UpClassResultDto
        {
            MemID = candidate.MemID,
            CardNo = candidate.CardNo ?? string.Empty,
            NewClassNo = targetClass.ClassNo,
            NewClassName = targetClass.ClassName,
            CloseAmt = candidate.CloseAmt ?? 0
        };

        using var db = _connectionFactory.CreateConnection();
        if (db.State != ConnectionState.Open)
            db.Open();

        using var trans = db.BeginTransaction();
        try
        {
            // 1. Tìm bản ghi MemClassLedger hiện tại đang hoạt động (Status = 'O')
            const string checkLedgerSql = @"
                SELECT TOP 1 ID, CLPID, ClassNo, OpenAmt, DebitAmt, CreditAmt, CloseAmt 
                FROM dbo.mssMemClassLedger WITH (UPDLOCK, ROWLOCK) 
                WHERE MemID = @MemId AND Status = 'O'
                ORDER BY ID DESC";

            var currentLedger = await db.QueryFirstOrDefaultAsync<CurrentLedgerRow>(
                new CommandDefinition(checkLedgerSql, new { MemId = candidate.MemID }, transaction: trans, cancellationToken: ct)
            );

            int clpId = currentLedger?.CLPID ?? 1;
            decimal openAmt = currentLedger?.CloseAmt ?? (candidate.CloseAmt ?? 0);
            result.OldClassNo = currentLedger?.ClassNo ?? string.Empty;

            // 2. Đóng bản ghi hạng cũ (Status = 'C')
            if (currentLedger != null)
            {
                int oldId = currentLedger.ID;
                const string closeOldSql = @"
                    UPDATE dbo.mssMemClassLedger 
                    SET Status = 'C', ModifiedOn = GETDATE(), ModifiedBy = 'System' 
                    WHERE ID = @OldId";

                await db.ExecuteAsync(
                    new CommandDefinition(closeOldSql, new { OldId = oldId }, transaction: trans, cancellationToken: ct)
                );
            }

            // 3. Tính toán số dư ban đầu cho hạng thẻ mới (cộng RankingPoint nếu có)
            decimal newOpenAmt = openAmt;
            string notes = "UpMemberClass";
            if (targetClass.RankingPoint.HasValue && targetClass.RankingPoint > 0)
            {
                newOpenAmt += targetClass.RankingPoint.Value;
                notes += $". For Ranking Point: {targetClass.RankingPoint.Value}";
            }
            decimal newCloseAmt = newOpenAmt;

            // 4. Tạo bản ghi MemClassLedger mới với Status = 'O'
            const string insertNewLedgerSql = @"
                INSERT INTO dbo.mssMemClassLedger (
                    EventDate, CLPID, MemID, ClassNo, OpenAmt, DebitAmt, CreditAmt, CloseAmt,
                    Unit, Notes, DocType, ReviewFlag, Status, CreatedBy, CreatedOn
                ) VALUES (
                    GETDATE(), @CLPID, @MemId, @ClassNo, @OpenAmt, 0, 0, @CloseAmt,
                    'P', @Notes, 'R', 'Y', 'O', 'System', GETDATE()
                )";

            await db.ExecuteAsync(new CommandDefinition(insertNewLedgerSql, new
            {
                CLPID = clpId,
                MemId = candidate.MemID,
                ClassNo = targetClass.ClassNo,
                OpenAmt = newOpenAmt,
                CloseAmt = newCloseAmt,
                Notes = notes
            }, transaction: trans, cancellationToken: ct));

            // 5. Nếu có điểm thưởng thăng hạng (RewardPoint) -> Cộng điểm vào mssCLMLedger & ghi mssCLMGiftPoint
            if (targetClass.RewardPoint.HasValue && targetClass.RewardPoint > 0)
            {
                decimal rewardPoint = targetClass.RewardPoint.Value;
                result.BonusRewardPoints = rewardPoint;

                const string updatePointsSql = @"
                    UPDATE dbo.mssCLMLedger 
                    SET AccPoints = AccPoints + @RewardPoint, ModifiedOn = GETDATE(), ModifiedBy = 'System' 
                    WHERE MemID = @MemId";

                await db.ExecuteAsync(new CommandDefinition(updatePointsSql, new { MemId = candidate.MemID, RewardPoint = rewardPoint }, transaction: trans, cancellationToken: ct));

                const string insertGiftPointSql = @"
                    INSERT INTO dbo.mssCLMGiftPoint (
                        ID, MemID, RewardPoint, CreatedBy, CreatedOn, Description
                    ) VALUES (
                        NEWID(), @MemId, @RewardPoint, 'System', GETDATE(), N'Thưởng điểm nâng hạng thẻ ' + @ClassName
                    )";

                await db.ExecuteAsync(new CommandDefinition(insertGiftPointSql, new { MemId = candidate.MemID, RewardPoint = rewardPoint, ClassName = targetClass.ClassName }, transaction: trans, cancellationToken: ct));
            }

            trans.Commit();
            result.Success = true;
            return result;
        }
        catch (Exception ex)
        {
            trans.Rollback();
            result.Success = false;
            result.ErrorMessage = ex.Message;
            return result;
        }
    }

    /// <summary>
    /// Báo cáo lịch sử nâng hạng thẻ hội viên
    /// </summary>
    public async Task<List<UpClassHistoryReportDto>> GetUpClassHistoryReportAsync(
        DateTime? fromDate, 
        DateTime? toDate, 
        string? memId = null, 
        string? mobile = null, 
        CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT 
                ROW_NUMBER() OVER (ORDER BY mcl.EventDate DESC) AS OrderNo,
                mcl.MemID,
                c.CardNo,
                c.FullName,
                c.Mobile,
                mcl.ClassNo,
                ISNULL(gc.ClassName, mcl.ClassNo) AS ClassName,
                mcl.OpenAmt,
                mcl.CloseAmt,
                mcl.EventDate,
                mcl.Notes
            FROM dbo.mssMemClassLedger mcl WITH (NOLOCK)
            LEFT JOIN dbo.mssCustomer c WITH (NOLOCK) ON mcl.MemID = c.MemID
            LEFT JOIN dbo.G_MemberClass gc WITH (NOLOCK) ON mcl.ClassNo = gc.ClassNo
            WHERE mcl.Notes LIKE '%UpMemberClass%'
              AND (@FromDate IS NULL OR mcl.EventDate >= @FromDate)
              AND (@ToDate IS NULL OR mcl.EventDate <= @ToDate)
              AND (@MemId IS NULL OR mcl.MemID = @MemId)
              AND (@Mobile IS NULL OR c.Mobile LIKE '%' + @Mobile + '%')
            ORDER BY mcl.EventDate DESC";

        var list = await db.QueryAsync<UpClassHistoryReportDto>(
            new CommandDefinition(sql, new { FromDate = fromDate, ToDate = toDate, MemId = memId, Mobile = mobile }, cancellationToken: ct)
        );
        return list.AsList();
    }
    private class CurrentLedgerRow
    {
        public int ID { get; set; }
        public int CLPID { get; set; }
        public string ClassNo { get; set; } = string.Empty;
        public decimal? CloseAmt { get; set; }
    }
}