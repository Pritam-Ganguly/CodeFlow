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

        public async Task<bool> AddQuestionVoteAsync(Vote vote)
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

        public async Task<bool> AddAnswerVoteAsync(Vote vote)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            var sql = @"
                INSERT INTO Votes (VoteType, UserId, QuestionId, AnswerId)
                VALUES (@VoteType, @UserId, @QuestionId, @AnswerId)
                ON CONFLICT ON CONSTRAINT UQ_User_Answer DO UPDATE
                SET VoteType = EXCLUDED.VoteType
                WHERE Votes.AnswerId IS NOT NULL;";
            var rowsAffected = await connection.ExecuteAsync(sql, vote);
            return rowsAffected > 0;
        }

        public async Task<int> UpdateScoreForAnswerAsync(int answerId)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            var sql = @"UPDATE Answers SET Score = COALESCE((SELECT SUM(VoteType) FROM Votes WHERE AnswerId = @AnswerId), 0) 
                        WHERE Id = @AnswerId; 
                        SELECT Score FROM Answers WHERE Id = @AnswerId;";
            return await connection.ExecuteScalarAsync<int>(sql, new { AnswerId = answerId });
        }

        public async Task<int> UpdateScoreForQuestionAsync(int questionId)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            var sql = @"UPDATE Questions SET Score = COALESCE((SELECT SUM(VoteType) FROM Votes WHERE QuestionId = @QuestionId), 0) 
                        WHERE Id = @QuestionId; 
                        SELECT Score FROM Questions WHERE Id = @QuestionId;";
            return await connection.ExecuteScalarAsync<int>(sql, new { QuestionId = questionId });
        }
    }
}
