using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CodeFlow.core.Repositories.AuthServices
{
    /// <summary>
    /// Authorization helper used across the application.
    /// Provides simple ownership checks to determine whether a given user can edit a question or answer.
    /// Methods return null when the target entity cannot be found, true when the user is the owner, and false otherwise.
    /// </summary>
    public class AuthServices : IAuthServices
    {
        private readonly IQuestionRepository _questionRepository;
        private readonly IAnswerRepository _answerRepository;
        private readonly ILogger<AuthServices> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="AuthServices"/>.
        /// </summary>
        public AuthServices(IQuestionRepository questionRepository, IAnswerRepository answerRepository, ILogger<AuthServices> logger)
        {
            _questionRepository = questionRepository ?? throw new ArgumentNullException(nameof(questionRepository));
            _answerRepository = answerRepository ?? throw new ArgumentNullException(nameof(answerRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Returns null if the question does not exist, true if the user can edit (is the owner), otherwise false.
        /// </summary>
        public async Task<bool?> CanEditQuestionAsync(int questionId, int userId)
        {
            _logger.LogDebug("Enter {Method} QuestionId={QuestionId} UserId={UserId}", nameof(CanEditQuestionAsync), questionId, userId);
            try
            {
                var question = await _questionRepository.GetByIdAsync(questionId);
                if (question == null)
                {
                    _logger.LogInformation("{Method}: QuestionId={QuestionId} not found", nameof(CanEditQuestionAsync), questionId);
                    return null;
                }

                var canEdit = question.UserId == userId;
                _logger.LogInformation("{Method}: QuestionId={QuestionId} OwnerUserId={Owner} CanEdit={CanEdit}", nameof(CanEditQuestionAsync), questionId, question.UserId, canEdit);
                return canEdit;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Method} for QuestionId={QuestionId} UserId={UserId}", nameof(CanEditQuestionAsync), questionId, userId);
                return null;
            }
        }

        /// <summary>
        /// Returns null if the answer does not exist, true if the user can edit (is the owner), otherwise false.
        /// </summary>
        public async Task<bool?> CanEditAnswerAsync(int answerId, int userId)
        {
            _logger.LogDebug("Enter {Method} AnswerId={AnswerId} UserId={UserId}", nameof(CanEditAnswerAsync), answerId, userId);
            try
            {
                var answer = await _answerRepository.GetByIdAsync(answerId);
                if (answer == null)
                {
                    _logger.LogInformation("{Method}: AnswerId={AnswerId} not found", nameof(CanEditAnswerAsync), answerId);
                    return null;
                }

                var canEdit = answer.UserId == userId;
                _logger.LogInformation("{Method}: AnswerId={AnswerId} OwnerUserId={Owner} CanEdit={CanEdit}", nameof(CanEditAnswerAsync), answerId, answer.UserId, canEdit);
                return canEdit;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Method} for AnswerId={AnswerId} UserId={UserId}", nameof(CanEditAnswerAsync), answerId, userId);
                return null;
            }
        }
    }
}
