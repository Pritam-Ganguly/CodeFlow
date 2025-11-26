using CodeFlow.core.Models;
using CodeFlow.core.Models.Mapping;

namespace CodeFlow.core.Repositories
{
    public interface IReputationRepository
    {
        Task<int?> AddReputationTransactionAsync(int userId, ReputationTransactionTypes transaction, int relatedPostId, RelatedPostType relatedPostType, int? actingUserId = null);
        Task<int> CalculateReputationAsync(int userId);
        Task<IEnumerable<ReputationHistory>> GetReputationHistoryAsync(int userId);
    }
}