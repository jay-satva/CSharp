using FinalExam.Services;

namespace FinalExam.Middlewares;

public class RefreshMiddleware
{
    private readonly RequestDelegate _next;

    public RefreshMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, QuickBooksService qbService)
    {
        await _next(context);
    }
}
