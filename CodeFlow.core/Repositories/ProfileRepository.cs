using CodeFlow.core.Data;
using CodeFlow.core.Models;
using Dapper;

namespace CodeFlow.core.Repositories
{
    public class ProfileRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public ProfileRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<UserProfile?> GetUserProfileAsync(int userId)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            var sql = "SELECT * FROM UserProfiles WHERE UserId = @UserId";
            return await connection.ExecuteScalarAsync<UserProfile>(sql, new { UserId = userId });
        }

        public async Task<int> CreateUserProfileAsync(UserProfile userProfile)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            var sql = @"
                INSERT INTO UserProfiles (UserId, Bio, ProfilePicture, ProfilePictureMimeType, ProfilePictureFileName)
                VALUES (@UserId, @Bio, @ProfilePicture, @ProfilePictureMimeType, @ProfilePictureFileName)
                RETURNING Id;";
            return await connection.ExecuteScalarAsync<int>(sql);
        }

    }
}
