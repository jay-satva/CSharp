using System.Net;

namespace MyProject.Application.Exceptions
{
    public class ConflictException : AppException
    {
        public ConflictException(string message, object? details = null)
            : base(message, HttpStatusCode.Conflict, "CONFLICT", details)
        {
        }
    }
}
