using CodeFlow.core.Models;

namespace CodeFlow.core.Repositories
{
    public interface ITagRepository
    {
        Task<Tag?> GetByNameAsync(string name);
        Task<Tag> CreateAsync(Tag tag);
        Task<IEnumerable<Tag>> GetTagsForQuestionAsync(int questionId);
        Task AddTagsToQuestionAsync(int quesitonId, IEnumerable<int> tagIds);
    }
}
