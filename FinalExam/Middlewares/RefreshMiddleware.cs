namespace FinalExam.Middlewares
{
    using FinalExam.Services;
    using Microsoft.AspNetCore.Http;

    public class RefreshMiddleware
    {
        private readonly RequestDelegate _next;

        public RefreshMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, QuickBooksService qbService)
        {
            var userId = context.Session.GetString("userId");
            if (!string.IsNullOrEmpty(userId))
            {
                try 
                {

                    await qbService.GetAccessTokenAsync(userId);
                }
                catch 
                {

                }
            }
            await _next(context);
        }
    }
}
