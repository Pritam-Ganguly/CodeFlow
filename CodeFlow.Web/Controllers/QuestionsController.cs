using CodeFlow.core.Models;
using CodeFlow.core.Repositories;
using CodeFlow.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CodeFlow.Web.Controllers
{
    public class QuestionsController : Controller
    {
        private readonly IQuestionRepository _questionRepository;
        private readonly IAnswerRepository _answerRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITagRepository _tagRepository;

        public QuestionsController(IQuestionRepository questionRepository, UserManager<ApplicationUser> userManager, IAnswerRepository answerRepository, ITagRepository tagRepository)
        {
            _questionRepository = questionRepository;
            _userManager = userManager;
            _answerRepository = answerRepository;
            _tagRepository = tagRepository;
        }

        public async Task<IActionResult> Details(int id)
        {
            var question = await _questionRepository.GetByIdWithTagsAsync(id);
            if(question == null)
            {
                return NotFound();
            }

            var answers = await _answerRepository.GetByQuestionIdAsync(id);
            var updatedAnswers = await Task.WhenAll(answers.Select(async (answer) =>
            {
                return new AnswerViewModel(answer)
                {
                    IsAuthor = IsAuthor(answer.UserId),
                    CurrentVote = await CurrentVoteForAnswers(answer.Id)
                };
            }));

            var viewModel = new DetailsViewModel()
            {
                Question = new QuestionViewModel(question)
                {
                    IsAuthor = IsAuthor(question.UserId),
                    CurrentVote = await CurrentUserVote(question.Id)
                },
                Answers = updatedAnswers
            };
            return View(viewModel);
        }

        [Authorize]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(Question question, string tagsInput)
        {
            if(ModelState.IsValid) 
            {
                var user = await _userManager.GetUserAsync(User);
                if(user == null)
                {
                    return RedirectToAction(nameof(Details), new { id = question.Id });
                }
                question.UserId = user.Id;
                int newQuestionId = await _questionRepository.CreateAsync(question);

                if (!string.IsNullOrWhiteSpace(tagsInput))
                {
                    var tagNames = tagsInput.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim().ToLower()).Distinct();

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
                }

                return RedirectToAction(nameof(Details), new { id = newQuestionId });
            }
            return View(question);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Answer(int id, string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                ModelState.AddModelError("", "Answer body cannot be empty");
                return await RedisplayDetailsPage(id);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction(nameof(Details), new { id = id });
            }
            Answer answer = new Answer()
            {
                Body = body.Trim(),
                QuestionId = id,
                UserId = user.Id
            };

            await _answerRepository.CreateAsync(answer);
            return RedirectToAction(nameof(Details), new { id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> EditAnswer(int id, string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                ModelState.AddModelError("", "Answer body cannot be empty");
                return await RedisplayDetailsPage(id);
            }

            var answer = await _answerRepository.GetByIdAsync(id); 
            if(answer == null)
            {
                return NotFound();
            }

            var result = await _answerRepository.EditAnswerAsync(id, body);
            if(result == null)
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Details), new { id = answer.QuestionId});
        }

        public async Task<ActionResult> Edit(int id)
        {
            var question = await _questionRepository.GetByIdAsync(id);
            return View(question);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(Question question)
        {
            if (ModelState.IsValid) {
                var result = await _questionRepository.UpdateQuestionAsync(question.Id, question.Title, question.Body);
                return await RedisplayDetailsPage(question.Id);
            }
            return View(question);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> AcceptAnswer(int answerId)
        {
            var result = await _answerRepository.AcceptAnswer(answerId);
            if(result == 0)
            {
                return NotFound();
            }
            var question = await _answerRepository.GetQuestionByAnserIdAync(answerId);
            if (question == null)
            {
                return NotFound();
            }
            return RedirectToAction(nameof(Details), new { id = question.Id });
        }

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
                    CurrentVote = await CurrentVoteForAnswers(answer.Id)
                };
            }));

            var viewModel = new DetailsViewModel()
            {
                Question = new QuestionViewModel(question)
                {
                    IsAuthor = IsAuthor(question.UserId),
                    CurrentVote = await CurrentUserVote(question.Id)
                },
                Answers = updatedAnswers
            };
            return View("Details", viewModel);
        }

        private bool IsAuthor(int authorId) => authorId == GetCurrentUser();
            
        private async Task<int> CurrentUserVote(int questionId)
        {
            int userId = GetCurrentUser();
            int? voteType = await _questionRepository.CurrentVoteAsync(userId, questionId);
            return voteType ?? 0;
        }

        private async Task<int> CurrentVoteForAnswers(int answerId)
        {
            int userId = GetCurrentUser();
            int? voteType = await _questionRepository.CurrentVoteForAnswerItemAsync(userId, answerId);
            return voteType ?? 0;
        }

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
