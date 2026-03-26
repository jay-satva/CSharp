using System.Net;

namespace MyProject.Application.Exceptions
{
    public class BadRequestException : AppException
    {
        public BadRequestException(string message, object? details = null)
            : base(message, HttpStatusCode.BadRequest, "BAD_REQUEST", details)
        {
        }
    }
}
