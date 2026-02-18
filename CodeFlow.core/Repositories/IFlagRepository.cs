using CodeFlow.core.Models;

namespace CodeFlow.core.Repositories
{
    public interface IFlagRepository
    {
        Task<int?> CreateFlagAsync(Flag flag);
        Task<Flag?> GetByIdAsync(int id);
        Task<IEnumerable<Flag>> GetPendingFlagsAsync(int limit = 20);
        Task<bool> UpdateStatusAsync(int flagId, FlagStatusType status, int resolvedByUserId, string? resolutionNotes);
        Task<bool> GetIfUserHaveAlreadyReported(FlagPostType postType, int postId, int reportingUserId);
        Task<IEnumerable<FlagType>> GetAllFlagTypes();
    }
}