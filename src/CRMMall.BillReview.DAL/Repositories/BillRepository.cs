using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using CRMMall.BillReview.DAL.Data;
using CRMMall.BillReview.DAL.Entities;

namespace CRMMall.BillReview.DAL.Repositories
{
    public class BillRepository : IBillRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public BillRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task EnsureTableSchemaAsync()
        {
            const string sql = @"
            IF OBJECT_ID('dbo.mssOCRTrialTrans', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.mssOCRTrialTrans (
                    [ID] [uniqueidentifier] NOT NULL PRIMARY KEY DEFAULT (NEWID()),
                    [MemID] [nvarchar](50) NOT NULL,
                    [UploadImageDateTime] [datetime] NOT NULL DEFAULT (GETDATE()),
                    [ImagePath] [nvarchar](250) NULL,
                    [ImageFileName] [nvarchar](250) NULL,
                    [OutletNumber] [nvarchar](50) NULL,
                    [BrandNumber] [nvarchar](50) NULL,
                    [OutletAddress] [nvarchar](500) NULL,
                    [BillNumber] [nvarchar](50) NULL,
                    [BillDateTime] [nvarchar](50) NULL,
                    [BillDateTimeFormat] [nvarchar](50) NULL,
                    [BillTotal] [nvarchar](50) NULL,
                    [CashierName] [nvarchar](100) NULL,
                    [ScanDate] [datetime] NULL,
                    [ReviewDate] [datetime] NULL,
                    [ReviewBy] [nvarchar](100) NULL,
                    [TransDocID] [uniqueidentifier] NULL,
                    [Approved] [bit] NULL,
                    [PointGain] [decimal](18, 2) NULL DEFAULT (0),
                    [Reviewed] [bit] NULL DEFAULT (0),
                    [ReviewNotes] [nvarchar](500) NULL,
                    [DataStatus] [nvarchar](50) NULL DEFAULT ('Pending'),
                    [BillNumberA] [nvarchar](50) NULL,
                    [BillDateTimeA] [datetime] NULL,
                    [BillTotalA] [decimal](18, 2) NULL,
                    [BrandNumberA] [nvarchar](50) NULL,
                    [OutletNumberA] [nvarchar](50) NULL,
                    [OutletName] [nvarchar](250) NULL,
                    [BrandName] [nvarchar](250) NULL,
                    [FloorName] [nvarchar](50) NULL,
                    [MallInfo] [nvarchar](100) NULL
                );
                CREATE NONCLUSTERED INDEX IX_mssOCRTrialTrans_Status ON dbo.mssOCRTrialTrans(Approved, UploadImageDateTime);
            END";
            using var conn = _connectionFactory.CreateConnection();
            await conn.ExecuteAsync(sql);
        }

        public async Task<IEnumerable<OcrTrialTrans>> GetPendingBillsAsync(DateTime? fromDate = null, DateTime? toDate = null, int limit = 100)
        {
            await EnsureTableSchemaAsync();
            using var conn = _connectionFactory.CreateConnection();
            var sql = @"
                SELECT TOP (@Limit) *
                FROM dbo.mssOCRTrialTrans WITH (NOLOCK)
                WHERE Approved IS NULL OR Reviewed = 0
                  AND (@FromDate IS NULL OR UploadImageDateTime >= @FromDate)
                  AND (@ToDate IS NULL OR UploadImageDateTime <= @ToDate)
                ORDER BY UploadImageDateTime DESC";

            return await conn.QueryAsync<OcrTrialTrans>(sql, new { FromDate = fromDate, ToDate = toDate, Limit = limit });
        }

        public async Task<OcrTrialTrans?> GetByIdAsync(Guid id)
        {
            await EnsureTableSchemaAsync();
            using var conn = _connectionFactory.CreateConnection();
            const string sql = "SELECT * FROM dbo.mssOCRTrialTrans WITH (NOLOCK) WHERE ID = @ID";
            return await conn.QueryFirstOrDefaultAsync<OcrTrialTrans>(sql, new { ID = id });
        }

        public async Task<bool> InsertAsync(OcrTrialTrans bill)
        {
            await EnsureTableSchemaAsync();
            using var conn = _connectionFactory.CreateConnection();
            const string sql = @"
                INSERT INTO dbo.mssOCRTrialTrans (
                    ID, MemID, UploadImageDateTime, ImagePath, ImageFileName,
                    OutletNumber, BrandNumber, OutletAddress, BillNumber, BillDateTime,
                    BillDateTimeFormat, BillTotal, CashierName, ScanDate,
                    Approved, Reviewed, DataStatus, OutletName, BrandName
                ) VALUES (
                    @ID, @MemID, @UploadImageDateTime, @ImagePath, @ImageFileName,
                    @OutletNumber, @BrandNumber, @OutletAddress, @BillNumber, @BillDateTime,
                    @BillDateTimeFormat, @BillTotal, @CashierName, @ScanDate,
                    @Approved, @Reviewed, @DataStatus, @OutletName, @BrandName
                )";
            var affected = await conn.ExecuteAsync(sql, bill);
            return affected > 0;
        }

        public async Task<bool> UpdateApprovalStatusAsync(
            Guid id, 
            bool approved, 
            string reviewBy, 
            string? reviewNotes, 
            decimal? approvedAmount, 
            string? approvedBillNumber, 
            DateTime? approvedBillDateTime, 
            string? approvedOutletNumber, 
            string? approvedBrandNumber, 
            decimal? pointGain)
        {
            await EnsureTableSchemaAsync();
            using var conn = _connectionFactory.CreateConnection();
            const string sql = @"
                UPDATE dbo.mssOCRTrialTrans
                SET Approved = @Approved,
                    Reviewed = 1,
                    ReviewDate = GETDATE(),
                    ReviewBy = @ReviewBy,
                    ReviewNotes = @ReviewNotes,
                    BillTotalA = @ApprovedAmount,
                    BillNumberA = @ApprovedBillNumber,
                    BillDateTimeA = @ApprovedBillDateTime,
                    OutletNumberA = @ApprovedOutletNumber,
                    BrandNumberA = @ApprovedBrandNumber,
                    PointGain = @PointGain,
                    DataStatus = CASE WHEN @Approved = 1 THEN 'Approved' ELSE 'Rejected' END
                WHERE ID = @ID";

            var affected = await conn.ExecuteAsync(sql, new {
                ID = id,
                Approved = approved,
                ReviewBy = reviewBy,
                ReviewNotes = reviewNotes,
                ApprovedAmount = approvedAmount,
                ApprovedBillNumber = approvedBillNumber,
                ApprovedBillDateTime = approvedBillDateTime,
                ApprovedOutletNumber = approvedOutletNumber,
                ApprovedBrandNumber = approvedBrandNumber,
                PointGain = pointGain
            });
            return affected > 0;
        }

        public async Task<bool> IsBillNumberExistsAsync(string outletNumber, string billNumber, Guid excludeBillId)
        {
            await EnsureTableSchemaAsync();
            using var conn = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT COUNT(1) 
                FROM dbo.mssOCRTrialTrans WITH (NOLOCK)
                WHERE OutletNumberA = @OutletNumber 
                  AND BillNumberA = @BillNumber 
                  AND Approved = 1 
                  AND ID <> @ExcludeBillId";

            var count = await conn.ExecuteScalarAsync<int>(sql, new { 
                OutletNumber = outletNumber, 
                BillNumber = billNumber, 
                ExcludeBillId = excludeBillId 
            });
            return count > 0;
        }
    }
}
