using CodeFlow.core.Repositories;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CodeFlow.core.Models.Services
{
    public class HotQuestionCacheService : IHotQuestionCacheService
    {
        private readonly IRedisCacheService _redisCacheService;
        private readonly IQuestionRepository _questionRepository;
        private readonly IRedisQuestionPopularityService _questionPopularityService;
        private readonly ILogger<HotQuestionCacheService> _logger;

        private const string HotQuestionCatchKey = "hot:questions:list";
        private const string HotQuestionPrefix = "hot:question:";

        private static readonly TimeSpan VeryHotTTL = TimeSpan.FromMinutes(60);
        private static readonly TimeSpan HotTTL = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan WarmTTL = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan CacheWarmingTTL = TimeSpan.FromMinutes(5);

        public HotQuestionCacheService(
            IRedisCacheService redisCacheService,
            IQuestionRepository questionRepository,
            IRedisQuestionPopularityService questionPopularityService,
            ILogger<HotQuestionCacheService> logger)
        {
            _redisCacheService = redisCacheService;
            _questionRepository = questionRepository;
            _questionPopularityService = questionPopularityService;
            _logger = logger;
        }

        public async Task<IEnumerable<Question>> GetHotQuestionsAsync(int count = 10)
        {
            var cached = await _redisCacheService.GetAsync<List<Question>>(HotQuestionCatchKey);
            if (cached != null)
            {
                _logger.LogDebug("Cache hit for hot questions list");
                return cached;
            }

            _logger.LogDebug("Cache miss for hot questions list, building from individual caches");

            var hotIds = await _questionPopularityService.GetHotQuestionIdsAsync(count * 2);

            var hotQuestions = new List<Question>();

            foreach (int id in hotIds)
            {
                var question = await GetHotQuestionAsync(id);
                if (question != null)
                {
                    hotQuestions.Add(question);
                }
            }

            if (hotQuestions.Any())
            {
                await _redisCacheService.SetAsync(HotQuestionCatchKey, hotQuestions, TimeSpan.FromMinutes(5));
            }

            return hotQuestions.Take(count);
        }

        private async Task<Question?> GetHotQuestionAsync(int questionId)
        {
            var cacheKey = $"{HotQuestionPrefix}{questionId}";
            var cached = await _redisCacheService.GetAsync<Question>(cacheKey);

            if (cached != null)
            {
                _logger.LogDebug("Cache hit for hot question {QuestionId}", questionId);
                return cached;
            }

            var question = await _questionRepository.GetByIdWithTagsAsync(questionId);
            if (question != null)
            {
                var hotnessScore = await _questionPopularityService.GetHotnessScoreAsync(questionId);
                var ttl = GetTTLForHotness(hotnessScore);

                await _redisCacheService.SetAsync(cacheKey, question, ttl);
                _logger.LogDebug("Cached hot question {QuestionId} with TTL {TTL} minutes",
                questionId, ttl.TotalMinutes);
            }
            return question;
        }

        private TimeSpan GetTTLForHotness(long score)
        {
            return score switch
            {
                > 1000 => VeryHotTTL,
                > 100 => HotTTL,
                > 10 => WarmTTL,
                _ => CacheWarmingTTL
            };
        }

        public async Task WarmTask()
        {
            _logger.LogInformation("Starting hot questions cache warming");

            var hotIds = await _questionPopularityService.GetHotQuestionIdsAsync(50);

            var hotQuestionTasks = hotIds.Select(async id =>
            {
                try
                {
                    var cacheKey = $"{HotQuestionPrefix}{id}";

                    var question = await _questionRepository.GetByIdWithTagsAsync(id);
                    if (question != null)
                    {
                        var hotnessScore = await _questionPopularityService.GetHotnessScoreAsync(id);
                        var ttl = GetTTLForHotness(hotnessScore);

                        await _redisCacheService.SetAsync(cacheKey, question, ttl);
                        _logger.LogDebug("Cached hot question {QuestionId} with TTL {TTL} minutes", id, ttl.TotalMinutes);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to warm cache for question {QuestionId}", id);
                }
            });

            await Task.WhenAll(hotQuestionTasks);
            _logger.LogInformation("Cache warming completed for {Count} questions", hotIds.Count());
        }
    }
}
