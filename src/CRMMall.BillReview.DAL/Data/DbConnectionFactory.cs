using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace CRMMall.BillReview.DAL.Data
{
    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public DbConnectionFactory(IConfiguration configuration)
        {
            string env = configuration["Env"] ?? "DEV";
            bool isProd = string.Equals(env, "PROD", StringComparison.OrdinalIgnoreCase);

            string? connStr = isProd 
                ? configuration.GetConnectionString("PRODConnection") 
                : configuration.GetConnectionString("DEVConnection");

            _connectionString = connStr 
                ?? configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException($"Connection string for Env '{env}' not found in configuration.");
        }

        public DbConnectionFactory(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public IDbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}
