using System.Diagnostics;
using BackendTemplate.Resp;
using BackendTemplate.Services.utils;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BackendTemplate.Server;

[InjectableService]
public class RequestLoggingFilterService : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context) {
    }

    public void OnActionExecuted(ActionExecutedContext context) {
        if(context.Result is ServerResponse resp) {
            context.HttpContext.Items["ServerResponse"] = resp;
        }
    }
}
