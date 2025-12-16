using CodeFlow.core.Models.Mapping;
using CodeFlow.core.Repositories;
using CodeFlow.core.Repositories.Utils;
using CodeFlow.Web.Filters;
using CodeFlow.Web.Models;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace CodeFlow.Web.Controllers
{
    public class ProfileController : Controller
    {
        private readonly IReputationRepository _reputationRepository;
        private readonly IQuestionRepository _questionRepository;
        private readonly IBadgeRepository _badgeRepository;
        private readonly IUserActivityRepository _userActivityRepository;
        private readonly IAnswerRepository _answerRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            IReputationRepository reputationRepository, 
            IQuestionRepository questionRepository, 
            IBadgeRepository badgeRepository, 
            IUserActivityRepository userActivityRepository,
            IAnswerRepository answerRepository,
            IUserRepository userRepository,
            ILogger<ProfileController> logger)
        {
            _reputationRepository = reputationRepository;
            _questionRepository = questionRepository;
            _badgeRepository = badgeRepository;
            _userActivityRepository = userActivityRepository;
            _answerRepository = answerRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        [LogAction]
        public async Task<IActionResult> UserProfile(int id)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("UserProfile: user was null for id {UserId}", id);
                    return BadRequest();
                }

                var reputation = await _reputationRepository.CalculateReputationAsync(id);
                var reputationHistory = await _reputationRepository.GetReputationHistoryAsync(id);
                var badges = await _badgeRepository.GetBadgesByUserIdAsync(id);
                var questions = await _questionRepository.GetAllQuestionsByUserId(id);

                foreach (var rh in reputationHistory)
                {
                    var desc = DescriptionHelper.GetDescription(rh.TransactionType, rh.RelatedPostId, rh.RelatedPostType);
                    if (rh.RelatedPostType == RelatedPostType.Question)
                    {
                        var question = await _questionRepository.GetByIdAsync(rh.RelatedPostId);
                        if (question != null)
                        {
                            desc = string.Format(desc, question.Title);
                        }
                    }
                    rh.Description = desc;
                }

                var UserProfileViewModel = new UserProfileViewModel()
                {
                    Id = id,
                    DisplayName = user.DisplayName,
                    Email = user.Email,
                    CreatedAt = user.CreatedAt,
                    Reputation = reputation,
                    ReputationHistory = reputationHistory,
                    Badges = badges,
                    Questions = questions,
                    UserActivities = await _userActivityRepository.GetUserActivitiesAsync(id),
                    AcceptedAnswerCount = await _answerRepository.GetAcceptedAnswerCountByUserIdAsync(id),
                    UserProfile = await _userRepository.GetUserProfile(id)
                };

                return View(UserProfileViewModel);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "UserProfile: requested user not found for id {UserId}", id);
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserProfile: unexpected error while loading profile for id {UserId}", id);
                return StatusCode(500);
            }
        }

        public async Task<IActionResult> ProfileImage(int id)
        {
            try
            {
                var userProfile = await _userRepository.GetUserProfile(id);
                if (userProfile == null)
                {
                    _logger.LogWarning("ProfileImage: no profile found for id {UserId}", id);
                    return BadRequest();
                }

                return File(userProfile.ProfilePicture, userProfile.ProfilePictureMimeType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProfileImage: failed to retrieve profile image for id {UserId}", id);
                return StatusCode(500);
            }
        }

        [LogAction]
        public async Task<IActionResult> UserQuestions(int id, int page = 1, int pageSize = 5)
        {
            _logger.LogInformation("Load more questions requested for {userId} for page {page} with pageSize {pageSize}", id, page, pageSize);
            try
            {
                var questions = await _questionRepository.GetAllQuestionsByUserId(id, page, pageSize);
                if (questions.Any())
                {
                    return PartialView("_UserProfileQuestions", questions);
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occured");
                return BadRequest();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [LogAction]
        public async Task<IActionResult> UpdateUserBio(int id, string bio)
        {
            try
            {
                var result = await _userRepository.UpdateUserProfileBio(id, bio);

                if (!result) 
                {
                    _logger.LogWarning("UpdateUserBio: update returned false for id {UserId}", id);
                    return BadRequest();
                }

                return RedirectToAction(nameof(UserProfile), new { id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateUserBio: failed to update bio for id {UserId}", id);
                ModelState.AddModelError(string.Empty, "An error occurred while updating the bio.");
                return RedirectToAction(nameof(UserProfile), new { id = id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [LogAction]
        public async Task<IActionResult> UpdateProfilePicture(int id, IFormFile profilePicture)
        {
            if (profilePicture == null || profilePicture.Length == 0)
            {
                _logger.LogWarning("UpdateProfilePicture: no file provided for id {UserId}", id);
                ModelState.AddModelError(string.Empty, "No file was provided.");
                return RedirectToAction(nameof(UserProfile), new { id = id });
            }

            try
            {
                using var memoryStream = new MemoryStream();
                await CompressImageAsync(profilePicture, memoryStream);

                byte[] image = memoryStream.ToArray();

                var fileExtension = Path.GetExtension(profilePicture.FileName).ToLowerInvariant();

                var result = await _userRepository.UpdateUserProfileImage(id, profilePicture.ContentType, $"profile_{id}_pic{fileExtension}", image);

                if (!result)
                {
                    _logger.LogWarning("UpdateProfilePicture: repository update returned false for id {UserId}", id);
                    ModelState.AddModelError(string.Empty, "An error occured while uploading the image");
                    return RedirectToAction(nameof(UserProfile), new { id = id });
                }

                _logger.LogInformation("UpdateProfilePicture: uploaded profile image for id {UserId}, file {FileName}", id, profilePicture.FileName);
                return RedirectToAction(nameof(UserProfile), new { id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateProfilePicture: failed to update profile picture for id {UserId}", id);
                ModelState.AddModelError(string.Empty, "An error occurred while uploading the image.");
                return RedirectToAction(nameof(UserProfile), new { id = id });
            }
        }

        private async Task CompressImageAsync(IFormFile imageFile, MemoryStream outputStream)
        {
            try
            {
                using var image = await Image.LoadAsync(imageFile.OpenReadStream());

                var resizeOption = new ResizeOptions()
                {
                    Size = new Size(500,500),
                    Mode = ResizeMode.Max
                };

                image.Mutate(x => x.Resize(resizeOption));

                IImageEncoder encoder = imageFile.ContentType.ToLower() switch
                {
                    "image/png" => new PngEncoder() { CompressionLevel = PngCompressionLevel.BestCompression },
                    _ => new JpegEncoder() { Quality = 80 }
                };

                await image.SaveAsync(outputStream, encoder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CompressImageAsync: failed to compress image (filename: {FileName})", imageFile?.FileName);
                throw;
            }
        }
    }
}
