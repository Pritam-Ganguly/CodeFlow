using CodeFlow.core.Models.Mapping;

namespace CodeFlow.core.Repositories.Utils
{
    public class DescriptionHelper
    {
        public static string GetDescription(ReputationTransactionTypes reputationTransactionTypes, int relatedPostId, RelatedPostType relatedPostType)
        {
            return reputationTransactionTypes switch
            {
                ReputationTransactionTypes.Question_Upvoted => "Upvoted question '{0}'",
                ReputationTransactionTypes.Question_Downvoted => "Downvoted question '{{0}}'",
                ReputationTransactionTypes.Answer_Upvoted => $"Upvoted an answer",
                ReputationTransactionTypes.Answer_Downvoted => $"Downvoted an answer",
                ReputationTransactionTypes.Downvote_On_Post =>
                    relatedPostType == RelatedPostType.Question ? "You're question '{{0}}' got downvoted" : "One of You're answers got downvoted",
                ReputationTransactionTypes.Answer_Accepted => "You're answer on question '{{0}}', got accepted",
                ReputationTransactionTypes.First_Post => "Ask you're first questiosn '{{0}}'",
                ReputationTransactionTypes.Post_Delete => "Downvoted question '{{0}}'",
                ReputationTransactionTypes.Post_Got_Deleted => "Downvoted question '{{0}}'",
                _ => ""
            };
        }
    }
}
