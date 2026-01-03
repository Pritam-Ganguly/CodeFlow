using CodeFlow.core.Data;
using CodeFlow.core.Models;
using CodeFlow.core.Models.Mapping;
using CodeFlow.core.Servies;
using Dapper;
using Microsoft.Extensions.Logging;

namespace CodeFlow.core.Repositories
{
    /// <summary>
    /// Repository for CRUD operations and queries related to answers.
    /// Provides methods to create, read, update, accept and delete answers.
    /// Methods include structured logging for entry, important events and errors.
    /// </summary>
    public class AnswerRepository : IAnswerRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IReputationRepository _reputationRepository;
        private readonly IMarkdownService _markdownService;
        private readonly ILogger<AnswerRepository> _logger;

        /// <summary>
        /// Creates a new instance of <see cref="AnswerRepository"/>.
        /// </summary>
        public AnswerRepository(IDbConnectionFactory connectionFactory, IReputationRepository reputationRepository, IMarkdownService markdownService, ILogger<AnswerRepository> logger)
        {
            _connectionFactory = connectionFactory;
            _reputationRepository = reputationRepository;
            _markdownService = markdownService;
            _logger = logger;
        }

        /// <summary>
        /// Inserts a new answer and returns its generated Id.
        /// </summary>
        public async Task<int> CreateAsync(Answer answer)
        {
            _logger.LogDebug("CreateAsync called for QuestionId={QuestionId}, UserId={UserId}", answer.QuestionId, answer.UserId);
            try
            {
                var html = _markdownService.ToHTML(answer.BodyMarkdown);
                answer.BodyHtml = html;
                var sql = @"
                    INSERT INTO Answers (BodyMarkDown, BodyHTML, QuestionId, UserId)
                    VALUES (@BodyMarkDown, @BodyHTML, @QuestionId, @UserId)
                    RETURNING Id;";

                using var connection = await _connectionFactory.CreateConnectionAsync();
                var newId = await connection.ExecuteScalarAsync<int>(sql, answer);
                _logger.LogInformation("Answer created with Id={AnswerId} for QuestionId={QuestionId} by UserId={UserId}", newId, answer.QuestionId, answer.UserId);
                return newId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create answer for QuestionId={QuestionId}, UserId={UserId}", answer.QuestionId, answer.UserId);
                throw;
            }
        }

        /// <summary>
        /// Retrieves an answer by id. Returns null when not found.
        /// </summary>
        public async Task<Answer?> GetByIdAsync(int id)
        {
            _logger.LogDebug("GetByIdAsync called with Id={AnswerId}", id);
            try
            {
                var sql = "SELECT * FROM Answers WHERE Id = @Id";
                using var connection = await _connectionFactory.CreateConnectionAsync();
                var answer = await connection.QueryFirstOrDefaultAsync<Answer>(sql, param: new { Id = id });
                if (answer != null)
                    _logger.LogInformation("GetByIdAsync found AnswerId={AnswerId} (QuestionId={QuestionId}, UserId={UserId})", id, answer.QuestionId, answer.UserId);
                else
                    _logger.LogInformation("GetByIdAsync did not find AnswerId={AnswerId}", id);
                return answer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get answer by Id={AnswerId}", id);
                throw;
            }
        }

        /// <summary>
        /// Returns all answers for a question ordered by score desc then creation asc.
        /// </summary>
        public async Task<IEnumerable<Answer>> GetByQuestionIdAsync(int questionId, int pageSize = 5, int pageNumber = 1) 
        {
            _logger.LogDebug("GetByQuestionIdAsync called for QuestionId={QuestionId}", questionId);
            try
            {
                int offset = (pageNumber - 1) * pageSize;
                var sql = @"
                        SELECT a.*, u.*
                        FROM Answers a
                        INNER JOIN Users u ON a.UserId = u.Id
                        WHERE a.QuestionId = @QuestionId
                        ORDER BY a.Score DESC, a.CreatedAt DESC LIMIT @PageSize OFFSET @Offset";

                using var connection = await _connectionFactory.CreateConnectionAsync();
                var answers = await connection.QueryAsync<Answer, User, Answer>(sql, map: (answer, user) =>
                {
                    answer.User = user;
                    return answer;
                },
                param: new { QuestionId = questionId, PageSize = pageSize, Offset = offset},
                splitOn: "Id");

                var count = answers?.Count() ?? 0;
                _logger.LogInformation("GetByQuestionIdAsync returned {Count} answers for QuestionId={QuestionId}", count, questionId);
                return answers ?? [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get answers for QuestionId={QuestionId}", questionId);
                throw;
            }
        }
        /// <summary>
        /// Returns total answers count for a give question id.
        /// </summary>
        public async Task<int> GetTotalAnswerCountForQuestionid(int questionId)
        {
            try{
                var sql = @"SELECT Count(*) FROM Answers a WHERE a.QuestionId = @QuestionId";

                using var connection = await _connectionFactory.CreateConnectionAsync();
                var answers = await connection.ExecuteScalarAsync<int>(sql, new
                {
                    QuestionId = questionId
                });
                return answers;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Failed to get answers count for QuestionId={QuestionId}", questionId);
                throw;
            }
        }

        /// <summary>
        /// Returns the question that an answer belongs to.
        /// </summary>
        public async Task<Question?> GetQuestionByAnswerIdAsync(int answerId)
        {
            _logger.LogDebug("GetQuestionByAnswerIdAsync called for AnswerId={AnswerId}", answerId);
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                var sql = "SELECT q.* FROM Questions q INNER JOIN Answers a ON q.Id = a.QuestionId WHERE a.Id = @Id";
                var question = await connection.QueryFirstOrDefaultAsync<Question>(sql, new { Id = answerId });
                if (question != null)
                    _logger.LogInformation("GetQuestionByAnswerIdAsync found QuestionId={QuestionId} for AnswerId={AnswerId}", question.Id, answerId);
                else
                    _logger.LogInformation("GetQuestionByAnswerIdAsync did not find a question for AnswerId={AnswerId}", answerId);
                return question;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get question for AnswerId={AnswerId}", answerId);
                throw;
            }
        }

