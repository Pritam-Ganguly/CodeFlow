using CodeFlow.core.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace CodeFlow.Web.Components
{
    public class UserActivityViewComponent : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(IEnumerable<UserActivity> userActivities)
        {
            IList<UserActivityViewComponentModel> userActivityViewComponentModel = new List<UserActivityViewComponentModel>();

            foreach (var activity in userActivities)
            {
                StringBuilder st = new StringBuilder();

                st.Append(activity.ActivityType switch {
                    ActivityType.question_asked => "Asked a question",
                    ActivityType.answer_posted => "Added an answer",
                    ActivityType.answer_accepted => "Accepted an answer",
                    ActivityType.vote_cast => "Updated vote",
                    _ => "",
                });

                st.Append(activity.TargetEntityType switch
                {
                    TargetEntityType.question => " for a question",
                    TargetEntityType.answer => " for a answer",
                    _ => ""
                });

                userActivityViewComponentModel.Add(new UserActivityViewComponentModel()
                {
                    Description = st.ToString(),
                    ActivityTime = activity.CreatedAt
                });

            }
            return View(userActivityViewComponentModel);
        }
    }
}
