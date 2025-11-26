using CodeFlow.core.Models.Mapping;

namespace CodeFlow.core.Repositories.Utils
{
    public class DescriptionHelper
    {
        public static string GetDescription(ReputationTransactionTypes reputationTransactionTypes, int relatedPostId, RelatedPostType relatedPostType)
        {
            return reputationTransactionTypes switch
            {
                ReputationTransactionTypes.Question_Upvoted => "Upvote on question '{0}'",
                ReputationTransactionTypes.Question_Downvoted => "Downvote on question '{0}'",
                ReputationTransactionTypes.Answer_Upvoted => $"Upvote on an answer",
                ReputationTransactionTypes.Answer_Downvoted => $"Downvote on an answer",
                ReputationTransactionTypes.Downvote_On_Post =>
                    relatedPostType == RelatedPostType.Question ? "Downvoted question '{0}'" : "Downvoted and answer",
                ReputationTransactionTypes.Answer_Accepted => "You're answer on question '{0}', got accepted",
                ReputationTransactionTypes.First_Post => "Ask you're first question",
                ReputationTransactionTypes.Post_Delete => "Deleted a post",
                ReputationTransactionTypes.Post_Got_Deleted => "One of you're post got deleted.",
                _ => ""
            };
        }
    }
}
