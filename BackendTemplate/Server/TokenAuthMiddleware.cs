using BackendTemplate.Services;
using BackendTemplate.Services.utils;

namespace BackendTemplate.Server;

[InjectableService]
public class TokenAuthMiddleware(AuthService authService) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var token = context.Request.Headers["Token"].ToString();
        if (string.IsNullOrWhiteSpace(token))
        {
            var authorization = context.Request.Headers.Authorization.ToString();
            if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                token = authorization["Bearer ".Length..].Trim();
        }

        if (!string.IsNullOrWhiteSpace(token)) await authService.LoadToken(token);
        await next(context);
    }
}
