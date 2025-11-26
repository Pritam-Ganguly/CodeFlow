using CodeFlow.core.Models;

namespace CodeFlow.core.Repositories
{
    public interface IUserActivityRepository
    {
        Task<int?> AddUserActivityAsync(int userId, ActivityType activityType, TargetEntityType targetEntityType, int? targetEntityId = 0);
        Task<IEnumerable<UserActivity>> GetUserActivitiesAsync(int userId);
    }
}