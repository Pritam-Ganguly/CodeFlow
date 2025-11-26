using CodeFlow.core.Models;

namespace CodeFlow.core.Repositories
{
    public interface IBadgeRepository
    {
        Task<int?> AddBadgeAsync(Badge badge);
        Task<int?> AwardBadgeAsync(int userId, int badgeId);
        Task CheckAndAwardBadge(int userId, BadgeTriggerCondition triggerCondition);
        Task<IEnumerable<Badge>> GetAllBadgesAsync();
        Task<Badge?> GetBadgeByTriggerConditionAsync(BadgeTriggerCondition triggerCondition);
        Task<IEnumerable<Badge>> GetBadgesByUserIdAsync(int userId);
    }
}