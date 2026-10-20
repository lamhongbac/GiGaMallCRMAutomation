using System.Data;
using CRM.Automation.Core.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace CRM.Automation.Infrastructure.Data;

public class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing in configuration.");
    }

    public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
}
