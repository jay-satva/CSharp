using System.Net;
using System.Text.Json;
using FluentValidation;
using MyProject.Application.Exceptions;

namespace MyProject.API.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var (statusCode, errorCode, message, details, errors) = exception switch
            {
                ValidationAppException validationAppException => (
                    validationAppException.StatusCode,
                    validationAppException.ErrorCode,
                    validationAppException.Message,
                    validationAppException.Details,
                    validationAppException.Errors),
                AppException appException => (
                    appException.StatusCode,
                    appException.ErrorCode,
                    appException.Message,
                    appException.Details,
                    null),
                ValidationException validationException => (
                    HttpStatusCode.UnprocessableEntity,
                    "VALIDATION_ERROR",
                    "Please correct the highlighted fields and try again.",
                    null,
                    validationException.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).Distinct().ToArray())),
                KeyNotFoundException => (HttpStatusCode.NotFound, "NOT_FOUND", exception.Message, null, null),
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "UNAUTHORIZED", exception.Message, null, null),
                JsonException => (HttpStatusCode.BadRequest, "INVALID_JSON", "Request payload is not a valid JSON document.", null, null),
                FormatException => (HttpStatusCode.BadRequest, "INVALID_FORMAT", exception.Message, null, null),
                HttpRequestException => (HttpStatusCode.BadGateway, "HTTP_REQUEST_FAILED", "A downstream service request failed. Please try again.", null, null),
                _ => (HttpStatusCode.InternalServerError, "INTERNAL_SERVER_ERROR", "An unexpected server error occurred.", null, null)
            };

            context.Response.StatusCode = (int)statusCode;

            var result = JsonSerializer.Serialize(new
            {
                statusCode = (int)statusCode,
                errorCode,
                message,
                details,
                errors,
                traceId = context.TraceIdentifier
            });

            await context.Response.WriteAsync(result);
        }
    }
}
