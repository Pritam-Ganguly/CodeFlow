using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CodeFlow.core.Models.Services
{
    public class RedisCacheInvalidationService : IRedisCacheInvalidationService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _database;
        private readonly ILogger<RedisCacheInvalidationService> _logger;

        private const string HotQuestionCatchKey = "hot:questions:list";
        private const string HotQuestionPrefix = "hot:question:";
        private const string HomePageCacheKey = "homepage_questions";
        private const string QuestionDetailsPrefix = "question_";

        public RedisCacheInvalidationService(IConnectionMultiplexer redisConnection, ILogger<RedisCacheInvalidationService> logger)
        {
            _redis = redisConnection;
            _database = redisConnection.GetDatabase();
            _logger = logger;
        }

        public async Task InvalidateQuestionAsync(int questionId)
        {
            _logger.LogInformation("Invalidating cache for question {QuestionId}", questionId);
            var keys = new List<string>()
            {
                $"{QuestionDetailsPrefix}{questionId}",
                $"{QuestionDetailsPrefix}{questionId}_with_tags",
            };

            var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
            await _database.KeyDeleteAsync(redisKeys);
        }

        public async Task InvalidateHomePageQuestionsAsync()
        {
            _logger.LogDebug("Invalidating homepage cache");
            await _database.KeyDeleteAsync(HomePageCacheKey);
        }

        public async Task InvalidateHotQuestionAsync()
        {
            _logger.LogDebug("Invalidating hot questions cache");

            await _database.KeyDeleteAsync(HotQuestionCatchKey);
            await InvalidateByPatternAsync($"{HotQuestionPrefix}*");
        }


        public async Task InvalidateByPatternAsync(string pattern)
        {
            try
            {
                var endPoints = _redis.GetEndPoints();
                var server = _redis.GetServer(endPoints.First());

                var keys = server.Keys(pattern: pattern).ToArray();

                if (keys.Any())
                {
                    _logger.LogDebug("Invalidating {Count} keys matching pattern {Pattern}", keys.Length, pattern);
                    await _database.KeyDeleteAsync(keys);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating cache by pattern {Pattern}", pattern);
            }
        }

        public async Task InvalidateResetActionAsync()
        {
            _logger.LogDebug("Resetting cache");
            await InvalidateByPatternAsync("*");
        }

    }
}
