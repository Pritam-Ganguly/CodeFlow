using CodeFlow.core.Models;
using CodeFlow.core.Repositories;
using CodeFlow.core.Repositories.AuthServices;
using CodeFlow.Web.Filters;
using CodeFlow.Web.Models;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CodeFlow.Web.Controllers
{
    [Authorize]
    public class QuestionsController : Controller
    {
        private readonly IQuestionRepository _questionRepository;
        private readonly IAnswerRepository _answerRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITagRepository _tagRepository;
        private readonly ICommentRepository _commentRepository;
        private readonly IAuthServices _authServices;
        private readonly ILogger<QuestionsController> _logger;

        public QuestionsController(
            IQuestionRepository questionRepository,
            UserManager<ApplicationUser> userManager,
            IAnswerRepository answerRepository,
            ITagRepository tagRepository, ICommentRepository commentRepository,
            IAuthServices authServices,
            ILogger<QuestionsController> logger)
        {
            _questionRepository = questionRepository;
            _userManager = userManager;
            _answerRepository = answerRepository;
            _tagRepository = tagRepository;
            _commentRepository = commentRepository;
            _logger = logger;
            _authServices = authServices;
        }

        [AllowAnonymous]
        [LogAction]
        public async Task<IActionResult> Details(int id)
        {
            _logger.LogInformation("Retrieving details for question {QuestionId}", id);

            try
            {
                var question = await _questionRepository.GetByIdWithTagsAsync(id);
                if (question == null)
                {
                    return NotFound();
                }

                var answers = await _answerRepository.GetByQuestionIdAsync(id);

                var updatedAnswers = await Task.WhenAll(answers.Select(async (answer) =>
                {
                    return new AnswerViewModel(answer)
                    {
                        IsAuthor = IsAuthor(answer.UserId),
                        CurrentVote = await CurrentVoteForAnswers(answer.Id),
                        Comments = await _commentRepository.GetAnswerCommentsAsync(answer.Id),
                    };
                }));

                var updatedQuestion = new QuestionViewModel(question)
                {
                    IsAuthor = IsAuthor(question.UserId),
                    CurrentVote = await CurrentUserVote(question.Id),
                    Comments = await _commentRepository.GetQuestionCommentsAsync(question.Id),
                    ViewCount = question.ViewCount + 1
                };

                var viewModel = new DetailsViewModel()
                {
                    Question = updatedQuestion,
                    Answers = updatedAnswers
                };

                _logger.LogInformation("Succesfully fetched details for question id {Id}", id);

                return View(viewModel);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occured while fetching details for question with id: {Id}", id);
                return RedirectToAction("Index", "Home");
            }
        }

        [LogAction]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [LogAction]
        [TrackUserActivity(ActivityType.question_asked, TargetEntityType.system)]
        public async Task<IActionResult> Create(CreateRequestModel request)
        {
            _logger.LogInformation("Started executing create method for question {Title}", request.Title);

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Challenge();
                }

                var validator = new CreateRequestValidator();

                ValidationResult result = await validator.ValidateAsync(request);

                if (!result.IsValid)
                {
                    foreach(var error in result.Errors)
                    {
                        ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                    }
                    return View(request);
                }

                var question = new Question() 
                { 
                    UserId = user.Id,
                    Title = request.Title,
                    Body = request.Body,
                    UpdatedAt = DateTime.Now,
                };

                int newQuestionId = await _questionRepository.CreateAsync(question);
                var tagNames = request.TagsInput.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim().ToLower()).Distinct();
                var tagIds = await Task.WhenAll(tagNames.Select(async tagName =>
                {
                    var existingTag = await _tagRepository.GetByNameAsync(tagName);
                    if (existingTag != null)
                    {
                        return existingTag.Id;
                    }
                    else
                    {
                        var newTag = new Tag { Name = tagName };
                        newTag = await _tagRepository.CreateAsync(newTag);
                        return newTag.Id;
                    }
                }));

                if (tagIds.Length != 0)
                {
                    await _tagRepository.AddTagsToQuestionAsync(newQuestionId, tagIds);
                }

                _logger.LogInformation("Succesfully created a question with id {QuestionId}", newQuestionId);
                return RedirectToAction(nameof(Details), new { id = newQuestionId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured while creating a question with title {Title}", request.Title);
                ModelState.AddModelError(string.Empty, "An unexpected error occured.");
                return View(request);
            }
                
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [LogAction]
        [TrackUserActivity(ActivityType.answer_posted, TargetEntityType.question)]
        public async Task<IActionResult> Answer(int questionId, string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                ModelState.AddModelError("answerbody", "Answer body cannot be empty");
                return await RedisplayDetailsPage(questionId);
            }
            else if(body.Length < 3)
            {
                ModelState.AddModelError("answerbody", "Answer body is too short");
                return await RedisplayDetailsPage(questionId);
            }
            else if (body.Length > 2000)
            {
                ModelState.AddModelError("answerbody", "Maximum answer body size reached");
                return await RedisplayDetailsPage(questionId);
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Challenge();
                }

                Answer answer = new Answer()
                {
                    Body = body.Trim(),
                    QuestionId = questionId,
                    UserId = user.Id
                };

                await _answerRepository.CreateAsync(answer);
                _logger.LogInformation("Successfully added answer for question with id {QuestionId}", questionId);
                return RedirectToAction(nameof(Details), new { id = questionId });
            }
            catch (Exception ex) 
            {

                _logger.LogError(ex, "An error occured while adding answer for question id {QuestionId}", questionId);
                ModelState.AddModelError("answerbody", "An unexpected error occured");
                return RedirectToAction(nameof(Details), new { id = questionId });
            }
        }

        [LogAction]
        public async Task<ActionResult> Edit(int id)
        {
            _logger.LogInformation("Opening edit view for question.");
            try
            {
                var access = await _authServices.CanEditQuestionAsync(id, GetCurrentUser());
                if (access == null)
                {
                    return NotFound();
                }
                if (access == false)
                {
                    return Forbid();
                }
                var question = await _questionRepository.GetByIdAsync(id);

                if(question == null)
                {
                    return BadRequest();
                }

                _logger.LogInformation("Successfully opened the edit view for question id: {QuestionId}", id);
                var editRequest = new EditRequestModel()
                {
                    Id = question.Id,
                    Title = question.Title,
                    Body = question.Body,
                };
                return View(editRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured while opening edit view");
                return StatusCode(500, "An error occured while opening edit view");
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [LogAction]
        public async Task<IActionResult> Edit(EditRequestModel request)
        {
            _logger.LogInformation("Executing edit operation for question id {QuestionId}", request.Id);

            try
            {
                var access = await _authServices.CanEditQuestionAsync(request.Id, GetCurrentUser());
                if (access == null)
                {
                    return NotFound();
                }
                if (access == false)
                {
                    return Forbid();
                }
                var question = await _questionRepository.GetByIdAsync(request.Id);

                if (question == null)
                {
                    return BadRequest();
                }

                var validator = new EditRequestModelValidator();
                var validationResult= validator.Validate(request);
                
                if (!validationResult.IsValid)
                {
                    foreach(var error in validationResult.Errors)
                    {
                        ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                    }

                    return View(request);
                }

                var rowsAffected = await _questionRepository.UpdateQuestionAsync(request.Id, request.Title, request.Body);
                if(rowsAffected == 0)
                {
                    ModelState.AddModelError(string.Empty, "Invalid request");
                    return View(request);
                }

                _logger.LogInformation("Successfully edited question with question id {QuestionId}", request.Id);
                return await RedisplayDetailsPage(request.Id);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occured");
                ModelState.AddModelError(string.Empty, "An unexpted error occured");
                return View(request);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [LogAction]
        public async Task<IActionResult> EditAnswer(int id, string? body)
        {
            _logger.LogInformation("Started executing edit request for answer id {AnswerId}", id);

            try
            {
                var question = await _answerRepository.GetQuestionByAnswerIdAsync(id);

                if(question == null)
                {
                    return BadRequest();
                }

                if (string.IsNullOrWhiteSpace(body))
                {
                    ModelState.AddModelError("answer-" + id, "Answer body cannot be empty");
                    return await RedisplayDetailsPage(question.Id);
                }
                if (body.Length < 2)
                {
                    ModelState.AddModelError("answer-" + id, "Answer body content is too short");
                    return await RedisplayDetailsPage(question.Id);
                }
                if (body.Length > 5000)
                {
                    ModelState.AddModelError("answer-" + id, "Maximum size limit reached for answer");
                    return await RedisplayDetailsPage(question.Id);
                }

                var answer = await _answerRepository.GetByIdAsync(id);
                if (answer == null)
                {
                    return NotFound();
                }

                var result = await _answerRepository.EditAnswerAsync(answer.Id, body);
                if (result == null)
                {
                    return NotFound();
                }

                _logger.LogInformation("Successfully edited answer with answer id {AnswerId}", answer.Id);

                return RedirectToAction(nameof(Details), new { id = answer.QuestionId });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occured while exeuting edit request for answer id {AnswerId}", id);
                ModelState.AddModelError(string.Empty, "An unexpected error occred.");
                return await RedisplayDetailsPage(id);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [LogAction]
        public async Task<IActionResult> Delete(int questionId)
        {
            _logger.LogInformation("Started executing delete operation for {QuestionId}", questionId);
            try
            {
                await _questionRepository.DeleteQuestionAsync(questionId, GetCurrentUser());
                _logger.LogInformation("Successfully executed delete operation for question id {QuestionId}", questionId);
                return RedirectToAction("Index", "Home");
            }
            catch(Exception e)
            {
                _logger.LogError(e, "An error occured while executing delete operation for question id {QuestionId}", questionId);
                ModelState.AddModelError(string.Empty, "An error occured while performing delete operation");
                return await RedisplayDetailsPage(questionId);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [LogAction]
        public async Task<IActionResult> DeleteAnswer(int answerId)
        {
            _logger.LogInformation("Started executing delete operation for answer id {AnswerId}", answerId);

            try
            {
                var question = await _answerRepository.GetQuestionByAnswerIdAsync(answerId);
                if (question == null)
                {
                    return BadRequest();
                }
                await _answerRepository.DeleteAnswerAsync(answerId);

                _logger.LogInformation("Successfully executed delete operation for answer id {AnswerId}", answerId);
                return await RedisplayDetailsPage(question.Id);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "An error occured while executing delete operation for answer id {AnswerId}", answerId);
                ModelState.AddModelError(string.Empty, "An error occured while performing delete operation");
                return Forbid();
            }
            
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [LogAction]
        [TrackUserActivity(ActivityType.answer_accepted, TargetEntityType.answer)]
        public async Task<IActionResult> AcceptAnswer(int answerId)
        {
            _logger.LogInformation("Started executing accept answer request for answer id {AnswerId}", answerId);

            try
            {
                var result = await _answerRepository.AcceptAnswer(answerId);
                if (result == 0)
                {
                    return NotFound();
                }
                var question = await _answerRepository.GetQuestionByAnswerIdAsync(answerId);
                if (question == null)
                {
                    return NotFound();
                }
                _logger.LogInformation("Successfully executed accept answer operation for answer id {AnswerId}", answerId);
                return RedirectToAction(nameof(Details), new { id = question.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured while executing accept answer operation for answer id {AnswerId}", answerId);
                return BadRequest();
            }
           
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [LogAction]
        [TrackUserActivity(ActivityType.comment_added, TargetEntityType.question)]
        public async Task<IActionResult> CommentOnQuestion(int id, string? body)
        {
            _logger.LogInformation("Started executing comment question request for question id {QuestionId}", id);

            if (string.IsNullOrWhiteSpace(body))
            {
                ModelState.AddModelError("questioncomment", "Comment Body cannot be empty");
                return await RedisplayDetailsPage(id);
            }
            if(body.Length > 500)
            {
                ModelState.AddModelError("questioncomment", "Comment body is too long");
                return await RedisplayDetailsPage(id);
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);

                if (user == null)
                {
                    return RedirectToAction(nameof(Details), new { id = id });
                }

                var comment = new Comment
                {
                    Body = body,
                    QuestionId = id,
                    UserId = user.Id,
                };

                await _commentRepository.AddCommentAsync(comment);
                _logger.LogInformation("Successfully executed comment question request for question id {QuestionId}", id);
                return RedirectToAction(nameof(Details), new { id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured while executing comment question request for question id {QuestionId}", id);
                ModelState.AddModelError(string.Empty, "An error occured while executing comment request");
                return await RedisplayDetailsPage(id);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [LogAction]
        [TrackUserActivity(ActivityType.comment_added, TargetEntityType.answer)]
        public async Task<IActionResult> CommentOnAnswer(int id, string? body)
        {
            _logger.LogInformation("Started executing comment answer requestion for answer id {AnswerId}", id);
            try
            {
                var question = await _answerRepository.GetQuestionByAnswerIdAsync(id);
                if (question == null)
                {
                    return BadRequest();
                }

                if (string.IsNullOrWhiteSpace(body))
                {
                    ModelState.AddModelError("comment-answer-"+id, "Comment body cannot be empty");
                    return await RedisplayDetailsPage(question.Id);
                }

                if (body.Length > 500)
                {
                    ModelState.AddModelError("comment-answer-"+id, "Comment body is too long");
                    return await RedisplayDetailsPage(question.Id);
                }

                var comment = new Comment
                {
                    Body = body,
                    AnswerId = id,
                    UserId = GetCurrentUser(),
                };

                await _commentRepository.AddCommentAsync(comment);
                _logger.LogInformation("Successfully executed comment answer requestion for answer id {AnswerId}", id);
                return RedirectToAction(nameof(Details), new { id = question.Id });
            }
            catch(Exception ex)
            {
                _logger.LogInformation(ex, "An error occured while executing comment request on answer id {answerId}", id);
                return BadRequest();
            }
        }

        /// <summary>
        /// Asynchronously retrieves and prepares the details of a question and its associated answers for display.
        /// </summary>
        /// <remarks>This method is used mainly for redisplaying page with validation errors.</remarks>
        /// <param name="questionId">The unique identifier of the question to be displayed.</param>
        /// <returns>An <see cref="IActionResult"/> that renders the details view with the question and its answers. Returns <see
        private async Task<IActionResult> RedisplayDetailsPage(int questionId)
        {
            var question = await _questionRepository.GetByIdAsync(questionId);
            if(question == null)
            {
                return NotFound();
            }

            var answers = await _answerRepository.GetByQuestionIdAsync(questionId);
            var updatedAnswers = await Task.WhenAll(answers.Select(async (answer) =>
            {
                return new AnswerViewModel(answer)
                {
                    IsAuthor = IsAuthor(answer.UserId),
                    CurrentVote = await CurrentVoteForAnswers(answer.Id),
                    Comments = await _commentRepository.GetAnswerCommentsAsync(answer.Id),
                };
            }));

            var updatedQuestion = new QuestionViewModel(question)
            {
                IsAuthor = IsAuthor(question.UserId),
                CurrentVote = await CurrentUserVote(question.Id),
                Comments = await _commentRepository.GetQuestionCommentsAsync(question.Id),
            };

            var viewModel = new DetailsViewModel()
            {
                Question = updatedQuestion,
                Answers = updatedAnswers
            };
            return View("Details", viewModel);
        }

        /// <summary>
        /// Retrieves the current user's vote type for a specified question.
        /// </summary>
        /// <param name="questionId">The identifier of the question for which to retrieve the vote.</param>
        /// <returns>The vote type as an integer for the specified question by the current user.  Returns 0 if the user has not
        /// voted on the question.</returns>
        private async Task<int> CurrentUserVote(int questionId)
        {
            int userId = GetCurrentUser();
            int? voteType = await _questionRepository.CurrentVoteAsync(userId, questionId);
            return voteType ?? 0;
        }

        /// <summary>
        /// Retrieves the current vote type for a specific answer by the current user.
        /// </summary>
        /// <param name="answerId">The identifier of the answer for which to retrieve the vote type.</param>
        /// <returns>The vote type as an integer. Returns 0 if no vote has been cast by the current user.</returns>
        private async Task<int> CurrentVoteForAnswers(int answerId)
        {
            int userId = GetCurrentUser();
            int? voteType = await _questionRepository.CurrentVoteForAnswerItemAsync(userId, answerId);
            return voteType ?? 0;
        }

        /// <summary>
        /// Determines whether the specified author ID matches the current user's ID.
        /// </summary>
        /// <param name="authorId">The ID of the author to compare with the current user's ID.</param>
        /// <returns>returns if the user is the author.</returns>
        private bool IsAuthor(int authorId) => authorId == GetCurrentUser();

        /// <summary>
        /// Retrieves the current user's identifier.
        /// </summary>
        /// <returns>The identifier of the current user as an integer. Returns -1 if the user is not logged in.</returns>
        private int GetCurrentUser()
        {
            bool isUserLoggedIn = int.TryParse(_userManager.GetUserId(User), out int userId);
            if (!isUserLoggedIn)
            {
                userId = -1;
            }
            return userId;
        }
    }
}
