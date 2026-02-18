using CodeFlow.core.Models;

namespace CodeFlow.core.Repositories
{
    public interface IQuestionRepository
    {
        Task<Question?> GetByIdAsync(int id);
        Task<int> CreateAsync(Question question);
        Task<Question?> GetByIdWithTagsAsync(int id);
        Task<int> GetAllResult(string searchQuery);
        Task<int> GetAllQuestions();
        Task<IEnumerable<Question>> SearchAsync(string searchQuery, int pageNumber = 1, int pageSize = 10, QuestionSortType sortType = QuestionSortType.Newest);
        Task<IEnumerable<Question>> GetRecentWithTagsAsync(int pageNumber = 1, int pageSize = 10, QuestionSortType sortType = QuestionSortType.Newest);
        Task<IEnumerable<Question>> GetAllQuestionsByUserId(int userId, int pageNumber = 1, int pageSize = 10);
        Task<int?> CurrentVoteAsync(int userId, int quesitonId);
        Task<int?> CurrentVoteForAnswerItemAsync(int userId, int answerId);
        Task<int> UpdateQuestionAsync(int questionId, string newTitle, string newBody);
        Task<bool> DeleteQuestionAsync(int questionId, int userId);
        Task<bool> FirstPost(int userId);
    }
}