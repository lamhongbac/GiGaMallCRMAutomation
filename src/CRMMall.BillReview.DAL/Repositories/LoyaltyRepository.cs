using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using CRMMall.BillReview.DAL.Data;
using CRMMall.BillReview.DAL.Entities;

namespace CRMMall.BillReview.DAL.Repositories
{
    public class LoyaltyRepository : ILoyaltyRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public LoyaltyRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<ClmLedgerRecord?> GetClmLedgerAsync(string memId)
        {
            using var conn = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT ID, CLPID, MemID, AccSales, AccPoints, RedeemPoints, AccTC, LastSaleDate, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy
                FROM dbo.mssCLMLedger WITH (NOLOCK)
                WHERE MemID = @MemID";
            return await conn.QueryFirstOrDefaultAsync<ClmLedgerRecord>(sql, new { MemID = memId });
        }

        public async Task<MemberClassLedgerRecord?> GetActiveMemberClassAsync(string memId)
        {
            using var conn = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT TOP 1 ID, EventDate, CLPID, MemID, ClassNo, OpenAmt, DebitAmt, CreditAmt, CloseAmt,
                             Unit, Notes, DocType, ReviewFlag, Status, LastSaleDocID, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn
                FROM dbo.mssMemClassLedger WITH (NOLOCK)
                WHERE MemID = @MemID AND Status = 'O'
                ORDER BY ID DESC";
            return await conn.QueryFirstOrDefaultAsync<MemberClassLedgerRecord>(sql, new { MemID = memId });
        }

        public async Task<MemberClassInfo?> GetMemberClassAsync(string classNo)
        {
            using var conn = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT ID, CLPID, ClassNo, ClassName, ClassOrder, RewardRate, ReviewRate, IsMember
                FROM dbo.G_MemberClass WITH (NOLOCK)
                WHERE ClassNo = @ClassNo";
            return await conn.QueryFirstOrDefaultAsync<MemberClassInfo>(sql, new { ClassNo = classNo });
        }

        public async Task<BasePointRate> GetPointRateAsync(string classNo, decimal billTotal, DateTime transDate, string outletNumber)
        {
            using var conn = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT RewardRate, ReviewRate
                FROM dbo.G_MemberClass WITH (NOLOCK)
                WHERE ClassNo = @ClassNo";
            
            var classInfo = await conn.QueryFirstOrDefaultAsync<MemberClassInfo>(sql, new { ClassNo = classNo });
            
            var pointRate = new BasePointRate();
            if (classInfo != null)
            {
                pointRate.RewardPointRate = classInfo.RewardRate > 0 ? classInfo.RewardRate : 10000m;
                pointRate.RankingPointRate = classInfo.ReviewRate > 0 ? classInfo.ReviewRate : 10000m;
            }
            return pointRate;
        }

