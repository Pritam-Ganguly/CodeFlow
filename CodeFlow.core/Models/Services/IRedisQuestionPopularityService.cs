
namespace CodeFlow.core.Models.Services
{
    public interface IRedisQuestionPopularityService
    {
        Task CleanUpOldScoreAsync(int keepTopN = 10);
        Task<long> GetHotnessScoreAsync(int questionId);
        Task<IEnumerable<int>> GetHotQuestionIdsAsync(int count = 20);
        Task RecordViewAsync(int questionId);
    }
}