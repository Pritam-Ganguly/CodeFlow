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
        Task<bool> AddQuestionVoteAsync(Vote vote);
        Task<bool> AddAnswerVoteAsync(Vote vote);
        Task<int> UpdateScoreForQuestionAsync(int questionId);
        Task<int> UpdateScoreForAnswerAsync(int answerId);

    }
}
