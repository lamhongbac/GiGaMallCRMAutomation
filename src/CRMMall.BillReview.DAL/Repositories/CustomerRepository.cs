using System.Threading.Tasks;
using Dapper;
using CRMMall.BillReview.DAL.Data;
using CRMMall.BillReview.DAL.Entities;

namespace CRMMall.BillReview.DAL.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public CustomerRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<CustomerInfo?> GetCustomerByIdAsync(string memId)
        {
            using var conn = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT MemID, CardNo, FullName, Mobile, Email, Status, CreatedOn AS DateJoined
                FROM dbo.mssCustomer WITH (NOLOCK)
                WHERE MemID = @MemID";
            return await conn.QueryFirstOrDefaultAsync<CustomerInfo>(sql, new { MemID = memId });
        }

        public async Task<string?> GetDeviceTokenAsync(string memId)
        {
            try
            {
                using var conn = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT TOP 1 AppID
                    FROM dbo.mssMobileTracking WITH (NOLOCK)
                    WHERE MemID = @MemID
                    ORDER BY ID DESC";
                return await conn.ExecuteScalarAsync<string?>(sql, new { MemID = memId });
            }
            catch
            {
                return null;
            }
        }
    }
}
