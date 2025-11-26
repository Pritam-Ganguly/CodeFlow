using CodeFlow.core.Models;
using CodeFlow.core.Repositories;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace CodeFlow.Web.Filters
{
    public class TrackUserActivityAttribute : ActionFilterAttribute, IAsyncActionFilter
    {
        public ActivityType ActivityType { get; set; }
        public TargetEntityType TargetEntityType { get; set; }

        public TrackUserActivityAttribute(ActivityType activityType, TargetEntityType targetEntityType)
        {
            ActivityType = activityType;
            TargetEntityType = targetEntityType;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {

            var result = await next();

            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<TrackUserActivityAttribute>>();
            var user = context.HttpContext.User;
            var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;


            if(int.TryParse(userId, out int id))
            {
                try
                {
                    if (!result.Canceled)
                    {
                        var userActivityRepository = context.HttpContext.RequestServices.GetRequiredService<IUserActivityRepository>();
                        int? targetEntity = 0;

                        if (TargetEntityType == TargetEntityType.system)
                        {
                            targetEntity = null;
                        }

                        if (TargetEntityType == TargetEntityType.question || TargetEntityType == TargetEntityType.answer)
                        {
                            targetEntity = (int)context.ActionArguments.Values.First()!;
                        }

                        await userActivityRepository.AddUserActivityAsync(id, ActivityType, TargetEntityType, targetEntity);
                        logger.LogInformation("Added user activity tracking for user id {UserId}", userId);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occured while trackign activity for user id {UserId}", userId);
                }
            }
        }

    }
}
