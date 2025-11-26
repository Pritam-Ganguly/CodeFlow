namespace CodeFlow.core.Models
{
    public class UserActivity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public ActivityType ActivityType { get; set; }
        public TargetEntityType TargetEntityType { get; set; }
        public int? TargetEntityId { get; set; }
        public DateTime CreatedAt { get; set; }

    }

    public enum ActivityType
    {
        question_asked,
        answer_posted, 
        comment_added,
        vote_cast,
        post_edited,
        answer_accepted,
        answer_accepted_owner,
        badge_earned,
    }

    public enum TargetEntityType
    {
        system,
        question,
        answer, 
        comment,
        vote,
        user,
        badge
    }
}
