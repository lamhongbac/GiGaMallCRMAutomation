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

            const string alterSql = @"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.mssOCRTrialTrans') AND name = 'OCRStatus')
                BEGIN
                    ALTER TABLE dbo.mssOCRTrialTrans ADD [OCRStatus] [bit] NULL;
                END";
            await conn.ExecuteAsync(alterSql);

            const string alterCrmProcessSql = @"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.mssOCRTrialTrans') AND name = 'CRMProcess')
                BEGIN
                    ALTER TABLE dbo.mssOCRTrialTrans ADD [CRMProcess] [bit] NULL DEFAULT (0);
                END";
            await conn.ExecuteAsync(alterCrmProcessSql);

        }

        public async Task<IEnumerable<OcrTrialTrans>> GetPendingBillsAsync(DateTime? fromDate = null, DateTime? toDate = null, int limit = 100)
        {
            await EnsureTableSchemaAsync();
            using var conn = _connectionFactory.CreateConnection();
            var sql = @"
                SELECT TOP (@Limit) *
                FROM dbo.mssOCRTrialTrans WITH (NOLOCK)
                WHERE (OCRStatus = 0 OR OCRStatus IS NULL)
                  AND (Approved = 0 OR Approved IS NULL)
                  AND (@FromDate IS NULL OR UploadImageDateTime >= @FromDate)
                  AND (@ToDate IS NULL OR UploadImageDateTime <= @ToDate)
                ORDER BY UploadImageDateTime ASC";

            return await conn.QueryAsync<OcrTrialTrans>(sql, new { FromDate = fromDate, ToDate = toDate, Limit = limit });
        }

        public async Task<IEnumerable<OcrTrialTrans>> GetBillsForReviewAsync(DateTime? fromDate = null, DateTime? toDate = null, int limit = 100)
        {
            await EnsureTableSchemaAsync();
            using var conn = _connectionFactory.CreateConnection();
            var sql = @"
                SELECT TOP (@Limit) *
                FROM dbo.mssOCRTrialTrans WITH (NOLOCK)
                WHERE OCRStatus = 1
                  AND (Approved = 0 OR Approved IS NULL)
                  AND (@FromDate IS NULL OR UploadImageDateTime >= @FromDate)
                  AND (@ToDate IS NULL OR UploadImageDateTime <= @ToDate)
                ORDER BY UploadImageDateTime ASC";

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
                    Approved, Reviewed, DataStatus, OCRStatus, OutletName, BrandName
                ) VALUES (
                    @ID, @MemID, @UploadImageDateTime, @ImagePath, @ImageFileName,
                    @OutletNumber, @BrandNumber, @OutletAddress, @BillNumber, @BillDateTime,
                    @BillDateTimeFormat, @BillTotal, @CashierName, @ScanDate,
                    @Approved, @Reviewed, @DataStatus, @OCRStatus, @OutletName, @BrandName
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

            public async Task<(IEnumerable<OcrTrialTrans> Items, int TotalCount)> GetPendingUploadBillsPagedAsync(DateTime? fromDate = null, DateTime? toDate = null, int pageIndex = 1, int pageSize = 25)
        {
            await EnsureTableSchemaAsync();
            using var conn = _connectionFactory.CreateConnection();

            int offset = (pageIndex - 1) * pageSize;
            if (offset < 0) offset = 0;

            // Nếu toDate chỉ có phần ngày (00:00:00), mở rộng đến cuối ngày 23:59:59.997
            DateTime? adjustedToDate = toDate.HasValue && toDate.Value.TimeOfDay == TimeSpan.Zero
                ? toDate.Value.Date.AddDays(1).AddTicks(-1)
                : toDate;

            const string sql = @"
                SELECT COUNT(1)
                FROM dbo.mssOCRTrialTrans WITH (NOLOCK)
                WHERE (OCRStatus = 0 OR OCRStatus IS NULL)
                  AND (Approved = 0 OR Approved IS NULL)
                  AND (@FromDate IS NULL OR UploadImageDateTime >= @FromDate)
                  AND (@ToDate IS NULL OR UploadImageDateTime <= @ToDate);

                SELECT *
                FROM dbo.mssOCRTrialTrans WITH (NOLOCK)
                WHERE (OCRStatus = 0 OR OCRStatus IS NULL)
                  AND (Approved = 0 OR Approved IS NULL)
                  AND (@FromDate IS NULL OR UploadImageDateTime >= @FromDate)
                  AND (@ToDate IS NULL OR UploadImageDateTime <= @ToDate)
                ORDER BY UploadImageDateTime ASC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            using var multi = await conn.QueryMultipleAsync(sql, new
            {
                FromDate = fromDate,
                ToDate = adjustedToDate,
                Offset = offset,
                PageSize = pageSize
            });

            int totalCount = await multi.ReadSingleAsync<int>();
            var items = await multi.ReadAsync<OcrTrialTrans>();

            return (items, totalCount);
        }

        public async Task<IEnumerable<OcrTrialTrans>> GetPendingUploadBillsAsync(DateTime? fromDate = null, DateTime? toDate = null, int limit = 50)
        {
            await EnsureTableSchemaAsync();
            using var conn = _connectionFactory.CreateConnection();

            DateTime? adjustedToDate = toDate.HasValue && toDate.Value.TimeOfDay == TimeSpan.Zero
                ? toDate.Value.Date.AddDays(1).AddTicks(-1)
                : toDate;

            const string sql = @"
                SELECT TOP (@Limit) *
                FROM dbo.mssOCRTrialTrans WITH (NOLOCK)
                WHERE (OCRStatus = 0 OR OCRStatus IS NULL)
                  AND (Approved = 0 OR Approved IS NULL)
                  AND (@FromDate IS NULL OR UploadImageDateTime >= @FromDate)
                  AND (@ToDate IS NULL OR UploadImageDateTime <= @ToDate)
                ORDER BY UploadImageDateTime ASC";

            return await conn.QueryAsync<OcrTrialTrans>(sql, new { FromDate = fromDate, ToDate = adjustedToDate, Limit = limit });
        }

        public async Task<bool> UpdateOcrScanResultAsync(
            Guid billId, 
            string? outletNumber, 
            string? brandNumber, 
            string? outletName, 
            string? brandName, 
            string? billNumber, 
            string? billDateTime, 
            string? billTotal, 
            string? cashierName,
            string? rawOcrResponse = null)
        {
            await EnsureTableSchemaAsync();
            using var conn = _connectionFactory.CreateConnection();
            const string sql = @"
                UPDATE dbo.mssOCRTrialTrans
                SET OutletNumber = COALESCE(@OutletNumber, OutletNumber),
                    BrandNumber = COALESCE(@BrandNumber, BrandNumber),
                    OutletName = COALESCE(@OutletName, OutletName),
                    BrandName = COALESCE(@BrandName, BrandName),
                    BillNumber = COALESCE(@BillNumber, BillNumber),
                    BillDateTime = COALESCE(@BillDateTime, BillDateTime),
                    BillTotal = COALESCE(@BillTotal, BillTotal),
                    CashierName = COALESCE(@CashierName, CashierName),
                    ScanDate = GETDATE(),
                    OCRStatus = 1,
                    DataStatus = 'OCR'
                WHERE ID = @BillId";

            var affected = await conn.ExecuteAsync(sql, new {
                BillId = billId,
                OutletNumber = outletNumber,
                BrandNumber = brandNumber,
                OutletName = outletName,
                BrandName = brandName,
                BillNumber = billNumber,
                BillDateTime = billDateTime,
                BillTotal = billTotal,
                CashierName = cashierName
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
                WHERE COALESCE(OutletNumberA, OutletNumber) = @OutletNumber 
                  AND COALESCE(BillNumberA, BillNumber) = @BillNumber 
                  AND Approved = 1 
                  AND ID <> @ExcludeBillId";

            var count = await conn.ExecuteScalarAsync<int>(sql, new { 
                OutletNumber = outletNumber, 
                BillNumber = billNumber, 
                ExcludeBillId = excludeBillId 
            });
            return count > 0;
        }

        public async Task<(IEnumerable<OcrTrialTrans> Items, int TotalCount)> GetBillsPagedAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? brandNumber = null,
            string? outletNumber = null,
            string? keyword = null,
            bool? ocrStatus = null,
            bool? reviewed = null,
            bool? approved = null,
            int pageIndex = 1,
            int pageSize = 25)
        {
            await EnsureTableSchemaAsync();
            using var conn = _connectionFactory.CreateConnection();

            int offset = (pageIndex - 1) * pageSize;
            if (offset < 0) offset = 0;

            DateTime? adjustedToDate = toDate.HasValue && toDate.Value.TimeOfDay == TimeSpan.Zero
                ? toDate.Value.Date.AddDays(1).AddTicks(-1)
                : toDate;

            string keywordLike = string.IsNullOrWhiteSpace(keyword) ? null : $"%{keyword.Trim()}%";

            const string countSql = @"
                SELECT COUNT(1)
                FROM dbo.mssOCRTrialTrans WITH (NOLOCK)
                WHERE 1=1
                  AND (@FromDate IS NULL OR UploadImageDateTime >= @FromDate)
                  AND (@ToDate IS NULL OR UploadImageDateTime <= @ToDate)
                  AND (@BrandNumber IS NULL OR BrandNumber = @BrandNumber OR BrandNumberA = @BrandNumber)
                  AND (@OutletNumber IS NULL OR OutletNumber = @OutletNumber OR OutletNumberA = @OutletNumber)
                  AND (@OcrStatus IS NULL OR (@OcrStatus = 1 AND OCRStatus = 1) OR (@OcrStatus = 0 AND (OCRStatus = 0 OR OCRStatus IS NULL)))
                  AND (@Reviewed IS NULL OR (@Reviewed = 1 AND Reviewed = 1) OR (@Reviewed = 0 AND (Reviewed = 0 OR Reviewed IS NULL)))
                  AND (@Approved IS NULL OR Approved = @Approved)
                  AND (@KeywordLike IS NULL OR MemID LIKE @KeywordLike OR OutletName LIKE @KeywordLike OR BillNumber LIKE @KeywordLike OR BillNumberA LIKE @KeywordLike);";

            const string selectSql = @"
                SELECT *
                FROM dbo.mssOCRTrialTrans WITH (NOLOCK)
                WHERE 1=1
                  AND (@FromDate IS NULL OR UploadImageDateTime >= @FromDate)
                  AND (@ToDate IS NULL OR UploadImageDateTime <= @ToDate)
                  AND (@BrandNumber IS NULL OR BrandNumber = @BrandNumber OR BrandNumberA = @BrandNumber)
                  AND (@OutletNumber IS NULL OR OutletNumber = @OutletNumber OR OutletNumberA = @OutletNumber)
                  AND (@OcrStatus IS NULL OR (@OcrStatus = 1 AND OCRStatus = 1) OR (@OcrStatus = 0 AND (OCRStatus = 0 OR OCRStatus IS NULL)))
                  AND (@Reviewed IS NULL OR (@Reviewed = 1 AND Reviewed = 1) OR (@Reviewed = 0 AND (Reviewed = 0 OR Reviewed IS NULL)))
                  AND (@Approved IS NULL OR Approved = @Approved)
                  AND (@KeywordLike IS NULL OR MemID LIKE @KeywordLike OR OutletName LIKE @KeywordLike OR BillNumber LIKE @KeywordLike OR BillNumberA LIKE @KeywordLike)
                ORDER BY UploadImageDateTime DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            var parameters = new
            {
                FromDate = fromDate,
                ToDate = adjustedToDate,
                BrandNumber = string.IsNullOrWhiteSpace(brandNumber) ? null : brandNumber,
                OutletNumber = string.IsNullOrWhiteSpace(outletNumber) ? null : outletNumber,
                OcrStatus = ocrStatus,
                Reviewed = reviewed,
                Approved = approved,
                KeywordLike = keywordLike,
                Offset = offset,
                PageSize = pageSize
            };

            int totalCount = await conn.ExecuteScalarAsync<int>(countSql, parameters);
            var items = await conn.QueryAsync<OcrTrialTrans>(selectSql, parameters);

            return (items, totalCount);
        }

        public async Task<IEnumerable<string>> GetDistinctBrandsAsync()
        {
            await EnsureTableSchemaAsync();
            using var conn = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT DISTINCT COALESCE(BrandName, BrandNumber) AS Brand
                FROM dbo.mssOCRTrialTrans WITH (NOLOCK)
                WHERE BrandName IS NOT NULL OR BrandNumber IS NOT NULL
                ORDER BY Brand;";
            return await conn.QueryAsync<string>(sql);
        }

        public async Task<IEnumerable<(string OutletNumber, string OutletName, string BrandNumber)>> GetDistinctOutletsAsync()
        {
            await EnsureTableSchemaAsync();
            using var conn = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT DISTINCT 
                    COALESCE(OutletNumber, OutletNumberA) AS OutletNumber,
                    COALESCE(OutletName, OutletNumber) AS OutletName,
                    COALESCE(BrandNumber, BrandNumberA) AS BrandNumber
                FROM dbo.mssOCRTrialTrans WITH (NOLOCK)
                WHERE OutletNumber IS NOT NULL OR OutletNumberA IS NOT NULL
                ORDER BY OutletName;";

            var rows = await conn.QueryAsync(sql);
            var result = new List<(string, string, string)>();
            foreach (var r in rows)
            {
                result.Add(((string)r.OutletNumber, (string)r.OutletName, (string)r.BrandNumber));
            }
            return result;
        }
    }
}
