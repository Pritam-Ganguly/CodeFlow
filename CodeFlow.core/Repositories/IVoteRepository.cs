using CodeFlow.core.Models;

namespace CodeFlow.core.Repositories
{
    public interface IVoteRepository
    {
        Task<bool> AddQuestionVoteAsync(Vote vote);
        Task<bool> AddAnswerVoteAsync(Vote vote);
        Task<int> UpdateScoreForQuestionAsync(int questionId);
        Task<int> UpdateScoreForAnswerAsync(int answerId);

    }
}
