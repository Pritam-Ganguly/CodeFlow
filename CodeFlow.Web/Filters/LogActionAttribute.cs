using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;

namespace CodeFlow.Web.Filters
{
    public class LogActionAttribute : ActionFilterAttribute
    {
        private Stopwatch? _stopwatch;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            _stopwatch = Stopwatch.StartNew();

            var logger = context.HttpContext.RequestServices.GetService<ILogger<LogActionAttribute>>();

            logger?.LogInformation("Started executing action: {ActionName} for user: {UserName} with IP {IPAddress}",
                context.ActionDescriptor.DisplayName,
                context.HttpContext.User.Identity?.Name ?? "Anonymous",
                context.HttpContext.Connection.RemoteIpAddress
                );

            base.OnActionExecuting(context);
        }


        public override void OnActionExecuted(ActionExecutedContext context)
        {
            _stopwatch?.Stop();

            var logger = context.HttpContext.RequestServices.GetService<ILogger<LogActionAttribute>>();


            if(context.Exception == null)
            {
                logger?.LogInformation("Completed action: {ActionName} in {ElapsedMs} for user: {UserName} with result: {ResultType}",
                    context.ActionDescriptor.DisplayName,
                    _stopwatch?.ElapsedMilliseconds,
                    context.HttpContext.User.Identity?.Name ?? "Anonymous",
                    context.Result?.GetType().Name ?? "Unknown");
            }
            else
            {
                logger?.LogInformation(context.Exception, "Action {ActionName} failed after {ElapsedMs}ms", 
                    context.ActionDescriptor.DisplayName, 
                    _stopwatch?.ElapsedMilliseconds);
            }

            
            base.OnActionExecuted(context);
        }
    }
}
