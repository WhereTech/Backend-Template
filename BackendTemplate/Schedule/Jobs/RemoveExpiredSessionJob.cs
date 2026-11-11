using BackendTemplate.Options;
using BackendTemplate.Data;
using BackendTemplate.Model;
using BackendTemplate.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BackendTemplate.Schedule.Jobs;

public class RemoveExpiredSessionJob(AppDbContext dbContext, IOptions<AuthConfig> authConfig, ILogger<RemoveExpiredSessionJob> logger) : IJob
{
    public async Task Execute() {
        var cnt = await dbContext.Sessions.Where(s =>
            s.is_apikey == false &&
            s.last_active_at < DateTime.UtcNow.AddDays(-authConfig.Value.SessionExpireDays)).ExecuteDeleteAsync();
        if(cnt > 0) logger.LogInformation($"Removed {cnt} expired sessions.");
    }
}