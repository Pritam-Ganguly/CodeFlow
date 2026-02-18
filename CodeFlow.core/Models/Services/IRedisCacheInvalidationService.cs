
namespace CodeFlow.core.Models.Services
{
    public interface IRedisCacheInvalidationService
    {
        Task InvalidateByPatternAsync(string pattern);
        Task InvalidateHomePageQuestionsAsync();
        Task InvalidateHotQuestionAsync();
        Task InvalidateQuestionAsync(int questionId);
        Task InvalidateResetActionAsync();
    }
}