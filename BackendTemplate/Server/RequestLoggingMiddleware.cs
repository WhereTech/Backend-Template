using System.Diagnostics;
using BackendTemplate.Resp;
using BackendTemplate.Services.utils;

namespace BackendTemplate.Server;

[InjectableService]
public class RequestLoggingMiddleware : IMiddleware
{
    private readonly ILogger logger;

    public RequestLoggingMiddleware(ILoggerFactory loggerFactory) {
        logger = loggerFactory.CreateLogger("RequestLogging");
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next) {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        Exception? err = null;

        try {
            await next(context);
        }
        catch(Exception e) {
            err = e;
        }

        stopwatch.Stop();

        var log = $"{context.Request.Method} {context.Request.Path} | {context.Connection.RemoteIpAddress} | {stopwatch.Elapsed.TotalMilliseconds:F0}ms | {context.Response.StatusCode} ";

        if(err != null) {
            log += $"| ERR Exception: {err.Message}";
            logger.LogError(log);
            throw err;
        }

        var resp = context.Items.ContainsKey("ServerResponse") ? context.Items["ServerResponse"] as ServerResponse : null;

        bool isError = false;
        if(context.Response.StatusCode >= 400 ) {
            isError = true;
        }
        else if(resp == null) {
            // pass
        }
        else if(resp.model.errCode == ErrorCode.Ok.ToString()) {
            log += "| Success";
        }
        else if(string.IsNullOrWhiteSpace(resp.model.errMsg)) {
            log += $"| ERR {resp.model.errCode}";
            isError = true;
        }else {
            log += $"| ERR {resp.model.errCode}: {resp.model.errMsg}";
            isError = true;
        }

        if(isError) {
            logger.LogError(log);
        }
        else {
            logger.LogInformation(log);
        }
    }
}