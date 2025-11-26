using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace CodeFlow.Web.Filters
{
    public class RedirectIfAuthenticatedAttribute : ActionFilterAttribute
    {
        public string RedirectController { get; set; } = "Home";
        public string RedirectAction { get; set; } = "Index";

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                var logger = context.HttpContext.RequestServices.GetService<ILogger<RedirectIfAuthenticatedAttribute>>();
                logger?.LogInformation("Authenticated {UserName} attempted to access {Action}, redirecting to {Controller}/{Action}",
                    context.HttpContext.User.Identity.Name,
                    context.ActionDescriptor.DisplayName,
                    RedirectController,
                    RedirectAction);

                context.Result = new RedirectToActionResult(RedirectAction, RedirectController, null);
            }

            base.OnActionExecuting(context);
        }
    }
}
