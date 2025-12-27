using CodeFlow.core.Data;
using CodeFlow.core.Models;
using Dapper;
using CodeFlow.core.Models.Mapping;
using Microsoft.Extensions.Logging;
using CodeFlow.core.Servies;

namespace CodeFlow.core.Repositories
{
    /// <summary>
    /// Repository for CRUD operations and queries related to questions.
    /// Provides methods to create, read, update, search and delete questions.
    /// All public methods log entry, important events and errors to aid diagnosis.
    /// </summary>
    public class QuestionRepository : IQuestionRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IReputationRepository _reputationRepository;
        private readonly IUserActivityRepository _userActivityRepository;
        private readonly IMarkdownService _markDownServices;
        private readonly ILogger<QuestionRepository> _logger;

        public QuestionRepository(IDbConnectionFactory connectionFactory,
            IReputationRepository reputationRepository,
            IUserActivityRepository userActivityRepository,
            IMarkdownService markDownServices,
            ILogger<QuestionRepository> logger)
        {
            _connectionFactory = connectionFactory;
            _reputationRepository = reputationRepository;
            _userActivityRepository = userActivityRepository;
            _markDownServices = markDownServices;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new question and records reputation and user activity where applicable.
        /// Returns the new question Id.
        /// </summary>
        public async Task<int> CreateAsync(Question question)
        {
            _logger.LogDebug("CreateAsync called for UserId={UserId}, Title={Title}", question.UserId, question.Title);
            try
            {
                var html = _markDownServices.ToHTML(question.BodyMarkdown);
                question.BodyHtml = html;

                using var connection = await _connectionFactory.CreateConnectionAsync();
                var sql = @"
                INSERT INTO Questions (Title, BodyMarkDown, BodyHTML, UserId)
                VALUES (@Title, @BodyMarkDown, @BodyHTML, @UserId)
                RETURNING Id;";
                var isFirstPost = await FirstPost(question.UserId);
                var newId = await connection.ExecuteScalarAsync<int>(sql, question);
                _logger.LogInformation("Question created with Id={QuestionId} for UserId={UserId}", newId, question.UserId);

                if (isFirstPost)
                {
                    _logger.LogInformation("UserId={UserId} made their first post. Adding reputation transaction.", question.UserId);
                    await _reputationRepository.AddReputationTransactionAsync(question.UserId, ReputationTransactionTypes.First_Post, question.Id, RelatedPostType.Question);
                }

                await _userActivityRepository.AddUserActivityAsync(question.UserId, ActivityType.question_asked, TargetEntityType.question, newId);
                _logger.LogDebug("User activity recorded for UserId={UserId} and QuestionId={QuestionId}", question.UserId, newId);

                return newId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create question for UserId={UserId}", question.UserId);
                throw;
            }
        }

        /// <summary>
        /// Retrieves a question by id including the posting user.
        /// Returns null if not found.
        /// </summary>
        public async Task<Question?> GetByIdAsync(int id)
        {
            _logger.LogDebug("GetByIdAsync called with Id={Id}", id);
            try
            {
                var sql = @"
            SELECT q.*, u.* 
            FROM Questions q 
            INNER JOIN Users u ON q.UserID = u.Id
            WHERE q.Id = @Id";

                using var connection = await _connectionFactory.CreateConnectionAsync();
                var questions = await connection.QueryAsync<Question, User, Question>(sql, map: (question, user) =>
                {
                    question.User = user;
                    return question;
                },
                param: new { Id = id },
                 splitOn: "Id"
                );

                var result = questions.SingleOrDefault();
                if (result != null)
                    _logger.LogInformation("GetByIdAsync found question Id={Id} (UserId={UserId})", id, result.UserId);
                else
                    _logger.LogInformation("GetByIdAsync did not find question Id={Id}", id);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get question by Id={Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Retrieves a question by id including user and tags.
        /// Returns null if not found.
        /// </summary>
        public async Task<Question?> GetByIdWithTagsAsync(int id)
        {
            _logger.LogDebug("GetByIdWithTagsAsync called with Id={Id}", id);
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                var sql = @"SELECT q.*, u.*, t.*
                        FROM Questions q
                        INNER JOIN Users u ON q.UserId = u.Id
                        LEFT JOIN QuestionTags qt ON q.Id = qt.QuestionId
                        LEFT JOIN Tags t ON qt.TagId = t.Id
                        WHERE q.Id = @Id";
                Question? question = null;
                var results = await connection.QueryAsync<Question, User, Tag, Question>(
                    sql,
                    map: (q, u, t) =>
                    {
                        if (question == null)
                        {
                            question = q;
                            question.User = u;
                        }
                        if (t != null && t.Id != 0)
                        {
                            question.Tags.Add(t);
                        }

                        return question;
                    },
                    param: new { Id = id },
                    splitOn: "Id, Id"
                );

                var res = results.FirstOrDefault();
                if (res != null)
                    _logger.LogInformation("GetByIdWithTagsAsync found Id={Id} with {TagCount} tags", id, res.Tags?.Count ?? 0);
                else
                    _logger.LogInformation("GetByIdWithTagsAsync did not find Id={Id}", id);

                return res;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get question with tags by Id={Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Returns recent questions ordered by created date descending.
        /// </summary>
        public async Task<IEnumerable<Question>> GetRecentAsync(int pageNumber = 1, int pageSize = 10, QuestionSortType sortBy = QuestionSortType.Newest)
        {
            _logger.LogDebug("GetRecentAsync called with limit={Limit}", pageSize);
            try
            {
                var sql = @"
                SELECT q.*, u.*
                FROM Questions q
                INNER JOIN Users u ON q.UserId = u.Id";

                sql += sortBy switch
                {
                    QuestionSortType.Oldest => @" ORDER BY q.CreatedAt",
                    QuestionSortType.Score => @" ORDER BY q.Score",
                    _ => @" ORDER BY q.CreatedAt DESC"
                };

                sql += @" LIMIT @PageSize OFFSET @Offset";

                var offset = (pageNumber = 1) * pageSize;
                using var connection = await _connectionFactory.CreateConnectionAsync();
                var results = await connection.QueryAsync<Question, User, Question>(sql, map: (question, user) =>
                {
                    question.User = user;
                    return question;
                },
                param: new { PageSize = pageSize, Offset = offset},
                splitOn: "Id");

                var count = results?.Count() ?? 0;
                _logger.LogInformation("GetRecentAsync returned {Count} questions (limit {Limit})", count, pageSize);
                return results ?? [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get recent questions (limit={Limit})", pageSize);
                throw;
            }
        }

        public async Task<int> GetAllQuestions()
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                var sql = @"SELECT COUNT(*) FROM Questions";
                return await connection.ExecuteScalarAsync<int>(sql);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get questions count");
                throw;
            }
        }

        /// <summary>
        /// Returns recent questions with their tags.
        /// </summary>
        public async Task<IEnumerable<Question>> GetRecentWithTagsAsync(int pageNumber = 1, int pageSize = 10, QuestionSortType sortType = QuestionSortType.Newest)
        {
            _logger.LogDebug("GetRecentWithTagsAsync called with limit={limit}", pageSize);
            try
            {
                var offset = (pageNumber - 1) * pageSize;
                using var connection = await _connectionFactory.CreateConnectionAsync();

                var sql = @"SELECT q.*, u.* FROM Questions q INNER JOIN Users u ON q.UserId = u.Id";

                sql += sortType switch
                {
                    QuestionSortType.Oldest => " ORDER BY q.CreatedAt",
                    QuestionSortType.Score => " ORDER BY q.Score",
                    _ => " ORDER BY q.CreatedAt DESC"
                };

                sql += " LIMIT @PageSize OFFSET @Offset";

                var questions = await connection.QueryAsync<Question, User, Question>(
                    sql,
                    map: (question, user) =>
                    {
                        question.User = user;
                        return question;
                    },
                    param: new {
                        PageSize = pageSize,
                        Offset = offset
                    },
                    splitOn: "Id,Id"
                );

                var questionIds = questions.Select(q => q.Id).ToList();
                _logger.LogDebug("GetRecentWithTagsAsync retrieved {QuestionCount} questions; fetching tags for {QuestionIdCount} ids", questionIds.Count, questionIds.Count);

                if (questionIds.Count != 0)
                {
                    var tagsSql = @"SELECT qt.QuestionId, t.* FROM QuestionTags qt INNER JOIN Tags t ON qt.TagId = t.Id WHERE qt.QuestionId = ANY(@QuestionIds)";

                    var tagsByQuestionId = await connection.QueryAsync<int, Tag, (int QuestionId, Tag Tag)>(
                        tagsSql,
                        map: (questionId, tag) => (questionId, tag),
                        param: new { QuestionIds = questionIds.ToArray() },
                        splitOn: "Id"
                    );

                    var tagsLookup = tagsByQuestionId.ToLookup(x => x.QuestionId, x => x.Tag);
                    foreach (var question in questions)
                    {
                        if (tagsLookup.Contains(question.Id))
                        {
                            var tags = tagsLookup[question.Id].ToList();
                            question.Tags.AddRange(tags);
                            _logger.LogDebug("Attached {TagCount} tags to QuestionId={QuestionId}", tags.Count, question.Id);
                        }
                    }
                }

                _logger.LogInformation("GetRecentWithTagsAsync returning {Count} questions (limit {Limit})", questions.Count(), pageSize);
                return questions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get recent questions with tags (limit={Limit})", pageSize);
                throw;
            }
        }

        public async Task<int> GetAllResult(string searchQuery)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                var sql = @"
                SELECT COUNT(*) FROM Questions
                WHERE SearchVector @@ plainto_tsquery('english', @SearchQuery)";
                return await connection.ExecuteScalarAsync<int>(sql, new
                {
                    SearchQuery = searchQuery
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get result count");
                throw;
            }
        }

        /// <summary>
        /// Searches questions using full text search; returns recent questions if query is empty.
        /// </summary>
        public async Task<IEnumerable<Question>> SearchAsync(string searchQuery, int pageNumber = 1, int pageSize = 10, QuestionSortType sortType = QuestionSortType.Newest)
        {
            _logger.LogDebug("SearchAsync called with query='{SearchQuery}'", searchQuery);
            try
            {
                if (string.IsNullOrWhiteSpace(searchQuery))
                {
                    _logger.LogInformation("SearchAsync empty query - returning recent questions");
                    return await GetRecentAsync(pageNumber, pageSize, sortType);
                }

                var offset = (pageNumber - 1) * pageSize;
                using var connection = await _connectionFactory.CreateConnectionAsync();
                var sql = @"SELECT q.*, u.* FROM Questions q INNER JOIN Users u ON q.UserId = u.Id WHERE q.SearchVector @@ plainto_tsquery('english', @SearchQuery)
                        ORDER BY ts_rank(q.SearchVector, plainto_tsquery('english', @SearchQuery)) DESC,";

                sql += sortType switch
                {
                    QuestionSortType.Oldest => " q.CreatedAt",
                    QuestionSortType.Score => " q.Score",
                    _ => " q.CreatedAt DESC"
                };

                sql += @" LIMIT @PageSize OFFSET @Offset";

                Console.WriteLine(sql);

                var results = await connection.QueryAsync<Question, User, Question>(
                    sql,
                    map: (question, user) =>
                    {
                        question.User = user;
                        return question;
                    },
                    param: new { SearchQuery = searchQuery, PageSize = pageSize, Offset = offset },
                    splitOn: "Id");

                _logger.LogInformation("SearchAsync returned {Count} results for query='{SearchQuery}'", results.Count(), searchQuery);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search questions for query='{SearchQuery}'", searchQuery);
                throw;
            }
        }

        /// <summary>
        /// Gets all questions for a given user.
        /// </summary>
        public async Task<IEnumerable<Question>> GetAllQuestionsByUserId(int userId, int pageNumber = 1, int pageSize = 5)
        {
            _logger.LogDebug("GetAllQuestionsByUserId called for UserId={UserId}", userId);
            try
            {
                int offset = (pageNumber - 1) * pageSize;
                using var connection = await _connectionFactory.CreateConnectionAsync();
                var sql = @"SELECT q.* FROM Questions q 
                            INNER JOIN Users u ON q.UserId = u.Id
                            WHERE q.UserId = @UserId 
                            ORDER BY q.CreatedAt DESC
                            LIMIT @PageSize OFFSET @Offset;";
                var results = await connection.QueryAsync<Question>(sql, param: new
                {
                    UserId = userId,
                    PageSize = pageSize,
                    Offset = offset
                });

                _logger.LogInformation("GetAllQuestionsByUserId returned {Count} questions for UserId={UserId}", results.Count(), userId);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all questions for UserId={UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Gets the current vote type (if any) that a user has cast on a question.
        /// </summary>
        public async Task<int?> CurrentVoteAsync(int userId, int quesitonId)
        {
            _logger.LogDebug("CurrentVoteAsync called for UserId={UserId}, QuestionId={QuestionId}", userId, quesitonId);
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                var sql = @"SELECT VoteType FROM Votes WHERE UserId = @UserId AND QuestionId = @QuestionId";
                var existingVote = await connection.QueryFirstOrDefaultAsync<int?>(sql, new
                {
                    UserId = userId,
                    QuestionId = quesitonId
                });
                _logger.LogInformation("CurrentVoteAsync UserId={UserId}, QuestionId={QuestionId}, VoteType={VoteType}", userId, quesitonId, existingVote);
                return existingVote;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get current vote for UserId={UserId}, QuestionId={QuestionId}", userId, quesitonId);
                throw;
            }
        }

        /// <summary>
        /// Gets the current vote type (if any) that a user has cast on an answer item.
        /// </summary>
        public async Task<int?> CurrentVoteForAnswerItemAsync(int userId, int answerId)
        {
            _logger.LogDebug("CurrentVoteForAnswerItemAsync called for UserId={UserId}, AnswerId={AnswerId}", userId, answerId);
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                var sql = @"SELECT VoteType FROM Votes WHERE UserId = @UserId AND AnswerId = @AnswerId";
                var existingVote = await connection.QueryFirstOrDefaultAsync<int?>(sql, new
                {
                    UserId = userId,
                    AnswerId = answerId
                });
                _logger.LogInformation("CurrentVoteForAnswerItemAsync UserId={UserId}, AnswerId={AnswerId}, VoteType={VoteType}", userId, answerId, existingVote);
                return existingVote;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get current vote for answer UserId={UserId}, AnswerId={AnswerId}", userId, answerId);
                throw;
            }
        }

        /// <summary>
        /// Updates a question's title and body. Returns number of affected rows.
        /// </summary>
        public async Task<int> UpdateQuestionAsync(int questionId, string newTitle, string newBodyMarkdown)
        {
            _logger.LogDebug("UpdateQuestionAsync called for QuestionId={QuestionId}", questionId);
            try
            {
                var html = _markDownServices.ToHTML(newBodyMarkdown);
                using var connection = await _connectionFactory.CreateConnectionAsync();
                Question? question = await GetByIdWithTagsAsync(questionId);
                var sql = @"UPDATE Questions SET Title = @Title, BodyMarkDown = @BodyMarkDown, BodyHTML = @BodyHTML WHERE Questions.Id = @Id";

                var affected = await connection.ExecuteAsync(sql, new
                {
                    Title = newTitle,
                    BodyMarkDown = newBodyMarkdown,
                    BodyHTML = html,
                    Id = questionId
                });

                _logger.LogInformation("UpdateQuestionAsync updated QuestionId={QuestionId}. RowsAffected={RowsAffected}", questionId, affected);
                return affected;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update question QuestionId={QuestionId}", questionId);
                throw;
            }
        }

        /// <summary>
        /// Determines if this is the user's first post.
        /// </summary>
        public async Task<bool> FirstPost(int userId)
        {
            _logger.LogDebug("FirstPost called for UserId={UserId}", userId);
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                var sql = @"SELECT COUNT(*) FROM Questions WHERE UserId = @UserId";
                var postCount = await connection.ExecuteScalarAsync<int>(sql, new
                {
                    UserId = userId
                });
                var isFirst = postCount == 0;
                _logger.LogInformation("FirstPost UserId={UserId} PostCount={PostCount} IsFirst={IsFirst}", userId, postCount, isFirst);
                return isFirst;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to determine if first post for UserId={UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Deletes a question and records reputation change. Returns true when a row was deleted.
        /// </summary>
        public async Task<bool> DeleteQuestionAsync(int questionId, int userId)
        {
            _logger.LogInformation("DeleteQuestionAsync called for QuestionId={QuestionId} by UserId={UserId}", questionId, userId);
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                var sql = @"DELETE FROM Questions WHERE Id = @Id";
                var question = await GetByIdAsync(questionId);
                if (question!.UserId == userId)
                {
                    _logger.LogInformation("UserId={UserId} deleting their own question Id={QuestionId} - applying Post_Delete reputation change", userId, questionId);
                    await _reputationRepository.AddReputationTransactionAsync(userId, ReputationTransactionTypes.Post_Delete, questionId, RelatedPostType.Question);
                }
                else
                {
                    _logger.LogInformation("UserId={ActingUserId} deleted question Id={QuestionId} owned by UserId={OwnerId} - applying Post_Got_Deleted reputation change", userId, questionId, question.UserId);
                    await _reputationRepository.AddReputationTransactionAsync(question.UserId, ReputationTransactionTypes.Post_Got_Deleted, questionId, RelatedPostType.Question, userId);
                }
                int alteredRows = await connection.ExecuteAsync(sql, new
                {
                    Id = questionId
                });
                _logger.LogInformation("DeleteQuestionAsync completed for QuestionId={QuestionId}. RowsAffected={RowsAffected}", questionId, alteredRows);
                return alteredRows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete question QuestionId={QuestionId} by UserId={UserId}", questionId, userId);
                throw;
            }
        }

    }
}