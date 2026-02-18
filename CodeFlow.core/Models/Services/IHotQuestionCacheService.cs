
namespace CodeFlow.core.Models.Services
{
    public interface IHotQuestionCacheService
    {
        Task<IEnumerable<Question>> GetHotQuestionsAsync(int count = 10);
        Task WarmTask();
    }
}