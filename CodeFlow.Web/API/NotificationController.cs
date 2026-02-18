using CodeFlow.core.Models;
using CodeFlow.core.Repositories;
using CodeFlow.Web.API.DTO;
using CodeFlow.Web.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CodeFlow.Web.API
{
    [ApiController]
    [Authorize]
    [Route("notification")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        public NotificationController(INotificationRepository notificationRepository, UserManager<ApplicationUser> userManager)
        {
            _notificationRepository = notificationRepository;
            _userManager = userManager;
        }

        [LogAction]
        [HttpGet("all")]
        public async Task<IEnumerable<NotificationDTO>> GetAllUnreadNotifications()
        {
            var result = await _notificationRepository.GetAllUnreadNotification(GetCurrentUserId());

            if (result == null) {
                return [];
            }

            return result.Select(r => new NotificationDTO(r));
        }

        [LogAction]
        [HttpGet("count")]
        public async Task<int> GetAllUnreadNotificationsCount()
        {
            var result = await _notificationRepository.GetAllUnreadNotification(GetCurrentUserId());

            if (result == null)
            {
                return 0;
            }

            return result.Count();
        }

        [LogAction]
        [HttpPost("mark_as_read/{id}")]
        public async Task<bool> MarkAsRead(int id)
        {
            var result = await _notificationRepository.UpdateNotification(id, true);
            return result;
        }

        private int GetCurrentUserId()
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
