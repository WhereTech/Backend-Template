using BackendTemplate.Data;
using BackendTemplate.Resp;
using BackendTemplate.Services.utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BackendTemplate.Server;

/// <summary>
/// 给路由函数加上DB事务
/// </summary>
[InjectableService]
public class UseTransactionFilterService : IActionFilter
{
    private AppDbContext dbContext;
    private ILogger<UseTransactionFilterService> logger;
    public UseTransactionFilterService(
        AppDbContext dbContext,
        ILogger<UseTransactionFilterService> logger
    ) {
        this.dbContext = dbContext;
        this.logger = logger;
    }

    public void OnActionExecuting(ActionExecutingContext context) {
        dbContext.Database.BeginTransaction();
        logger.LogDebug("Begin Tran");
    }

    public void OnActionExecuted(ActionExecutedContext context) {
        string errCode = "Ok";
        if(context.Result is ServerResponse resp) {
            errCode = resp.model.errCode;
        }

        if(dbContext.Database.CurrentTransaction != null) {
            if(context.Exception != null || errCode != "Ok") {
                dbContext.Database.RollbackTransaction();
                if(context.Exception != null) {
                    logger.LogError(context.Exception.Message, "Exception in action, Rollback Tran");
                }
                else {
                    logger.LogError($"errCode={errCode}, Rollback Tran");
                }
            }
            else {
                dbContext.Database.CommitTransaction();
                logger.LogDebug("Commit Tran");
            }
        }
        else {
            logger.LogDebug("Need Not Commit Tran");
        }

    }
}
