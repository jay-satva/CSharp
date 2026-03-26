using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MyProject.API.Filters
{
    public class AutoValidationFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var allErrors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var argument in context.ActionArguments.Values)
            {
                if (argument is null)
                {
                    continue;
                }

                var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
                if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
                {
                    continue;
                }

                var validationContext = new ValidationContext<object>(argument);
                var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

                if (result.IsValid)
                {
                    continue;
                }

                foreach (var error in result.Errors)
                {
                    if (!allErrors.TryGetValue(error.PropertyName, out var messages))
                    {
                        messages = new List<string>();
                        allErrors[error.PropertyName] = messages;
                    }

                    if (!messages.Contains(error.ErrorMessage, StringComparer.Ordinal))
                    {
                        messages.Add(error.ErrorMessage);
                    }
                }
            }

            if (allErrors.Count > 0)
            {
                var errors = allErrors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray(), StringComparer.OrdinalIgnoreCase);

                context.Result = new UnprocessableEntityObjectResult(new
                {
                    statusCode = StatusCodes.Status422UnprocessableEntity,
                    errorCode = "VALIDATION_ERROR",
                    message = "Please correct the highlighted fields and try again.",
                    errors,
                    traceId = context.HttpContext.TraceIdentifier
                });

                return;
            }

            await next();
        }
    }
}
