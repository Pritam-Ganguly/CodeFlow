using CodeFlow.core.Repositories;
using StackExchange.Redis;

namespace CodeFlow.core.Models.Services
{
    public class RedisQuestionPopularityService : IRedisQuestionPopularityService
    {
        private readonly IDatabase _database;
        private readonly IQuestionRepository _questionRepository;

        private const string ViewCountKey = "question:views:";
        private const string HotQuestionsKey = "hot:questions:";

        public RedisQuestionPopularityService(IConnectionMultiplexer redisConnection, IQuestionRepository questionRepository)
        {
            _questionRepository = questionRepository;
            _database = redisConnection.GetDatabase();
        }

        public async Task RecordViewAsync(int questionId)
        {
            var cacheKey = $"{ViewCountKey}:{questionId}";

            await _database.StringIncrementAsync(cacheKey);
            await _database.KeyExpireAsync(cacheKey, TimeSpan.FromHours(24));

            await UpdateHotnessScore(questionId);
        }

        private async Task UpdateHotnessScore(int questionId)
        {
            var views = await GetDailyViewsAsync(questionId);

            var hotnessScore = views;

            var questionDetails = await _questionRepository.GetByIdAsync(questionId);
            if (questionDetails != null)
            {
                hotnessScore *= questionDetails.ViewCount;
            }

            await _database.SortedSetAddAsync(HotQuestionsKey, questionId, hotnessScore);
        }

        private async Task<long> GetDailyViewsAsync(int questionId)
        {
            var cacheKey = $"{ViewCountKey}:{questionId}";
            var value = await _database.StringGetAsync(cacheKey);
            return value.HasValue ? (long)value : 0;
        }

        public async Task<IEnumerable<int>> GetHotQuestionIdsAsync(int count = 20)
        {
            var entries = await _database.SortedSetRangeByRankWithScoresAsync(HotQuestionsKey, 0, count - 1, Order.Descending);

            return entries.Where(e => !e.Element.IsNullOrEmpty).Select(e => int.Parse(e.Element!));
        }

        public async Task<long> GetHotnessScoreAsync(int questionId)
        {
            var score = await _database.SortedSetScoreAsync(HotQuestionsKey, questionId);
            return score.HasValue ? (long)score : 0;
        }

        public async Task CleanUpOldScoreAsync(int keepTopN = 5)
        {
            await _database.SortedSetRemoveRangeByRankAsync(HotQuestionsKey, 0, -keepTopN - 1);
        }
    }
}
