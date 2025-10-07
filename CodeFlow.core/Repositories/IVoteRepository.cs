using CodeFlow.core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeFlow.core.Repositories
{
    public interface IVoteRepository
    {
        Task<bool> AddVoteAsync(Vote vote);
        Task<int> GetScoreForQuestionAsync(int questionId);
        Task<int> GetScoreForAnswerAsync(int answerId);
    }
}
