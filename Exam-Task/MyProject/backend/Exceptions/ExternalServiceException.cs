using System.Net;

namespace MyProject.Application.Exceptions
{
    public class ExternalServiceException : AppException
    {
        public ExternalServiceException(string message, object? details = null, HttpStatusCode statusCode = HttpStatusCode.BadGateway)
            : base(message, statusCode, "EXTERNAL_SERVICE_ERROR", details)
        {
        }
    }
}