        public async Task<bool> RecordSaleAndPointsTransactionAsync(
            CardTransRecord cardTrans,
            decimal rewardPoints,
            decimal rankPoints,
            decimal billTotal,
            DateTime billDateTime,
            string reviewBy)
        {
            using var conn = _connectionFactory.CreateConnection();
            conn.Open();
            using var transaction = conn.BeginTransaction();
            try
            {
                // 1. Thêm bản ghi giao dịch mua sắm (mssCardTrans)
                const string sqlCardTrans = @"
                    INSERT INTO dbo.mssCardTrans (
                        DocID, CLPID, MemID, CardNo, ClassNo, OrderID, BillNo, CheckNo, OutletNumber,
                        TransDate, POSBusinessDate, TransType, SubTotal, TotalBillValue,
                        RPointGain, RPointRate, ReviewPointGain, ReviewPointRate,
                        CashierName, UseMobileApp, BProcess, Notes, CreatedBy, CreatedOn
                    ) VALUES (
                        @DocID, @CLPID, @MemID, @CardNo, @ClassNo, @OrderID, @BillNo, @CheckNo, @OutletNumber,
                        @TransDate, @POSBusinessDate, @TransType, @SubTotal, @TotalBillValue,
                        @RPointGain, @RPointRate, @ReviewPointGain, @ReviewPointRate,
                        @CashierName, @UseMobileApp, @BProcess, @Notes, @CreatedBy, @CreatedOn
                    );";
                await conn.ExecuteAsync(sqlCardTrans, cardTrans, transaction);

                // 2. Cập nhật ví điểm hội viên (mssCLMLedger)
                const string sqlClmLedger = @"
                    IF EXISTS (SELECT 1 FROM dbo.mssCLMLedger WITH (UPDLOCK, ROWLOCK) WHERE MemID = @MemID)
                    BEGIN
                        UPDATE dbo.mssCLMLedger
                        SET AccSales = ISNULL(AccSales, 0) + @BillTotal,
                            AccPoints = ISNULL(AccPoints, 0) + @RewardPoints,
                            AccTC = ISNULL(AccTC, 0) + 1,
                            LastSaleDate = @LastSaleDate,
                            ModifiedOn = GETDATE(),
                            ModifiedBy = @ReviewBy
                        WHERE MemID = @MemID;
                    END
                    ELSE
                    BEGIN
                        INSERT INTO dbo.mssCLMLedger (
                            CLPID, MemID, AccSales, AccPoints, RedeemPoints, AccTC, LastSaleDate, CreatedOn, CreatedBy
                        ) VALUES (
                            1, @MemID, @BillTotal, @RewardPoints, 0, 1, @LastSaleDate, GETDATE(), @ReviewBy
                        );
                    END";

                await conn.ExecuteAsync(sqlClmLedger, new {
                    MemID = cardTrans.MemID,
                    BillTotal = billTotal,
                    RewardPoints = rewardPoints,
                    LastSaleDate = billDateTime,
                    ReviewBy = reviewBy
                }, transaction);

                // 3. Cập nhật sổ cái xếp hạng thành viên (mssMemClassLedger)
                const string sqlMemClassLedger = @"
                    UPDATE dbo.mssMemClassLedger
                    SET DebitAmt = ISNULL(DebitAmt, 0) + @RankPoints,
                        CloseAmt = ISNULL(OpenAmt, 0) + (ISNULL(DebitAmt, 0) + @RankPoints) - ISNULL(CreditAmt, 0),
                        DocType = 'S',
                        ReviewFlag = 'N',
                        LastSaleDocID = @CardTransDocID,
                        ModifiedOn = GETDATE(),
                        ModifiedBy = @ReviewBy
                    WHERE MemID = @MemID AND Status = 'O';";

                await conn.ExecuteAsync(sqlMemClassLedger, new {
                    MemID = cardTrans.MemID,
                    RankPoints = rankPoints,
                    CardTransDocID = cardTrans.DocID,
                    ReviewBy = reviewBy
                }, transaction);

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<IEnumerable<MemberClassRange>> GetMemberRangesAsync()
        {
            using var conn = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT ID, CLPID, ClassNo, FromAmt, ToAmt, UpgradeClass, ValidMonths
                FROM dbo.G_MemRangeClass WITH (NOLOCK)
                ORDER BY FromAmt ASC";
            return await conn.QueryAsync<MemberClassRange>(sql);
        }

        public async Task<bool> UpgradeMemberClassAsync(string memId, string newClassNo, decimal bonusPoints, string notes)
        {
            using var conn = _connectionFactory.CreateConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                // Đóng hạng cũ (Status = 'C')
                await conn.ExecuteAsync(@"
                    UPDATE dbo.mssMemClassLedger 
                    SET Status = 'C', Notes = @CloseNotes, ModifiedOn = GETDATE(), ModifiedBy = 'BillReviewService'
                    WHERE MemID = @MemID AND Status = 'O'", 
                    new { MemID = memId, CloseNotes = "Closed due to UpClass to " + newClassNo }, trans);

                // Mở hạng mới (Status = 'O')
                await conn.ExecuteAsync(@"
                    INSERT INTO dbo.mssMemClassLedger (
                        EventDate, CLPID, MemID, ClassNo, OpenAmt, DebitAmt, CreditAmt, CloseAmt,
                        Unit, Notes, DocType, ReviewFlag, Status, CreatedBy, CreatedOn
                    ) VALUES (
                        GETDATE(), 1, @MemID, @NewClassNo, 0, 0, 0, 0,
                        'P', @Notes, 'R', 'Y', 'O', 'BillReviewService', GETDATE()
                    )", new { MemID = memId, NewClassNo = newClassNo, Notes = notes }, trans);

                // Cộng điểm thưởng nâng hạng (nếu có)
                if (bonusPoints > 0)
                {
                    await conn.ExecuteAsync(@"
                        UPDATE dbo.mssCLMLedger
                        SET AccPoints = AccPoints + @BonusPoints,
                            ModifiedOn = GETDATE(),
                            ModifiedBy = 'BillReviewService'
                        WHERE MemID = @MemID;

                        INSERT INTO dbo.mssCLMGiftPoint (ID, CLPID, MemID, RewardPoint, Description, CreatedOn, CreatedBy)
                        VALUES (NEWID(), 1, @MemID, @BonusPoints, @Notes, GETDATE(), 'BillReviewService');
                    ", new { MemID = memId, BonusPoints = bonusPoints, Notes = notes }, trans);
                }

                trans.Commit();
                return true;
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        public async Task<LotteryProgramInfo?> GetEligibleLotteryProgramAsync(
            decimal billAmount, DateTime transDate, DateTime? joinDate, string memId, string outletNumber)
        {
            using var conn = _connectionFactory.CreateConnection();
            const string sqlCandidates = @"
                SELECT ID, Type, Number, Name, FromDate, ToDate, ExtendExpiredDay, MinBillValue, ScopeID,
                       JoinFrom, JoinTo, MaxLotteryCount, MaxLotteryMemberCount, MaxLotteryCountByDay, MaxLotteryMemberCountByDay,
                       RankPoint, RewardPoint, TakeExpiredDay, TakeByBillDate
                FROM dbo.mssLotteryProgram WITH (NOLOCK)
                WHERE Type = 'Lottery' AND (MinBillValue IS NULL OR MinBillValue <= @BillAmount)
                ORDER BY ToDate DESC, FromDate DESC, MinBillValue DESC";

            var candidates = (await conn.QueryAsync<LotteryProgramInfo>(sqlCandidates, new { BillAmount = billAmount })).ToList();

            foreach (var prog in candidates)
            {
                // 1. Kiểm tra khung ngày hiệu lực (kể cả gia hạn ExtendExpiredDay)
                var effectiveToDate = (prog.ToDate ?? DateTime.MaxValue).AddDays(prog.ExtendExpiredDay ?? 0);
                if (prog.FromDate.HasValue && prog.FromDate.Value.Date > transDate.Date) continue;
                if (effectiveToDate.Date < transDate.Date) continue;

                // 2. Kiểm tra ngày gia nhập hội viên
                if (prog.JoinFrom.HasValue && (joinDate == null || joinDate.Value.Date < prog.JoinFrom.Value.Date)) continue;
                if (prog.JoinTo.HasValue && (joinDate == null || joinDate.Value.Date > prog.JoinTo.Value.Date)) continue;

                // 3. Kiểm tra phạm vi cửa hàng áp dụng (ScopeID)
                if (prog.ScopeID.HasValue)
                {
                    const string sqlScope = "SELECT ID, Number, Name, OutletList FROM dbo.G_Scope WITH (NOLOCK) WHERE ID = @ScopeID";
                    var scope = await conn.QueryFirstOrDefaultAsync<ScopeInfo>(sqlScope, new { ScopeID = prog.ScopeID.Value });
                    if (scope != null && !string.IsNullOrWhiteSpace(scope.OutletList))
                    {
                        var outlets = scope.OutletList.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        if (!outlets.Any(o => o.Equals(outletNumber, StringComparison.OrdinalIgnoreCase)))
                        {
                            continue; // Outlet không nằm trong Scope
                        }
                    }
                }

                // 4. Kiểm tra hạn ngạch 3 tầng trên mssLotteryGift
                // Tầng 1: Tổng số vé 1 hội viên được nhận trong toàn chiến dịch
                if (prog.MaxLotteryMemberCount.HasValue && prog.MaxLotteryMemberCount.Value > 0)
                {
                    const string sqlMemberTotal = @"
                        SELECT COUNT(1) FROM dbo.mssLotteryGift WITH (NOLOCK)
                        WHERE MemID = @MemID AND ProgramID = @ProgramID";
                    var memberTotal = await conn.ExecuteScalarAsync<int>(sqlMemberTotal, new { MemID = memId, ProgramID = prog.ID });
                    if (memberTotal >= prog.MaxLotteryMemberCount.Value) continue;
                }

                // Tầng 2: Số vé 1 hội viên được nhận trong 1 ngày
                if (prog.MaxLotteryMemberCountByDay.HasValue && prog.MaxLotteryMemberCountByDay.Value > 0)
                {
                    const string sqlMemberDay = @"
                        SELECT COUNT(1) FROM dbo.mssLotteryGift WITH (NOLOCK)
                        WHERE MemID = @MemID AND ProgramID = @ProgramID AND CAST(TransDate AS DATE) = @TransDate";
                    var memberDay = await conn.ExecuteScalarAsync<int>(sqlMemberDay, new { MemID = memId, ProgramID = prog.ID, TransDate = transDate.Date });
                    if (memberDay >= prog.MaxLotteryMemberCountByDay.Value) continue;
                }

                // Tầng 3: Tổng số vé toàn chương trình phát ra trong 1 ngày
                if (prog.MaxLotteryCountByDay.HasValue && prog.MaxLotteryCountByDay.Value > 0)
                {
                    const string sqlProgDay = @"
                        SELECT COUNT(1) FROM dbo.mssLotteryGift WITH (NOLOCK)
                        WHERE ProgramID = @ProgramID AND CAST(TransDate AS DATE) = @TransDate";
                    var progDay = await conn.ExecuteScalarAsync<int>(sqlProgDay, new { ProgramID = prog.ID, TransDate = transDate.Date });
                    if (progDay >= prog.MaxLotteryCountByDay.Value) continue;
                }

                // Đạt đầy đủ điều kiện
                return prog;
            }

            return null;
        }

        public async Task<LotteryProgramInfo?> GetEligiblePromotionProgramAsync(
            decimal billAmount, DateTime transDate, string memId, string outletNumber)
        {
            using var conn = _connectionFactory.CreateConnection();

            // Kiểm tra xem hội viên đã từng nhận quà khuyến mãi chưa
            const string sqlCheckPrior = "SELECT COUNT(1) FROM dbo.mssPromotionGift WITH (NOLOCK) WHERE MemID = @MemID";
            var priorGiftsCount = await conn.ExecuteScalarAsync<int>(sqlCheckPrior, new { MemID = memId });
            bool isFirstTransEligible = (priorGiftsCount == 0);

            // Truy vấn ứng viên chương trình khuyến mãi
            const string sqlCandidates = @"
                SELECT ID, Type, Number, Name, FromDate, ToDate, ExtendExpiredDay, MinBillValue, ScopeID,
                       JoinFrom, JoinTo, MaxLotteryCount, MaxLotteryMemberCount, MaxLotteryCountByDay, MaxLotteryMemberCountByDay,
                       RankPoint, RewardPoint, TakeExpiredDay, TakeByBillDate
                FROM dbo.mssLotteryProgram WITH (NOLOCK)
                WHERE Type IN ('FirstTrans', 'Gift') AND (MinBillValue IS NULL OR MinBillValue <= @BillAmount)
                ORDER BY 
                    CASE WHEN Type = 'FirstTrans' THEN 1 ELSE 2 END,
                    ToDate DESC, FromDate DESC, MinBillValue DESC";

            var allCandidates = (await conn.QueryAsync<LotteryProgramInfo>(sqlCandidates, new { BillAmount = billAmount })).ToList();

            // Lọc ưu tiên FirstTrans (nếu chưa từng có quà), nếu không thì chọn Gift
            var candidates = allCandidates
                .Where(p => isFirstTransEligible ? (p.Type == "FirstTrans" || p.Type == "Gift") : (p.Type == "Gift"))
                .ToList();

            foreach (var prog in candidates)
            {
                // 1. Kiểm tra khung ngày hiệu lực
                if (prog.FromDate.HasValue && prog.FromDate.Value.Date > transDate.Date) continue;
                if (prog.ToDate.HasValue && prog.ToDate.Value.Date < transDate.Date) continue;

                // 2. Kiểm tra Scope gian hàng
                if (prog.ScopeID.HasValue)
                {
                    const string sqlScope = "SELECT ID, Number, Name, OutletList FROM dbo.G_Scope WITH (NOLOCK) WHERE ID = @ScopeID";
                    var scope = await conn.QueryFirstOrDefaultAsync<ScopeInfo>(sqlScope, new { ScopeID = prog.ScopeID.Value });
                    if (scope != null && !string.IsNullOrWhiteSpace(scope.OutletList))
                    {
                        var outlets = scope.OutletList.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        if (!outlets.Any(o => o.Equals(outletNumber, StringComparison.OrdinalIgnoreCase)))
                        {
                            continue;
                        }
                    }
                }

                // 3. Kiểm tra hạn ngạch 3 tầng trên mssPromotionGift
                if (prog.MaxLotteryMemberCount.HasValue && prog.MaxLotteryMemberCount.Value > 0)
                {
                    const string sqlMemberTotal = @"
                        SELECT COUNT(1) FROM dbo.mssPromotionGift WITH (NOLOCK)
                        WHERE MemID = @MemID AND ProgramID = @ProgramID";
                    var memberTotal = await conn.ExecuteScalarAsync<int>(sqlMemberTotal, new { MemID = memId, ProgramID = prog.ID });
                    if (memberTotal >= prog.MaxLotteryMemberCount.Value) continue;
                }

                if (prog.MaxLotteryMemberCountByDay.HasValue && prog.MaxLotteryMemberCountByDay.Value > 0)
                {
                    const string sqlMemberDay = @"
                        SELECT COUNT(1) FROM dbo.mssPromotionGift WITH (NOLOCK)
                        WHERE MemID = @MemID AND ProgramID = @ProgramID AND CAST(TransDate AS DATE) = @TransDate";
                    var memberDay = await conn.ExecuteScalarAsync<int>(sqlMemberDay, new { MemID = memId, ProgramID = prog.ID, TransDate = transDate.Date });
                    if (memberDay >= prog.MaxLotteryMemberCountByDay.Value) continue;
                }

                if (prog.MaxLotteryCountByDay.HasValue && prog.MaxLotteryCountByDay.Value > 0)
                {
                    const string sqlProgDay = @"
                        SELECT COUNT(1) FROM dbo.mssPromotionGift WITH (NOLOCK)
                        WHERE ProgramID = @ProgramID AND CAST(TransDate AS DATE) = @TransDate";
                    var progDay = await conn.ExecuteScalarAsync<int>(sqlProgDay, new { ProgramID = prog.ID, TransDate = transDate.Date });
                    if (progDay >= prog.MaxLotteryCountByDay.Value) continue;
                }

                return prog;
            }

            return null;
        }

        public async Task<LotteryGiftRecord?> IssueLotteryTicketAsync(
            LotteryProgramInfo program, string memId, string billNo, DateTime transDate, decimal subTotal)
        {
            using var conn = _connectionFactory.CreateConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                // Lấy kho vé số chưa phát
                const string sqlAvailable = @"
                    SELECT TOP 50 ID, ProgramID, LotteryCode, Name, ItemCode, ItemType, GiftValue
                    FROM dbo.mssLotteryGift WITH (UPDLOCK, ROWLOCK)
                    WHERE ProgramID = @ProgramID AND MemID IS NULL AND (IsEnable = 1 OR IsEnable IS NULL)";

                var availableTickets = (await conn.QueryAsync<LotteryGiftRecord>(sqlAvailable, new { ProgramID = program.ID }, trans)).ToList();
                if (!availableTickets.Any())
                {
                    trans.Rollback();
                    return null;
                }

                // Bốc thăm ngẫu nhiên (Weighted Random theo GiftValue)
                var rand = new Random();
                var selectedTicket = availableTickets[rand.Next(availableTickets.Count)];

                // Gán vé cho hội viên và hóa đơn tương ứng
                const string sqlAssign = @"
                    UPDATE dbo.mssLotteryGift
                    SET MemID = @MemID,
                        BillNo = @BillNo,
                        TransDate = @TransDate,
                        SubTotal = @SubTotal,
                        GetCodeDate = GETDATE()
                    WHERE ID = @ID";

                await conn.ExecuteAsync(sqlAssign, new {
                    ID = selectedTicket.ID,
                    MemID = memId,
                    BillNo = billNo,
                    TransDate = transDate,
                    SubTotal = subTotal
                }, trans);

                selectedTicket.MemID = memId;
                selectedTicket.BillNo = billNo;
                selectedTicket.TransDate = transDate;
                selectedTicket.SubTotal = subTotal;
                selectedTicket.GetCodeDate = DateTime.Now;

                trans.Commit();
                return selectedTicket;
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        public async Task<PromotionGiftRecord?> IssuePromotionGiftAsync(
            LotteryProgramInfo program, string memId, string billNo, DateTime transDate, decimal subTotal)
        {
            using var conn = _connectionFactory.CreateConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                // Lấy kho quà chưa phát
                const string sqlAvailable = @"
                    SELECT TOP 50 ID, ProgramID, PromotionCode, Name, ItemCode, ItemType, GiftValue
                    FROM dbo.mssPromotionGift WITH (UPDLOCK, ROWLOCK)
                    WHERE ProgramID = @ProgramID AND MemID IS NULL AND (IsEnable = 1 OR IsEnable IS NULL)";

                var availableGifts = (await conn.QueryAsync<PromotionGiftRecord>(sqlAvailable, new { ProgramID = program.ID }, trans)).ToList();
                if (!availableGifts.Any())
                {
                    trans.Rollback();
                    return null;
                }

                // Bốc thăm ngẫu nhiên
                var rand = new Random();
                var selectedGift = availableGifts[rand.Next(availableGifts.Count)];

                int expireDays = program.TakeExpiredDay ?? 30;
                var expireDate = DateTime.Now.AddDays(expireDays);

                // Gán quà cho hội viên và hóa đơn
                const string sqlAssign = @"
                    UPDATE dbo.mssPromotionGift
                    SET MemID = @MemID,
                        BillNo = @BillNo,
                        TransDate = @TransDate,
                        SubTotal = @SubTotal,
                        GetCodeDate = GETDATE(),
                        ToDate = @ExpireDate
                    WHERE ID = @ID";

                await conn.ExecuteAsync(sqlAssign, new {
                    ID = selectedGift.ID,
                    MemID = memId,
                    BillNo = billNo,
                    TransDate = transDate,
                    SubTotal = subTotal,
                    ExpireDate = expireDate
                }, trans);

                selectedGift.MemID = memId;
                selectedGift.BillNo = billNo;
                selectedGift.TransDate = transDate;
                selectedGift.SubTotal = subTotal;
                selectedGift.GetCodeDate = DateTime.Now;
                selectedGift.ToDate = expireDate;

                trans.Commit();
                return selectedGift;
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        public async Task<NotificationTemplate?> GetNotificationTemplateAsync(string templateNumber)
        {
            using var conn = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT ID, Number, Subject, Content
                FROM dbo.G_NotificationMessage WITH (NOLOCK)
                WHERE Number = @Number";
            return await conn.QueryFirstOrDefaultAsync<NotificationTemplate>(sql, new { Number = templateNumber });
        }

        public async Task<bool> SaveNotificationLogAsync(string memId, string? appId, string title, string eventName, string subject, string body)
        {
            using var conn = _connectionFactory.CreateConnection();
            const string sql = @"
                INSERT INTO dbo.mssNotification (ID, MemID, AppID, Title, EventName, Subject, Body, Status, CreatedOn, CreatedBy)
                VALUES (NEWID(), @MemID, @AppID, @Title, @EventName, @Subject, @Body, 'Sent', GETDATE(), 'BillReviewService')";
            var affected = await conn.ExecuteAsync(sql, new {
                MemID = memId,
                AppID = appId,
                Title = title,
                EventName = eventName,
                Subject = subject,
                Body = body
            });
            return affected > 0;
        }
    }
}
