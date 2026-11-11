using BackendTemplate.Resp;
using BackendTemplate.Services.utils;

namespace BackendTemplate.Server;

[InjectableService(InjectType.Singleton)]
public class ConcurrencyLimitMiddleware : IMiddleware
{
    private readonly int maxConcurrency;
    private readonly int waitTimeoutSeconds;
    private readonly SemaphoreSlim semaphore;

    public ConcurrencyLimitMiddleware(IConfiguration configuration) {
        maxConcurrency = configuration.GetValue("Server:MaxConcurrency", 20);
        waitTimeoutSeconds = configuration.GetValue("Server:WaitTimeoutSeconds", 30);
        semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next) {
        var acquired = await semaphore.WaitAsync(TimeSpan.FromSeconds(waitTimeoutSeconds));
        if(!acquired) {
            throw new ServiceError(ErrorCode.TooManyRequests, "服务器繁忙，请稍后再试");
        }

        try {
            await next(context);
        }
        finally {
            semaphore.Release();
        }
    }
}