        /// <summary>
        /// Marks an answer accepted and awards reputation to the answer owner.
        /// Returns number of affected rows from the update.
        /// </summary>
        public async Task<int> AcceptAnswer(int answerId)
        {
            _logger.LogDebug("AcceptAnswer called for AnswerId={AnswerId}", answerId);
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                var sql = @"UPDATE Answers SET IsAccepted = TRUE WHERE Id = @Id";

                var answer = await GetByIdAsync(answerId);
                var question = await GetQuestionByAnswerIdAsync(answerId);

                if (answer == null || question == null)
                {
                    _logger.LogWarning("AcceptAnswer could not find answer or question for AnswerId={AnswerId}. AnswerFound={AnswerFound}, QuestionFound={QuestionFound}", answerId, answer != null, question != null);
                }
                else
                {
                    try
                    {
                        await _reputationRepository.AddReputationTransactionAsync(
                            answer.UserId, ReputationTransactionTypes.Answer_Accepted, question.Id, RelatedPostType.Question);
                        _logger.LogInformation("Reputation transaction added for UserId={UserId} for accepted AnswerId={AnswerId}", answer.UserId, answerId);
                    }
                    catch (Exception repEx)
                    {
                        // log reputation errors but continue to mark answer accepted (to avoid inconsistent UI state)
                        _logger.LogError(repEx, "Failed to add reputation transaction for AnswerId={AnswerId}, UserId={UserId}", answerId, answer.UserId);
                    }
                }

                var rows = await connection.ExecuteAsync(sql, new { Id = answerId });
                _logger.LogInformation("AcceptAnswer updated AnswerId={AnswerId}. RowsAffected={Rows}", answerId, rows);
                return rows;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to accept answer AnswerId={AnswerId}", answerId);
                throw;
            }
        }

        /// <summary>
        /// Edits an existing answer's body. Returns number of affected rows (nullable).
        /// </summary>
        public async Task<int?> EditAnswerAsync(int answerId, string bodyMarkDown)
        {
            _logger.LogDebug("EditAnswerAsync called for AnswerId={AnswerId}", answerId);
            try
            {
                var html = _markdownService.ToHTML(bodyMarkDown);
                using var connection = await _connectionFactory.CreateConnectionAsync();
                var sql = @"UPDATE Answers SET BodyMarkDown = @BodyMarkDown, BodyHTML = @BodyHTML WHERE Id = @Id";
                var rows = await connection.ExecuteAsync(sql, new { BodyMarkDown = bodyMarkDown, BodyHTML = html, Id = answerId });
                _logger.LogInformation("EditAnswerAsync updated AnswerId={AnswerId}. RowsAffected={Rows}", answerId, rows);
                return rows;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit answer AnswerId={AnswerId}", answerId);
                throw;
            }
        }

        /// <summary>
        /// Deletes an answer. Returns true when a row was deleted.
        /// </summary>
        public async Task<bool> DeleteAnswerAsync(int answerId)
        {
            _logger.LogInformation("DeleteAnswerAsync called for AnswerId={AnswerId}", answerId);
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                var sql = @"DELETE FROM Answers WHERE Id = @Id";
                var rowsAltered = await connection.ExecuteAsync(sql, new { Id = answerId });
                _logger.LogInformation("DeleteAnswerAsync completed for AnswerId={AnswerId}. RowsAffected={Rows}", answerId, rowsAltered);
                return rowsAltered > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete answer AnswerId={AnswerId}", answerId);
                throw;
            }
        }

        /// <summary>
        /// Returns the number of accepted answers for a given user.
        /// </summary>
        public async Task<int> GetAcceptedAnswerCountByUserIdAsync(int userId)
        {
            _logger.LogDebug("GetAcceptedAnswerCountByUserIdAsync called for UserId={UserId}", userId);
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                var sql = @"SELECT COUNT(*) FROM Answers WHERE UserId = @UserId AND IsAccepted = TRUE";
                var count = await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId });
                _logger.LogInformation("GetAcceptedAnswerCountByUserIdAsync UserId={UserId} AcceptedCount={Count}", userId, count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get accepted answer count for UserId={UserId}", userId);
                throw;
            }
        }
    }
}