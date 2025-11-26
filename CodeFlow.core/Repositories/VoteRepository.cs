using CodeFlow.core.Data;
using CodeFlow.core.Models;
using CodeFlow.core.Models.Mapping;
using Dapper;
using Microsoft.Extensions.Logging;

namespace CodeFlow.core.Repositories
{
    /// <summary>
    /// Repository responsible for persisting and handling votes and keeping scores/reputation in sync.
    /// Provides methods for adding question/answer votes and updating aggregate scores.
    /// All public methods include structured logging for diagnostics and error reporting.
    /// </summary>
    public class VoteRepository : IVoteRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IReputationRepository _reputationRepository;
        private readonly IQuestionRepository _questionRepository;
        private readonly IAnswerRepository _answerRepository;
        private readonly ILogger<VoteRepository> _logger;

        /// <summary>
        /// Creates a new instance of <see cref="VoteRepository"/>.
        /// </summary>
        public VoteRepository(
            IDbConnectionFactory connectionFactory,
            IReputationRepository reputationRepository,
            IQuestionRepository questionRepository,
            IAnswerRepository answerRepository,
            ILogger<VoteRepository> logger)
        {
            _connectionFactory = connectionFactory;
            _reputationRepository = reputationRepository;
            _questionRepository = questionRepository;
            _answerRepository = answerRepository;
            _logger = logger;
        }

