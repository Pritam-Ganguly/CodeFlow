using CodeFlow.core.Models;

namespace CodeFlow.core.Repositories.Seed
{
    public class BadgeDataSeed
    {
        private readonly IBadgeRepository _badgeRepository;

        public BadgeDataSeed(IBadgeRepository badgeRepository)
        {
            _badgeRepository = badgeRepository;
        }

        public async Task SeedBadges()
        {
            var badges = await _badgeRepository.GetAllBadgesAsync();
            if (badges.Any())
            {
                return;
            }

            var badgeList = GetBadges();
            foreach (Badge badge in badgeList)
            {
                await _badgeRepository.AddBadgeAsync(badge);
            }

        }

        public IEnumerable<Badge> GetBadges()
        {
            return new List<Badge>
            {
                new Badge
                {
                    Name = "Asked First Question",
                    Description = "Awarded for asking your first question.",
                    IconUrl = "/img/badges/badge-bronze.svg",
                    BadgeType = BadgeType.bronze,
                    TriggerCondition = BadgeTriggerCondition.askedFirstQuestion,
                },
                new Badge
                {
                    Name = "Asked Ten Questions",
                    Description = "Awarded for asking your first ten question.",
                    IconUrl = "/img/badges/badge-silver.svg",
                    BadgeType = BadgeType.silver,
                    TriggerCondition = BadgeTriggerCondition.askedTenQuestions,
                },
                new Badge
                {
                    Name = "Asked Fifty Question",
                    Description = "Awarded for asking your first fifty question.",
                    IconUrl = "/img/badges/badge-gold.svg",
                    BadgeType = BadgeType.gold,
                    TriggerCondition = BadgeTriggerCondition.askedFiftyQuestions,
                },
                new Badge
                {
                    Name = "Answered First Question",
                    Description = "Awarded for answering your first question.",
                    IconUrl = "/img/badges/badge-bronze.svg",
                    BadgeType = BadgeType.bronze,
                    TriggerCondition = BadgeTriggerCondition.answeredFirstQuestion,
                },
                new Badge
                {
                    Name = "Answered Ten Questions",
                    Description = "Awarded for answering your first ten question.",
                    IconUrl = "/img/badges/badge-silver.svg",
                    BadgeType = BadgeType.silver,
                    TriggerCondition = BadgeTriggerCondition.answeredTenQuestions,
                },
                new Badge
                {
                    Name = "Answered Fifty Question",
                    Description = "Awarded for answering your first fifty question.",
                    IconUrl = "/img/badges/badge-gold.svg",
                    BadgeType = BadgeType.gold,
                    TriggerCondition = BadgeTriggerCondition.answeredFiftyQuestions,
                },
                new Badge
                {
                    Name = "100 Reputation",
                    Description = "Awarded for reaching 100 reputation points.",
                    IconUrl = "/img/badges/badge-bronze.svg",
                    BadgeType = BadgeType.bronze,
                    TriggerCondition = BadgeTriggerCondition.reached100Reputation,
                },
                new Badge
                {
                    Name = "500 Reputation",
                    Description = "Awarded for reaching 500 reputation points.",
                    IconUrl = "/img/badges/badge-silver.svg",
                    BadgeType = BadgeType.silver,
                    TriggerCondition = BadgeTriggerCondition.reached500Reputation,
                },
                new Badge
                {
                    Name = "1000 Reputation",
                    Description = "Awarded for reaching 1000 reputation points.",
                    IconUrl = "/img/badges/badge-gold.svg",
                    BadgeType = BadgeType.gold,
                    TriggerCondition = BadgeTriggerCondition.reached1000Reputation,
                },

            };
        }
    }
}
