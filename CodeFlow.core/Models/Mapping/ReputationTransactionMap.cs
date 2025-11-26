namespace CodeFlow.core.Models.Mapping
{
    public class ReputationTransactionMap
    {

        public static readonly IReadOnlyDictionary<ReputationTransactionTypes, int> ReputationMap = 
            new Dictionary<ReputationTransactionTypes, int>
            {
                { ReputationTransactionTypes.Question_Upvoted, 10 },
                { ReputationTransactionTypes.Answer_Upvoted, 10 },
                { ReputationTransactionTypes.Answer_Accepted, 25 },
                { ReputationTransactionTypes.Question_Downvoted, -2 },
                { ReputationTransactionTypes.Answer_Downvoted, -2 },
                { ReputationTransactionTypes.Downvote_On_Post, -1 },
                { ReputationTransactionTypes.First_Post, +50 },
                { ReputationTransactionTypes.Post_Delete, -1 },
                { ReputationTransactionTypes.Post_Got_Deleted, -25 },
            };
    }

    public enum ReputationTransactionTypes
    {
        Question_Upvoted,
        Answer_Upvoted,
        Answer_Accepted,
        Question_Downvoted,
        Answer_Downvoted,
        Downvote_On_Post,
        First_Post,
        Post_Delete,
        Post_Got_Deleted,
    }

    public enum RelatedPostType
    {
        Question,
        Answer
    }
}
