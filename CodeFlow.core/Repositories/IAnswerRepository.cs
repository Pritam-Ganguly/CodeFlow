using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeFlow.core.Models;

namespace CodeFlow.core.Repositories
{
    public interface IAnswerRepository
    {
        Task<Answer?> GetByIdAsync(int id);
        Task<IEnumerable<Answer>> GetByQuestionIdAsync(int questionId);
        Task<int> CreateAsync(Answer answer);
        Task<int> AcceptAnswer(int answerId);
        Task<Question?> GetQuestionByAnserIdAync(int answerId);
        Task<int?> EditAnswerAsync(int answerId, string body);
    }
}