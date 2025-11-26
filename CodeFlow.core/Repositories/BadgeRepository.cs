using CodeFlow.core.Data;
using CodeFlow.core.Models;
using Dapper;
using Microsoft.Extensions.Logging;

namespace CodeFlow.core.Repositories
{
    /// <summary>
    /// Repository responsible for CRUD operations related to badges and awarding badges to users.
    /// Provides methods to retrieve, create, and award badges. Methods include structured logging
    /// to help trace execution and surface failures.
    /// </summary>
    public class BadgeRepository : IBadgeRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ILogger<BadgeRepository> _logger;

        /// <summary>
        /// Creates a new instance of <see cref="BadgeRepository"/>.
        /// </summary>
        public BadgeRepository(IDbConnectionFactory dbConnectionFactory, ILogger<BadgeRepository> logger)
        {
            _dbConnectionFactory = dbConnectionFactory ?? throw new ArgumentNullException(nameof(dbConnectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Returns all badges available in the system.
        /// </summary>
        public async Task<IEnumerable<Badge>> GetAllBadgesAsync()
        {
            _logger.LogDebug("GetAllBadgesAsync called");
            try
            {
                using var connection = await _dbConnectionFactory.CreateConnectionAsync();
                var sql = "SELECT * FROM Badges";
                var badges = (await connection.QueryAsync<Badge>(sql)).ToList();
                _logger.LogInformation("GetAllBadgesAsync returned {Count} badges", badges.Count);
                return badges;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve all badges");
                throw;
            }
        }

        /// <summary>
        /// Inserts a new badge and returns the new badge id.
        /// </summary>
        public async Task<int?> AddBadgeAsync(Badge badge)
        {
            _logger.LogDebug("AddBadgeAsync called: Name={Name} TriggerCondition={TriggerCondition}", badge?.Name, badge?.TriggerCondition);
            if (badge is null)
            {
                throw new ArgumentNullException(nameof(badge));
            }

            try
            {
                using var connection = await _dbConnectionFactory.CreateConnectionAsync();
                var sql = @"INSERT INTO Badges (Name, Description, IconURL, BadgeType, TriggerCondition)
                            VALUES(@Name, @Description, @IconURL, @BadgeType, @TriggerCondition)
                            RETURNING Id;";
                var id = await connection.ExecuteScalarAsync<int>(sql, new
                {
                    badge.Name,
                    badge.Description,
                    badge.IconUrl,
                    BadgeType = badge.BadgeType.ToString(),
                    TriggerCondition = badge.TriggerCondition.ToString(),
                });

                _logger.LogInformation("AddBadgeAsync created badge Id={BadgeId} Name={Name}", id, badge.Name);
                return id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add badge Name={Name}", badge.Name);
                throw;
            }
        }

        /// <summary>
        /// Retrieves a badge that matches the specified trigger condition, or null when none exists.
        /// </summary>
        public async Task<Badge?> GetBadgeByTriggerConditionAsync(BadgeTriggerCondition triggerCondition)
        {
            _logger.LogDebug("GetBadgeByTriggerConditionAsync called: TriggerCondition={TriggerCondition}", triggerCondition);
            try
            {
                using var connection = await _dbConnectionFactory.CreateConnectionAsync();
                var sql = @"SELECT * FROM Badges WHERE TriggerCondition = @TriggerCondition";
                var badge = await connection.QuerySingleOrDefaultAsync<Badge>(sql, new
                {
                    TriggerCondition = triggerCondition.ToString()
                });

                if (badge == null)
                    _logger.LogInformation("No badge found for TriggerCondition={TriggerCondition}", triggerCondition);
                else
                    _logger.LogInformation("Found badge Id={BadgeId} for TriggerCondition={TriggerCondition}", badge.Id, triggerCondition);

                return badge;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get badge for TriggerCondition={TriggerCondition}", triggerCondition);
                throw;
            }
        }

        /// <summary>
        /// Awards a badge to a user and returns the UserBadge id.
        /// </summary>
        public async Task<int?> AwardBadgeAsync(int userId, int badgeId)
        {
            _logger.LogDebug("AwardBadgeAsync called: UserId={UserId}, BadgeId={BadgeId}", userId, badgeId);
            try
            {
                using var connection = await _dbConnectionFactory.CreateConnectionAsync();
                var sql = @"INSERT INTO UserBadges (UserId, BadgeId) VALUES (@UserId, @BadgeId) RETURNING Id";
                var id = await connection.ExecuteScalarAsync<int>(sql, new
                {
                    UserId = userId,
                    BadgeId = badgeId
                });

                _logger.LogInformation("AwardBadgeAsync awarded BadgeId={BadgeId} to UserId={UserId} (UserBadgeId={UserBadgeId})", badgeId, userId, id);
                return id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to award badge BadgeId={BadgeId} to UserId={UserId}", badgeId, userId);
                throw;
            }
        }

        /// <summary>
        /// Returns badges already awarded to the specified user.
        /// </summary>
        public async Task<IEnumerable<Badge>> GetBadgesByUserIdAsync(int userId)
        {
            _logger.LogDebug("GetBadgesByUserIdAsync called: UserId={UserId}", userId);
            try
            {
                using var connection = await _dbConnectionFactory.CreateConnectionAsync();
                var sql = @"SELECT b.* FROM UserBadges ub INNER JOIN Badges b ON ub.BadgeId = b.Id WHERE ub.UserId = @UserId ORDER BY b.NAME";
                var badges = (await connection.QueryAsync<Badge>(sql, param: new { UserId = userId })).ToList();
                _logger.LogInformation("GetBadgesByUserIdAsync returned {Count} badges for UserId={UserId}", badges.Count, userId);
                return badges;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get badges for UserId={UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Checks whether a user already has the badge for the given trigger condition and awards it if not.
        /// This method will do nothing if no matching badge is configured.
        /// </summary>
        public async Task CheckAndAwardBadge(int userId, BadgeTriggerCondition triggerCondition)
        {
            _logger.LogDebug("CheckAndAwardBadge called: UserId={UserId}, TriggerCondition={TriggerCondition}", userId, triggerCondition);
            try
            {
                var currentBadges = (await GetBadgesByUserIdAsync(userId)).ToList();
                var hasBadge = currentBadges.Any(b => b.TriggerCondition == triggerCondition);

                if (hasBadge)
                {
                    _logger.LogInformation("UserId={UserId} already has badge for TriggerCondition={TriggerCondition}", userId, triggerCondition);
                    return;
                }

                var badge = await GetBadgeByTriggerConditionAsync(triggerCondition);
                if (badge == null)
                {
                    _logger.LogWarning("No configured badge found for TriggerCondition={TriggerCondition}; nothing to award for UserId={UserId}", triggerCondition, userId);
                    return;
                }

                var awardedId = await AwardBadgeAsync(userId, badge.Id);
                _logger.LogInformation("CheckAndAwardBadge awarded BadgeId={BadgeId} to UserId={UserId} (UserBadgeId={UserBadgeId})", badge.Id, userId, awardedId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check/award badge for UserId={UserId} TriggerCondition={TriggerCondition}", userId, triggerCondition);
                // Do not rethrow to avoid blocking calling flows (badge awarding is a best-effort side-effect).
            }
        }
    }
}
