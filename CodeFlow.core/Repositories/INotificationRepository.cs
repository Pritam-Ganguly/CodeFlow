using CodeFlow.core.Models;

namespace CodeFlow.core.Repositories
{
    public interface INotificationRepository
    {
        Task<int?> CreateNotification(Notification notification);
        Task<bool> UpdateNotification(int notificationId, bool isRead);
        Task<IEnumerable<Notification>?> GetAllNotificationForUserId(int userId, int limit = 50);
        Task<IEnumerable<Notification>?> GetNotification(int id);
        Task<IEnumerable<Notification>?> GetAllUnreadNotification(int userId, int limit = 10);

    }
}