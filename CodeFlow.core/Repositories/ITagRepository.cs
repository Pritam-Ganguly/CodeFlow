using CodeFlow.core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
