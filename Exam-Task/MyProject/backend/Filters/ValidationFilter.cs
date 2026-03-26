using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MyProject.API.Filters
{
    public class ValidationFilter<T> : IAsyncActionFilter
    {
        private readonly IValidator<T> _validator;

        public ValidationFilter(IValidator<T> validator)
        {
            _validator = validator;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var argument = context.ActionArguments.Values.OfType<T>().FirstOrDefault();
            if (argument != null)
            {
                var result = await _validator.ValidateAsync(argument);
                if (!result.IsValid)
                {
                    var errors = result.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()
                        );
                    context.Result = new BadRequestObjectResult(new { errors });
                    return;
                }
            }

            await next();
        }
    }
}