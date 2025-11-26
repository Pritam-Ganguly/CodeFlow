using CodeFlow.core.Data;
using CodeFlow.core.Models;
using Dapper;
using Microsoft.Extensions.Logging;

namespace CodeFlow.core.Repositories
{
    /// <summary>
    /// Repository for CRUD operations related to comments.
    /// Provides methods to add comments and to fetch comments for questions and answers.
    /// Methods include structured logging for diagnostics and error reporting.
    /// </summary>
    public class CommentRepository : ICommentRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ILogger<CommentRepository> _logger;

        /// <summary>
        /// Creates a new instance of <see cref="CommentRepository"/>.
        /// </summary>
        public CommentRepository(IDbConnectionFactory dbConnectionFactory, ILogger<CommentRepository> logger)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _logger = logger;
        }

        /// <summary>
        /// Adds a comment to a question or answer. Returns the new comment Id.
        /// </summary>
        public async Task<int?> AddCommentAsync(Comment comment)
        {
            _logger.LogDebug("AddCommentAsync called: UserId={UserId}, QuestionId={QuestionId}, AnswerId={AnswerId}", comment.UserId, comment.QuestionId, comment.AnswerId);
            try
            {
                using var connection = await _dbConnectionFactory.CreateConnectionAsync();
                var sql = @"INSERT INTO Comments (Body, UserId, QuestionId, AnswerId) 
                            VALUES (@Body, @UserId, @QuestionId, @AnswerId) 
                            RETURNING Id;";

                var newId = await connection.ExecuteScalarAsync<int>(sql, new
                {
                    comment.Body,
                    comment.UserId,
                    comment.QuestionId,
                    comment.AnswerId
                });

                _logger.LogInformation("Comment created with Id={CommentId} by UserId={UserId}", newId, comment.UserId);
                return newId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add comment for QuestionId={QuestionId}, AnswerId={AnswerId}, UserId={UserId}", comment.QuestionId, comment.AnswerId, comment.UserId);
                throw;
            }
        }

        /// <summary>
        /// Retrieves comments for a given question ordered by creation date (ascending).
        /// </summary>
        public async Task<IEnumerable<Comment>> GetQuestionCommentsAsync(int questionId)
        {
            _logger.LogDebug("GetQuestionCommentsAsync called for QuestionId={QuestionId}", questionId);
            try
            {
                using var connection = await _dbConnectionFactory.CreateConnectionAsync();
                var sql = @"SELECT c.*, u.* FROM Comments c INNER JOIN 
                            Users u ON c.UserId = u.Id
                            WHERE c.QuestionId = @QuestionId
                            ORDER BY c.CreatedAt ASC";
                var comments = (await connection.QueryAsync<Comment, User, Comment>(sql,
                    map: (comment, user) =>
                    {
                        comment.User = user;
                        return comment;
                    },
                    param: new { QuestionId = questionId },
                    splitOn: "Id")).ToList();

                _logger.LogInformation("GetQuestionCommentsAsync returned {Count} comments for QuestionId={QuestionId}", comments.Count, questionId);
                return comments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get comments for QuestionId={QuestionId}", questionId);
                throw;
            }
        }

        /// <summary>
        /// Retrieves comments for a given answer ordered by creation date (ascending).
        /// </summary>
        public async Task<IEnumerable<Comment>> GetAnswerCommentsAsync(int answerId)
        {
            _logger.LogDebug("GetAnswerCommentsAsync called for AnswerId={AnswerId}", answerId);
            try
            {
                using var connection = await _dbConnectionFactory.CreateConnectionAsync();
                var sql = @"SELECT c.*, u.* FROM Comments c INNER JOIN 
                            Users u ON c.UserId = u.Id
                            WHERE AnswerId = @AnswerId
                            ORDER BY c.CreatedAt ASC";
                var comments = (await connection.QueryAsync<Comment, User, Comment>(sql,
                    map: (comment, user) =>
                    {
                        comment.User = user;
                        return comment;
                    },
                    param: new { AnswerId = answerId },
                    splitOn: "Id")).ToList();

                _logger.LogInformation("GetAnswerCommentsAsync returned {Count} comments for AnswerId={AnswerId}", comments.Count, answerId);
                return comments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get comments for AnswerId={AnswerId}", answerId);
                throw;
            }
        }
    }
}
