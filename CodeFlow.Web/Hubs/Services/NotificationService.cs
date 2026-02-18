using System;
using System.Threading.Tasks;
using CodeFlow.core.Models;
using CodeFlow.core.Repositories;
using CodeFlow.Web.API.DTO;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CodeFlow.Web.Hubs.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly INotificationRepository _notificationRepository;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IHubContext<NotificationHub> hubContext,
            INotificationRepository notificationRepository,
            ILogger<NotificationService> logger)
        {
            _notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }       

        public async Task<bool> Notify(string type, string message, int userId, int questionId)
        {
            var currentGroupName = $"User_Group_{userId}";

            try
            {
                if (userId <= 0)
                {
                    _logger.LogWarning("Notification not created: invalid userId {UserId}", userId);
                    return false;
                }

                if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(message))
                {
                    _logger.LogWarning("Notification not created: empty type or message for userId {UserId}, questionId {QuestionId}", userId, questionId);
                    return false;
                }

                var notification = new Notification()
                {
                    Type = type,
                    Message = message,
                    UserId = userId,
                    QuestionId = questionId,
                    CreatedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Creating notification for user {UserId}, question {QuestionId}", userId, questionId);
                var id = await _notificationRepository.CreateNotification(notification);

                if (id == null || id <= 0)
                {
                    _logger.LogError("Repository failed to create notification for user {UserId}, question {QuestionId}. Repository returned {RepositoryId}", userId, questionId, id);
                    return false;
                }

                var notificationDto = new NotificationDTO(notification)
                {
                    Id = id ?? -1
                };

                try
                {
                    _logger.LogInformation("Sending real-time notification to group {GroupName} (notificationId: {NotificationId})", currentGroupName, id);
                    await _hubContext.Clients.Group(currentGroupName).SendAsync("updateNotifications", notificationDto);
                }
                catch (Exception sendEx)
                {
                    // Persisted successfully, but real-time delivery failed — log and continue
                    _logger.LogWarning(sendEx, "Failed to send real-time notification to group {GroupName} for notificationId {NotificationId}", currentGroupName, id);
                }

                _logger.LogInformation("Notification {NotificationId} created for user {UserId}, question {QuestionId}", id, userId, questionId);
                return id > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating/sending notification for user {UserId}, question {QuestionId}", userId, questionId);
                return false;
            }
        }
    }
}
