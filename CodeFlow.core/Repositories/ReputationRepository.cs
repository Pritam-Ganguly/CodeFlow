using CodeFlow.core.Data;
using CodeFlow.core.Models;
using CodeFlow.core.Models.Mapping;
using Dapper;
using Microsoft.Extensions.Logging;

namespace CodeFlow.core.Repositories
{
    /// <summary>
    /// Handles reputation-related operations: calculating reputation, retrieving recent reputation history,
    /// and inserting reputation transactions (with side-effects such as updating user reputation and awarding badges).
    /// Methods include structured logging to aid diagnostics and observability.
    /// </summary>
    public class ReputationRepository : IReputationRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IBadgeRepository _badgeRepository;
        private readonly ILogger<ReputationRepository> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="ReputationRepository"/>.
        /// </summary>
        public ReputationRepository(IDbConnectionFactory dbConnectionFactory, IBadgeRepository badgeRepository, ILogger<ReputationRepository> logger)
        {
            _dbConnectionFactory = dbConnectionFactory ?? throw new ArgumentNullException(nameof(dbConnectionFactory));
            _badgeRepository = badgeRepository ?? throw new ArgumentNullException(nameof(badgeRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Returns the current reputation value for the specified user.
        /// </summary>
        public async Task<int> CalculateReputationAsync(int userId)
        {
            _logger.LogDebug("CalculateReputationAsync called for UserId={UserId}", userId);
            try
            {
                using var connection = await _dbConnectionFactory.CreateConnectionAsync();
                var sql = "SELECT Reputation FROM Users WHERE Id = @UserId";
                var reputation = await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId });
                _logger.LogInformation("CalculateReputationAsync UserId={UserId} Reputation={Reputation}", userId, reputation);
                return reputation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate reputation for UserId={UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Returns the most recent reputation transactions for the specified user (limited to 10).
        /// </summary>
        public async Task<IEnumerable<ReputationHistory>> GetReputationHistoryAsync(int userId)
        {
            _logger.LogDebug("GetReputationHistoryAsync called for UserId={UserId}", userId);
            try
            {
                using var connection = await _dbConnectionFactory.CreateConnectionAsync();
                var sql = "SELECT * FROM ReputationTransactions WHERE UserId = @UserId ORDER BY CreatedAt DESC LIMIT 10";
                var history = await connection.QueryAsync<ReputationHistory>(sql, param: new { UserId = userId });
                _logger.LogInformation("GetReputationHistoryAsync returned {Count} items for UserId={UserId}", history.AsList().Count, userId);
                return history;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get reputation history for UserId={UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Adds a reputation transaction for a user, updates the user's reputation, and checks/awards badges when thresholds are reached.
        /// Returns the inserted ReputationTransaction Id when successful.
        /// </summary>
        public async Task<int?> AddReputationTransactionAsync(
            int userId,
            ReputationTransactionTypes transaction,
            int relatedPostId,
            RelatedPostType relatedPostType,
            int? actingUserId = null)
        {
            _logger.LogDebug("AddReputationTransactionAsync called: UserId={UserId} Transaction={Transaction} RelatedPostId={RelatedPostId} RelatedPostType={RelatedPostType} ActingUserId={ActingUserId}",
                userId, transaction, relatedPostId, relatedPostType, actingUserId);

            try
            {
                if (!ReputationTransactionMap.ReputationMap.TryGetValue(transaction, out int amount))
                {
                    _logger.LogWarning("Unknown transaction type {Transaction} for UserId={UserId}", transaction, userId);
                    throw new ArgumentOutOfRangeException(nameof(transaction));
                }

                string transactionType = transaction.ToString();
                string postType = relatedPostType.ToString();

                var currentReputation = await CalculateReputationAsync(userId);
                _logger.LogInformation("UserId={UserId} current reputation before transaction: {CurrentReputation}. Transaction amount: {Amount}",
                    userId, currentReputation, amount);

                using var connection = await _dbConnectionFactory.CreateConnectionAsync();
                var sql = @"
                        -- Insert into reputation transaction
                        INSERT INTO ReputationTransactions (UserId, Amount, TransactionType, RelatedPostId, RelatedPostType, ActingUserId)
                        VALUES (@UserId, @Amount, @TransactionType, @RelatedPostId, @RelatedPostType, @ActingUserId) RETURNING Id;
                        
                        -- Update user's reputation
                        UPDATE Users SET Reputation = Reputation + @Amount WHERE Id = @UserId;";

                var insertedId = await connection.ExecuteScalarAsync<int>(sql, new
                {
                    UserId = userId,
                    Amount = amount,
                    TransactionType = transactionType,
                    RelatedPostId = relatedPostId,
                    RelatedPostType = postType,
                    ActingUserId = actingUserId,
                });

                _logger.LogInformation("Reputation transaction inserted. TransactionId={TransactionId} UserId={UserId} Amount={Amount}", insertedId, userId, amount);

                // Recalculate reputation after applying the transaction and check for badge thresholds.
                var updatedReputation = await CalculateReputationAsync(userId);
                _logger.LogInformation("UserId={UserId} reputation after transaction: {UpdatedReputation}", userId, updatedReputation);

                try
                {
                    // Badge checks are based on updated reputation; log which checks are evaluated.
                    if (updatedReputation >= 1000)
                    {
                        _logger.LogDebug("Checking badge: reached1000Reputation for UserId={UserId}", userId);
                        await _badgeRepository.CheckAndAwardBadge(userId, BadgeTriggerCondition.reached1000Reputation);
                    }
                    else if (updatedReputation >= 500)
                    {
                        _logger.LogDebug("Checking badge: reached500Reputation for UserId={UserId}", userId);
                        await _badgeRepository.CheckAndAwardBadge(userId, BadgeTriggerCondition.reached500Reputation);
                    }
                    else if (updatedReputation >= 100)
                    {
                        _logger.LogDebug("Checking badge: reached100Reputation for UserId={UserId}", userId);
                        await _badgeRepository.CheckAndAwardBadge(userId, BadgeTriggerCondition.reached100Reputation);
                    }
                }
                catch (Exception badgeEx)
                {
                    // Badge failures should not prevent reputation transactions; log and continue.
                    _logger.LogError(badgeEx, "Badge check/award failed for UserId={UserId} after transaction {TransactionId}", userId, insertedId);
                }

                return insertedId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add reputation transaction for UserId={UserId} Transaction={Transaction}", userId, transaction);
                throw;
            }
        }
    }
}
