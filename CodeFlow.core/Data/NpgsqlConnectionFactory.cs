using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;

namespace CodeFlow.core.Data
{
    public class NpgsqlConnectionFactory : IDbConnectionFactory
    {
        private readonly IConfiguration _configuration;

        public NpgsqlConnectionFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IDbConnection> CreateConnectionAsync()
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("DefaultConnection string is not configured.");
            }

            var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            return connection;
        }
    }
}