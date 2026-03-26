using System.Net;

namespace MyProject.Application.Exceptions
{
    public class ValidationAppException : AppException
    {
        public ValidationAppException(string message, Dictionary<string, string[]> errors)
            : base(message, HttpStatusCode.UnprocessableEntity, "VALIDATION_ERROR", errors)
        {
            Errors = errors;
        }

        public Dictionary<string, string[]> Errors { get; }
    }
}
