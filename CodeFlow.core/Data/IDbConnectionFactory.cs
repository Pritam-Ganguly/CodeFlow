using System.Data;
using Npgsql;

namespace CodeFlow.core.Data
{
    public interface IDbConnectionFactory
    {
        Task<IDbConnection> CreateConnectionAsync();
    }
}