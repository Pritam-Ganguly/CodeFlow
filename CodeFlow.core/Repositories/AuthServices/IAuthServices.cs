
namespace CodeFlow.core.Repositories.AuthServices
{
    public interface IAuthServices
    {
        Task<bool?> CanEditAnswerAsync(int answerId, int userId);
        Task<bool?> CanEditQuestionAsync(int questionId, int userId);
    }
}