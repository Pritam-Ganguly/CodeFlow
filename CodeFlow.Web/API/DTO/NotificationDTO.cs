using CodeFlow.core.Models;

namespace CodeFlow.Web.API.DTO
{
    public class NotificationDTO
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public int QuestionId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public NotificationDTO(Notification notification)
        {
            Id = notification.Id;
            Type = notification.Type;
            Message = notification.Message;
            IsRead = notification.IsRead;
            QuestionId = notification.QuestionId;
            CreatedAt = notification.CreatedAt;
        }

    }
}
