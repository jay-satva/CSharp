using System.Net;

namespace MyProject.Application.Exceptions
{
    public abstract class AppException : Exception
    {
        protected AppException(string message, HttpStatusCode statusCode, string errorCode, object? details = null)
            : base(message)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
            Details = details;
        }

        public HttpStatusCode StatusCode { get; }
        public string ErrorCode { get; }
        public object? Details { get; }
    }
}
