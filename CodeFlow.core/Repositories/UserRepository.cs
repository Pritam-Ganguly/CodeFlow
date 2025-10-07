using CodeFlow.core.Data;
using CodeFlow.core.Models;
using Dapper;

namespace CodeFlow.core.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public UserRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }
        public async Task<int> CreateAsync(User user)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            var sql = @"
                INSERT INTO Users (DisplayName, Email, PasswordHash, Reputation)
                VALUES (@DispalyName, @Email, @PasswordHash, @Reputation)
                RETURNING Id;";
            var newId = await connection.ExecuteScalarAsync<int>(sql, user);
            return newId;

        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            var sql = "SELECT * FROM Users WHERE Email = @Email";
            using var connection = await _connectionFactory.CreateConnectionAsync();
            return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Email = email });
        }

        public async Task<User> GetByIdAsync(int id)
        {
            var sql = "SELECT * FROM Users WHERE Id = @Id";
            using var connection = await _connectionFactory.CreateConnectionAsync();
            User? user = await connection.QueryFirstOrDefaultAsync<User>(sql, new { Id = id });
            
            if(user == null)
            {
                throw new InvalidOperationException("User doesn't exist.");
            }
            return user;
        }
    }
}