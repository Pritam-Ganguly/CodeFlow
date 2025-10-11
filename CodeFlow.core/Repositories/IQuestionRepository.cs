using CodeFlow.core.Models;

namespace CodeFlow.core.Repositories
{
    public interface IQuestionRepository
    {
        Task<Question?> GetByIdAsync(int id);
        Task<IEnumerable<Question>> GetRecentAsync(int limit = 20);
        Task<int> CreateAsync(Question question);
        Task<Question?> GetByIdWithTagsAsync(int id);
        Task<IEnumerable<Question>> SearchAsync(string searchQuery);
        Task<IEnumerable<Question>> GetRecentWithTagsAsync(int limit = 20);
        Task<int?> CurrentVoteAsync(int userId, int quesitonId);
        Task<int?> CurrentVoteForAnswerItemAsync(int userId, int answerId);
    }
}