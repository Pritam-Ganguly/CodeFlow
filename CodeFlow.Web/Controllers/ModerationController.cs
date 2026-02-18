using CodeFlow.core.Models;
using CodeFlow.core.Repositories;
using CodeFlow.Web.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CodeFlow.Web.Controllers
{
    [Authorize( Policy = "RequireAdminPrivilege")]
    public class ModerationController : Controller
    {
        private readonly IFlagRepository _flagRepository;
        private readonly ILogger<ModerationController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public ModerationController(IFlagRepository flagRepository, ILogger<ModerationController> logger, UserManager<ApplicationUser> userManager)
        {
            _flagRepository = flagRepository;  
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// This action fetches pending flags and displays them on the dashboard.
        /// </summary>
        /// <returns></returns>
        [LogAction]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                _logger.LogInformation("Started fetching flags from GetPendingFlagsAsync.");
                var pendingFlags = await _flagRepository.GetPendingFlagsAsync();
                _logger.LogInformation("Returned flags of count {count}", pendingFlags.Count());

                return View(pendingFlags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occured");
                return BadRequest();
            }
        }

        /// <summary>
        /// This action fetches details of a specific flag by its ID.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [LogAction]
        public async Task<IActionResult> FlagDetails(int id )
        {
            try
            {
                var flag = await _flagRepository.GetByIdAsync(id);
                if (flag == null)
                {
                    return NotFound();
                }

                return View(flag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occured");
                return BadRequest();
            }
        }


        /// <summary>
        /// This action resolves a flag by updating its status to Resolved.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [LogAction]
        public async Task<IActionResult> Resolve(int id)
        {
            try
            {
                var result = await _flagRepository.UpdateStatusAsync(id, FlagStatusType.Resolved, GetCurrentUser(), "Resolved by admin");
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occured");
                return BadRequest();
            }
        }

        /// <summary>
        /// This method retrieves the current logged-in user's ID.
        /// </summary>
        /// <returns></returns>
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
