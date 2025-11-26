using CodeFlow.core.Data;
using CodeFlow.core.Models;
using Dapper;

namespace CodeFlow.core.Repositories
{
    public class UserActivityRepository : IUserActivityRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        public UserActivityRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<int?> AddUserActivityAsync(int userId, ActivityType activityType, TargetEntityType targetEntityType, int? targetEntityId = null)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            var sql = @"INSERT INTO UserActivities (UserId, ActivityType, TargetEntityType, TargetEntityId)
                        VALUES (@UserId, @ActivityType, @TargetEntityType, @TagetEntityId) RETURNING Id;";
            return await connection.ExecuteScalarAsync<int>(sql, new
            {
                UserId = userId,
                ActivityType = activityType.ToString(),
                TargetEntityType = targetEntityType.ToString(),
                TagetEntityId = targetEntityId
            });
        }

        public async Task<IEnumerable<UserActivity>> GetUserActivitiesAsync(int userId)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            var sql = @"SELECT * FROM UserActivities WHERE UserId = @UserId ORDER BY CreatedAt DESC;";
            return await connection.QueryAsync<UserActivity>(sql, new { UserId = userId });
        }
    }
}
