using System.Net;

namespace MyProject.Application.Exceptions
{
    public class NotFoundException : AppException
    {
        public NotFoundException(string message, object? details = null)
            : base(message, HttpStatusCode.NotFound, "NOT_FOUND", details)
        {
        }
    }
}
