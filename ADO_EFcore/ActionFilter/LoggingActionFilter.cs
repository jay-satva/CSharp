using Microsoft.AspNetCore.Mvc.Filters;

namespace ADO_EFcore.ActionFilter
{
    public class LoggingActionFilter : IAsyncActionFilter
    {
        private readonly ILogger<LoggingActionFilter> _logger;

        public LoggingActionFilter(ILogger<LoggingActionFilter> logger)
        {
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var actionName = context.ActionDescriptor.DisplayName;
            var user = context.HttpContext.User.Identity?.Name ?? "Anonymous";

            _logger.LogInformation("Action '{Action}' started by user '{User}' at {Time}",
                actionName, user, DateTime.UtcNow);

            var result = await next();

            _logger.LogInformation("Action '{Action}' completed with status {Status} at {Time}",
                actionName, result.HttpContext.Response.StatusCode, DateTime.UtcNow);
        }
    }
}