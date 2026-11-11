using BackendTemplate.Resp;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BackendTemplate.Util;

public static class DbExceptionUtil
{
    public static async Task<T> ThrowDuplicate<T>(this Task<T> task, string userMsg, string? constraintName = null) {
        try {
            return await task;
        }
        catch(DbUpdateException dbUpdateException) {
            if(dbUpdateException.InnerException is PostgresException postgresException) {
                if(postgresException.SqlState == "23505") {
                    if(constraintName == null || postgresException.ConstraintName!.Contains(constraintName)) {
                        throw new ServiceError(ErrorCode.BadParam, userMsg, postgresException);
                    }
                }
            }

            throw;
        }
    }

}