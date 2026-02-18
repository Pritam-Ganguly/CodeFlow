using CodeFlow.core.Models;
using CodeFlow.core.Models.Services;
using Microsoft.Extensions.Logging;

namespace CodeFlow.core.Repositories.Decorators
{
    public class CachedQuestionRepository : IQuestionRepository
    {
        private const string HomePageCacheKey = "homepage_questions";
        private const string QuestionDetailsPrefix = "question_";
        private static readonly TimeSpan QuestionCacheDuration = TimeSpan.FromMinutes(30);


        private readonly IQuestionRepository _questionRepository;
        private readonly IRedisCacheService _redisCacheService;
        private readonly IRedisCacheInvalidationService _redisCacheInvalidationService;
        private readonly ILogger<CachedQuestionRepository> _logger;


        public CachedQuestionRepository(
            IQuestionRepository questionRepository,
            IRedisCacheService redisCacheService,
            IRedisCacheInvalidationService redisCacheInvalidationService,
            ILogger<CachedQuestionRepository> logger)
        {
            _questionRepository = questionRepository;
            _redisCacheService = redisCacheService;
            _redisCacheInvalidationService = redisCacheInvalidationService;
            _logger = logger;
        }

        public async Task<int> CreateAsync(Question question)
        {
            await _redisCacheInvalidationService.InvalidateHomePageQuestionsAsync();
            await _redisCacheInvalidationService.InvalidateHotQuestionAsync();

            return await _questionRepository.CreateAsync(question);
        }

        public async Task<int?> CurrentVoteAsync(int userId, int quesitonId)
        {
            return await _questionRepository.CurrentVoteAsync(userId, quesitonId);
        }

        public async Task<int?> CurrentVoteForAnswerItemAsync(int userId, int answerId)
        {
            return await _questionRepository.CurrentVoteForAnswerItemAsync(userId, answerId);
        }

        public async Task<bool> DeleteQuestionAsync(int questionId, int userId)
        {
            await _redisCacheInvalidationService.InvalidateQuestionAsync(questionId);
            await _redisCacheInvalidationService.InvalidateHomePageQuestionsAsync();
            await _redisCacheInvalidationService.InvalidateHotQuestionAsync();
            return await _questionRepository.DeleteQuestionAsync(questionId, userId);
        }

        public async Task<bool> FirstPost(int userId)
        {
            return await _questionRepository.FirstPost(userId);
        }

        public async Task<int> GetAllQuestions()
        {
            return await _questionRepository.GetAllQuestions();
        }

        public async Task<IEnumerable<Question>> GetAllQuestionsByUserId(int userId, int pageNumber = 1, int pageSize = 10)
        {
            return await _questionRepository.GetAllQuestionsByUserId(userId, pageNumber, pageSize);
        }

        public async Task<int> GetAllResult(string searchQuery)
        {
            return await _questionRepository.GetAllResult(searchQuery);
        }

        public async Task<Question?> GetByIdAsync(int id)
        {
            var cacheKey = $"{QuestionDetailsPrefix}{id}";

            var cachedQuestion = await _redisCacheService.GetAsync<Question>(cacheKey);
            if (cachedQuestion != null)
            {
                _logger.LogInformation("Returning cached response");
                return cachedQuestion;
            }

            var question = await _questionRepository.GetByIdAsync(id);

            if (question != null)
            {
                await _redisCacheService.SetAsync(cacheKey, question, QuestionCacheDuration);
            }

            return question;
        }

        public async Task<Question?> GetByIdWithTagsAsync(int id)
        {
            var cacheKey = $"{QuestionDetailsPrefix}{id}_with_tags";

            var cachedQuestion = await _redisCacheService.GetAsync<Question>(cacheKey);
            if (cachedQuestion != null)
            {
                _logger.LogInformation("Returning cached response");
                return cachedQuestion;
            }

            var question = await _questionRepository.GetByIdWithTagsAsync(id);

            if (question != null)
            {
                await _redisCacheService.SetAsync(cacheKey, question, QuestionCacheDuration);
            }

            return question;
        }

        public async Task<IEnumerable<Question>> GetRecentWithTagsAsync(int pageNumber = 1, int pageSize = 10, QuestionSortType sortType = QuestionSortType.Newest)
        {
            if(pageNumber == 1 && sortType == QuestionSortType.Newest)
            {
                var cachedQuestion = await _redisCacheService.GetAsync<IEnumerable<Question>>(HomePageCacheKey);
                if (cachedQuestion != null)
                {
                    _logger.LogInformation("Returning cached response");
                    return cachedQuestion.Take(pageSize);
                }

                var questions = await _questionRepository.GetRecentWithTagsAsync(pageNumber, 20, sortType);

                if (questions.Any())
                {
                    await _redisCacheService.SetAsync(HomePageCacheKey, questions, QuestionCacheDuration);
                }

                return questions;
            }
            return await _questionRepository.GetRecentWithTagsAsync(pageNumber, pageSize, sortType);
        }

        public async Task<IEnumerable<Question>> SearchAsync(string searchQuery, int pageNumber = 1, int pageSize = 10, QuestionSortType sortType = QuestionSortType.Newest)
        {
            return await _questionRepository.SearchAsync(searchQuery, pageNumber, pageSize, sortType);
        }

        public async Task<int> UpdateQuestionAsync(int questionId, string newTitle, string newBody)
        {
            await _redisCacheInvalidationService.InvalidateQuestionAsync(questionId);
            await _redisCacheInvalidationService.InvalidateHomePageQuestionsAsync();
            await _redisCacheInvalidationService.InvalidateHotQuestionAsync();
            return await _questionRepository.UpdateQuestionAsync(questionId, newTitle, newBody);
        }
    }
}
