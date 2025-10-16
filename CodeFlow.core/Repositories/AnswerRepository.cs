using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeFlow.core.Data;
using CodeFlow.core.Models;
using Dapper;

namespace CodeFlow.core.Repositories
{
    public class AnswerRepository : IAnswerRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public AnswerRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<int> CreateAsync(Answer answer)
        {
            var sql = @"
            INSERT INTO Answers (Body, QuestionId, UserId)
            VALUES (@Body, @QuestionId, @UserId)
            RETURNING Id;";

            using var connection = await _connectionFactory.CreateConnectionAsync();
            var newId = await connection.ExecuteScalarAsync<int>(sql, answer);
            return newId;
        }

        public async Task<Answer?> GetByIdAsync(int id)
        {
            var sql = "SELECT * FROM Answers WHERE Id = @Id";
            using var connection = await _connectionFactory.CreateConnectionAsync();
            return await connection.QueryFirstOrDefaultAsync<Answer>(sql, param: new { Id = id });
        }

        public async Task<IEnumerable<Answer>> GetByQuestionIdAsync(int questionId)
        {
            var sql = @"
            SELECT a.*, u.*
            FROM Answers a
            INNER JOIN Users u ON a.UserId = u.Id
            WHERE a.QuestionId = @QuestionId
            ORDER BY a.Score DESC, a.CreatedAt ASC";

            using var connection = await _connectionFactory.CreateConnectionAsync();
            return await connection.QueryAsync<Answer, User, Answer>(sql, map: (answer, user) =>
            {
                answer.User = user;
                return answer;
            },
            param: new { QuestionId = questionId },
            splitOn: "Id");

        }

        public async Task<Question?> GetQuestionByAnserIdAync(int answerId)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            var sql = "SELECT q.* FROM Questions q INNER JOIN Answers a ON q.Id = a.QuestionId WHERE a.Id = @Id";
            return await connection.QueryFirstOrDefaultAsync<Question?>(sql, new {Id = answerId});
        } 

        public async Task<int> AcceptAnswer(int answerId)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            var sql = @"UPDATE Answers SET IsAccepted = TRUE WHERE Id = @Id";
            return await connection.ExecuteAsync(sql, new { Id = answerId });
        }

        public async Task<int?> EditAnswerAsync(int answerId, string body)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            var sql = @"UPDATE Answers SET Body = @Body WHERE Id = @Id";
            return await connection.ExecuteAsync(sql, new { Body = body, Id = answerId });
        }
    }
}