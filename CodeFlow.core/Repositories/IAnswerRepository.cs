using CodeFlow.core.Models;

namespace CodeFlow.core.Repositories
{
    public interface IAnswerRepository
    {
        Task<Answer?> GetByIdAsync(int id);
        Task<IEnumerable<Answer>> GetByQuestionIdAsync(int questionId);
        Task<int> CreateAsync(Answer answer);
        Task<int> AcceptAnswer(int answerId);
        Task<Question?> GetQuestionByAnswerIdAsync(int answerId);
        Task<int?> EditAnswerAsync(int answerId, string body);
        Task<bool> DeleteAnswerAsync(int answerId);
        Task<int> GetAcceptedAnswerCountByUserIdAsync(int userId);
    }
}