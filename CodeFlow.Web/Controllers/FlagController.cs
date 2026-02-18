using CodeFlow.core.Models;
using CodeFlow.core.Repositories;
using CodeFlow.Web.Filters;
using CodeFlow.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CodeFlow.Web.Controllers
{
    [Authorize]
    public class FlagController : Controller
    {
        private readonly IFlagRepository _flagRepository;
        private readonly IQuestionRepository _questionRepository;
        private readonly IAnswerRepository _answerRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<FlagController> _logger;

        public FlagController(
            IFlagRepository flagRepository, 
            IQuestionRepository questionRepository, 
            IAnswerRepository answerRepository,
            UserManager<ApplicationUser> userManager,
            ILogger<FlagController> logger)
        {
            _flagRepository = flagRepository;
            _questionRepository = questionRepository;
            _answerRepository = answerRepository;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        [LogAction]
        public async Task<IActionResult> Report(string postType, int postId)
        {
            try 
            {
                _logger.LogDebug("Report action starting. postType={PostType}, postId={PostId}", postType, postId);

                var allowdPostTypes = Enum.GetNames(typeof(FlagPostType));
                if (!allowdPostTypes.Contains(postType))
                {
                    _logger.LogWarning("Report action called with invalid postType={PostType}", postType);
                    return BadRequest($"Invalid postType {postType}");
                }

                var viewModel = new ReportViewModel();
                viewModel.PostId = postId;

                if (postType == FlagPostType.Question.ToString())
                {
                    viewModel.PostType = FlagPostType.Question;
                    var question = await _questionRepository.GetByIdWithTagsAsync(postId);

                    if (question != null)
                    {
                        viewModel.PostTitle = question.Title;
                        viewModel.PostBody = question.BodyHtml;
                        viewModel.CreatedByUserId = question.User!.Id;
                        viewModel.CreatedByUserName = question.User!.DisplayName;
                    }
                }
                else if (postType == FlagPostType.Answer.ToString())
                {
                    viewModel.PostType = FlagPostType.Answer;
                    var answer = await _answerRepository.GetByIdAsync(postId);

                    if (answer != null)
                    {
                        var associatedQuestion = await _answerRepository.GetQuestionByAnswerIdAsync(postId);
                        viewModel.PostTitle = associatedQuestion!.Title ?? "Question not found...";
                        viewModel.PostBody = answer.BodyHtml;
                        viewModel.CreatedByUserId = answer.User!.Id;
                        viewModel.CreatedByUserName = answer.User!.DisplayName;
                    }
                }
                else
                {
                    _logger.LogWarning("Report action called with unsupported postType={PostType}", postType);
                    return BadRequest($"Post Type is not supported - {postType}");
                }

                var allowedFlags = await _flagRepository.GetAllFlagTypes();
                var flagSelectsList = allowedFlags.Select(t => new SelectListItem()
                {
                    Value = $"{t.Id}",
                    Text = $"{t.Name} - {t.Description}",
                });

                viewModel.FlagSelectList = flagSelectsList;

                _logger.LogDebug("Report action completed successfully for postType={PostType}, postId={PostId}", postType, postId);
                return View(viewModel);
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "An unexpected error occurred in Report action for postType={PostType}, postId={PostId}", postType, postId);
                return StatusCode(500, "An unexpected error has occured");
            }
        }
                
        [HttpPost]
        [LogAction]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Report(ReportViewModel report)
        {
            try
            {
                _logger.LogDebug("Report action starting. PostType={PostType}, PostId={PostId}", report?.PostType.ToString(), report?.PostId);

                if (report == null) {
                    return BadRequest();
                }

                var parsed = int.TryParse(_userManager.GetUserId(User), out int reportingUserId);
                if (!parsed)
                {
                    _logger.LogWarning("Report action unable to parse reporting user id for current user.");
                    return Unauthorized();
                }

                var validator = new ReportViewModelValidator();
                var validationResult = validator.Validate(report);

                if (!validationResult.IsValid) 
                { 
                    foreach(var error in validationResult.Errors)
                    {
                        ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                        _logger.LogDebug("Report action validation error: {Property} - {Error}", error.PropertyName, error.ErrorMessage);
                    }
                    
                    return await RedisplayReportPage(report);
                }

                var alreadyFlagged = await _flagRepository.GetIfUserHaveAlreadyReported(report.PostType, report.PostId, reportingUserId);

                if (alreadyFlagged) {
                    _logger.LogInformation("Report action user {UserId} already flagged postType={PostType} postId={PostId}", reportingUserId, report.PostType, report.PostId);
                    ModelState.AddModelError(string.Empty, "User has already reported this post");
                    return await RedisplayReportPage(report);
                }

                var flag = new Flag()
                {
                    FlagTypeId = report.SelectedFlagTypeId,
                    PostId = report.PostId,
                    PostType = report.PostType,
                    Reason = report.Reason,
                    Status = FlagStatusType.Pending,
                    UpdatedAt = DateTime.UtcNow,
                    ReportingUserId = reportingUserId
                };

                var result = await _flagRepository.CreateFlagAsync(flag);
                if(result == null || result == 0)
                {
                    _logger.LogWarning("Report action failed to create flag for user {UserId} postType={PostType} postId={PostId}", reportingUserId, report.PostType, report.PostId);
                    return BadRequest();
                }

                _logger.LogInformation("Report action created flag {FlagId} by user {UserId} for postType={PostType} postId={PostId}", result, reportingUserId, report.PostType, report.PostId);
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred in Report action for PostType={PostType}, PostId={PostId}", report?.PostType.ToString(), report?.PostId);
                return StatusCode(500, "An unexpected error has occured");
            }
        }

        private async Task<IActionResult> RedisplayReportPage(ReportViewModel viewModel)
        {
            try
            {
                if (viewModel == null) {
                    return BadRequest();
                }

                _logger.LogDebug("RedisplayReportPage starting for PostType={PostType}, PostId={PostId}", viewModel?.PostType.ToString(), viewModel?.PostId);

                var allowedFlags = await _flagRepository.GetAllFlagTypes();
                var flagSelectsList = allowedFlags.Select(t => new SelectListItem()
                {
                    Value = $"{t.Id}",
                    Text = $"{t.Name} - {t.Description}",
                });

                viewModel!.FlagSelectList = flagSelectsList;

                _logger.LogDebug("RedisplayReportPage completed for PostType={PostType}, PostId={PostId}", viewModel?.PostType.ToString(), viewModel?.PostId);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred in RedisplayReportPage for PostType={PostType}, PostId={PostId}", viewModel?.PostType.ToString(), viewModel?.PostId);
                return StatusCode(500, "An unexpected error has occured");
            }
        }
    }
}
