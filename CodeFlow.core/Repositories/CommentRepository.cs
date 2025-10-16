using CodeFlow.core.Data;
using CodeFlow.core.Models;
using Dapper;

namespace CodeFlow.core.Repositories
{
    public class CommentRepository : ICommentRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public CommentRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<int?> AddCommentAsync(Comment comment)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            var sql = @"INSERT INTO Comments (Body, UserId, QuestionId, AnswerId) 
                        VALUES (@Body, @UserId, @QuestionId, @AnswerId) 
                        RETURNING Id;";

            return await connection.ExecuteScalarAsync<int>(sql, new
            {
                comment.Body,
                comment.UserId,
                comment.QuestionId,
                comment.AnswerId
            });
        }

        public async Task<IEnumerable<Comment>> GetQuestionCommentsAsync(int questionId)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            var sql = @"SELECT c.*, u.* FROM Comments c INNER JOIN 
                        Users u ON c.UserId = u.Id
                        WHERE c.QuestionId = @QuestionId
                        ORDER BY c.CreatedAt ASC";
            return await connection.QueryAsync<Comment, User, Comment>(sql,
                map: (comment, user) =>
                {
                    comment.User = user;
                    return comment;
                },
                param: new { QuestionId = questionId },
                splitOn: "Id");
        }

        public async Task<IEnumerable<Comment>> GetAnswerCommentsAsync(int answerId)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            var sql = @"SELECT c.*, u.* FROM Comments c INNER JOIN 
                        Users u ON c.UserId = u.Id
                        WHERE AnswerId = @AnswerId
                        ORDER BY c.CreatedAt ASC";
            return await connection.QueryAsync<Comment, User, Comment>(sql,
                map: (comment, user) =>
                {
                    comment.User = user;
                    return comment;
                },
                param: new { AnswerId = answerId },
                splitOn: "Id");
        }
    }
}
