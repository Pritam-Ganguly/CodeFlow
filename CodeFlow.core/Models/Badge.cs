namespace CodeFlow.core.Models
{
    public class Badge
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public BadgeType BadgeType { get; set; }
        public BadgeTriggerCondition TriggerCondition { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public enum BadgeTriggerCondition
    {
        reached100Reputation,
        reached500Reputation,
        reached1000Reputation,
        askedFirstQuestion,
        askedTenQuestions,
        askedFiftyQuestions,
        answeredFirstQuestion,
        answeredTenQuestions,
        answeredFiftyQuestions,
    }

    public enum BadgeType
    {
        bronze,
        silver,
        gold
    }
}
