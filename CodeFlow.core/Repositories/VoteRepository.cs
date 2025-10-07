using CodeFlow.core.Data;
using CodeFlow.core.Models;
using Dapper;

namespace CodeFlow.core.Repositories
{
    public class VoteRepository : IVoteRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        public VoteRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<bool> AddVoteAsync(Vote vote)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            var sql = @"
                INSERT INTO Votes (VoteType, UserId, QuestionId, AnswerId)
                VALUES (@VoteType, @UserId, @QuestionId, @AnswerId)
                ON CONFLICT ON CONSTRAINT UQ_User_Question DO UPDATE
                SET VoteType = EXCLUDED.VoteType
                WHERE Votes.QuestionId IS NOT NULL;";

            var rowsAffected = await connection.ExecuteAsync(sql, vote);
            return rowsAffected > 0;
        }

        public async Task<int> GetScoreForAnswerAsync(int answerId)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            var sql = "SELECT COALESCE(SUM(VoteType), 0) FROM Votes WHERE AnswerId = @AnswerId";

            return await connection.ExecuteScalarAsync<int>(sql, new { AnswerId = answerId });
        }

        public async Task<int> GetScoreForQuestionAsync(int questionId)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            var sql = "SELECT COALESCE(SUM(VoteType), 0) FROM Votes WHERE QuestionId = @QuestionId";

            return await connection.ExecuteScalarAsync<int>(sql, new { QuestionId = questionId });
        }
    }
}
