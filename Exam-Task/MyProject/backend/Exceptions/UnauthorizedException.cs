using System.Net;

namespace MyProject.Application.Exceptions
{
    public class UnauthorizedException : AppException
    {
        public UnauthorizedException(string message, object? details = null)
            : base(message, HttpStatusCode.Unauthorized, "UNAUTHORIZED", details)
        {
        }
    }
}
