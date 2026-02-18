
namespace CodeFlow.Web.Hubs.Services
{
    public interface INotificationService
    {
        Task<bool> Notify(string type, string message, int userId, int questionId);
    }
}