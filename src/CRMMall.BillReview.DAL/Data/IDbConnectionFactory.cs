using System.Data;

namespace CRMMall.BillReview.DAL.Data
{
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}
