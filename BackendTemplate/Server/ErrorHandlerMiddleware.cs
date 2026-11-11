using BackendTemplate.Resp;
using BackendTemplate.Services.utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BackendTemplate.Server;

public class ModelStateException : Exception
{
    public ModelStateDictionary ModelState { get; }

    public ModelStateException(ModelStateDictionary modelState) {
        ModelState = modelState;
    }
}

[InjectableService]
public class ErrorHandlerMiddleware : IMiddleware
{
    private readonly ILogger logger;

    public ErrorHandlerMiddleware(ILoggerFactory loggerFactory) {
        logger = loggerFactory.CreateLogger("ErrorHandler");
    }

    public static ServerResponse OnException(Exception e) {
        if(e is ServiceError serviceError) {
            return ServerResponse.Err(serviceError.ErrCode, serviceError.ErrMsg);
        }

        if(e is DbUpdateException dbUpdateException) {
            var e2 = dbUpdateException.InnerException;
            return ServerResponse.Err(ErrorCode.BadParam, e2 is PostgresException pgEx ? pgEx.Detail ?? pgEx.MessageText : dbUpdateException.Message);
        }

        if(e is UnauthorizedAccessException) {
            return ServerResponse.Err(ErrorCode.Unauthorized);
        }

        if(e is ModelStateException modelStateException) {
            return OnInvalidModelState(modelStateException.ModelState);
        }

        return ServerResponse.Err(ErrorCode.InternalError, e.Message);
    }

    public static ServerResponse OnInvalidModelState(ModelStateDictionary modelState) {
        var errors = modelState.Where(s => s.Value is { ValidationState: ModelValidationState.Invalid })
            .Select(s => s.Key + ": " + string.Join(", ", s.Value!.Errors.Select(e => e.ErrorMessage)))
            .ToList();
        return ServerResponse.Err(ErrorCode.BadParam, "数据验证错误 " + string.Join("; ", errors));
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next) {
        try {
            await next(context);
        }
        catch(Exception e) {
            logger.LogError(e, "Exception in handler: {Message}", e.Message);
            var resp = OnException(e);
            context.Items["ServerResponse"] = resp;
            await context.Response.WriteAsJsonAsync(resp.model);
        }
    }
}