        /// <summary>
        /// Adds or updates a vote on a question and records reputation transactions where applicable.
        /// Returns true when the vote insert/update affected at least one row.
        /// </summary>
        public async Task<bool> AddQuestionVoteAsync(Vote vote)
        {
            _logger.LogDebug("AddQuestionVoteAsync called: UserId={UserId}, QuestionId={QuestionId}, VoteType={VoteType}", vote.UserId, vote.Questionid, vote.VoteType);

            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                var sql = @"
                INSERT INTO Votes (VoteType, UserId, QuestionId, AnswerId)
                VALUES (@VoteType, @UserId, @QuestionId, @AnswerId)
                ON CONFLICT ON CONSTRAINT UQ_User_Question DO UPDATE
                SET VoteType = EXCLUDED.VoteType
                WHERE Votes.QuestionId IS NOT NULL;";

                var rowsAffected = await connection.ExecuteAsync(sql, vote);
                _logger.LogInformation("Vote persisted for QuestionId={QuestionId} by UserId={UserId}. RowsAffected={RowsAffected}", vote.Questionid, vote.UserId, rowsAffected);

                var question = await _questionRepository.GetByIdAsync(vote.Questionid ?? 0);

                if (vote.VoteType == 1)
                {
                    _logger.LogInformation("Processing question upvote. QuestionOwnerUserId={OwnerId}, ActingUserId={ActingUserId}", question?.UserId, vote.UserId);
                    await _reputationRepository.AddReputationTransactionAsync(
                        question!.UserId, ReputationTransactionTypes.Question_Upvoted, vote.Questionid ?? 0, RelatedPostType.Question, vote.UserId);
                }
                else
                {
                    _logger.LogInformation("Processing question downvote. QuestionOwnerUserId={OwnerId}, ActingUserId={ActingUserId}", question?.UserId, vote.UserId);
                    await _reputationRepository.AddReputationTransactionAsync(
                        question!.UserId, ReputationTransactionTypes.Question_Downvoted, vote.Questionid ?? 0, RelatedPostType.Question, vote.UserId);
                    await _reputationRepository.AddReputationTransactionAsync(
                        vote.UserId, ReputationTransactionTypes.Downvote_On_Post, vote.Questionid ?? 0, RelatedPostType.Question);
                }

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add/update question vote. UserId={UserId}, QuestionId={QuestionId}", vote.UserId, vote.Questionid);
                throw;
            }
        }

        /// <summary>
        /// Adds or updates a vote on an answer and records reputation transactions where applicable.
        /// Returns true when the vote insert/update affected at least one row.
        /// </summary>
        public async Task<bool> AddAnswerVoteAsync(Vote vote)
        {
            _logger.LogDebug("AddAnswerVoteAsync called: UserId={UserId}, AnswerId={AnswerId}, VoteType={VoteType}", vote.UserId, vote.AnswerId, vote.VoteType);

            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                var sql = @"
                INSERT INTO Votes (VoteType, UserId, QuestionId, AnswerId)
                VALUES (@VoteType, @UserId, @QuestionId, @AnswerId)
                ON CONFLICT ON CONSTRAINT UQ_User_Answer DO UPDATE
                SET VoteType = EXCLUDED.VoteType
                WHERE Votes.AnswerId IS NOT NULL;";

                var rowsAffected = await connection.ExecuteAsync(sql, vote);
                _logger.LogInformation("Vote persisted for AnswerId={AnswerId} by UserId={UserId}. RowsAffected={RowsAffected}", vote.AnswerId, vote.UserId, rowsAffected);

                var answer = await _answerRepository.GetByIdAsync(vote.AnswerId ?? 0);

                if (vote.VoteType == 1)
                {
                    _logger.LogInformation("Processing answer upvote. AnswerOwnerUserId={OwnerId}, ActingUserId={ActingUserId}", answer?.UserId, vote.UserId);
                    await _reputationRepository.AddReputationTransactionAsync(
                        answer!.UserId, ReputationTransactionTypes.Answer_Upvoted, vote.AnswerId ?? 0, RelatedPostType.Answer, vote.UserId);
                }
                else
                {
                    _logger.LogInformation("Processing answer downvote. AnswerOwnerUserId={OwnerId}, ActingUserId={ActingUserId}", answer?.UserId, vote.UserId);
                    await _reputationRepository.AddReputationTransactionAsync(
                        answer!.UserId, ReputationTransactionTypes.Answer_Downvoted, vote.AnswerId ?? 0, RelatedPostType.Answer, vote.UserId);
                    await _reputationRepository.AddReputationTransactionAsync(
                        vote.UserId, ReputationTransactionTypes.Downvote_On_Post, vote.AnswerId ?? 0, RelatedPostType.Answer);
                }

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add/update answer vote. UserId={UserId}, AnswerId={AnswerId}", vote.UserId, vote.AnswerId);
                throw;
            }
        }

        /// <summary>
        /// Recalculates and returns the score for an answer by summing its votes.
        /// </summary>
        public async Task<int> UpdateScoreForAnswerAsync(int answerId)
        {
            _logger.LogDebug("UpdateScoreForAnswerAsync called for AnswerId={AnswerId}", answerId);

            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                var sql = @"UPDATE Answers SET Score = COALESCE((SELECT SUM(VoteType) FROM Votes WHERE AnswerId = @AnswerId), 0) 
                            WHERE Id = @AnswerId; 
                            SELECT Score FROM Answers WHERE Id = @AnswerId;";
                var newScore = await connection.ExecuteScalarAsync<int>(sql, new { AnswerId = answerId });
                _logger.LogInformation("Updated score for AnswerId={AnswerId}. NewScore={NewScore}", answerId, newScore);
                return newScore;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update score for AnswerId={AnswerId}", answerId);
                throw;
            }
        }

        /// <summary>
        /// Recalculates and returns the score for a question by summing its votes.
        /// </summary>
        public async Task<int> UpdateScoreForQuestionAsync(int questionId)
        {
            _logger.LogDebug("UpdateScoreForQuestionAsync called for QuestionId={QuestionId}", questionId);

            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                var sql = @"UPDATE Questions SET Score = COALESCE((SELECT SUM(VoteType) FROM Votes WHERE QuestionId = @QuestionId), 0) 
                            WHERE Id = @QuestionId; 
                            SELECT Score FROM Questions WHERE Id = @QuestionId;";
                var newScore = await connection.ExecuteScalarAsync<int>(sql, new { QuestionId = questionId });
                _logger.LogInformation("Updated score for QuestionId={QuestionId}. NewScore={NewScore}", questionId, newScore);
                return newScore;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update score for QuestionId={QuestionId}", questionId);
                throw;
            }
        }
    }
}
