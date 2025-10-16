using CodeFlow.core.Models;

namespace CodeFlow.core.Repositories
{
    public interface ICommentRepository
    {
        Task<int?> AddCommentAsync(Comment comment);
        Task<IEnumerable<Comment>> GetAnswerCommentsAsync(int answerId);
        Task<IEnumerable<Comment>> GetQuestionCommentsAsync(int questionId);
    }
}