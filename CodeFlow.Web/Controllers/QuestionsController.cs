using CodeFlow.core.Models;
using CodeFlow.core.Repositories;
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

            var viewModel = (Question: question, Answers: answers);
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
                    var tagIds = new List<int>();

                    foreach (var tagName in tagNames)
                    {
                        var existingTag = await _tagRepository.GetByNameAsync(tagName);
                        if (existingTag != null)
                        {
                            tagIds.Add(existingTag.Id);
                        }
                        else
                        {
                            var newTag = new Tag { Name = tagName };
                            newTag = await _tagRepository.CreateAsync(newTag);
                            tagIds.Add(newTag.Id);
                        }
                    }

                    if (tagIds.Count != 0)
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


        private async Task<IActionResult> RedisplayDetailsPage(int questionId)
        {
            var question = await _questionRepository.GetByIdAsync(questionId);
            if(question == null)
            {
                return NotFound();
            }

            var answers = await _answerRepository.GetByQuestionIdAsync(questionId);
            var viewModel = (Question: question, Answers: answers);
            return View("Details", viewModel);
        }
    }


}
