using Microsoft.AspNetCore.Mvc.Filters;

namespace BackendTemplate.Server;

public class InvalidModelStateFilter : IAsyncActionFilter
{
    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
        if(!context.ModelState.IsValid) {
            throw new ModelStateException(context.ModelState);
        }
        return next();
    }
}