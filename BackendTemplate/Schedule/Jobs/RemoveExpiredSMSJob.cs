using BackendTemplate.Options;
using BackendTemplate.Data;
using BackendTemplate.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BackendTemplate.Schedule.Jobs;

public class RemoveExpiredSMSJob(AppDbContext dbContext, IOptions<AuthConfig> authConfig, ILogger<RemoveExpiredSMSJob> logger) : IJob
{
    public async Task Execute() {
        var cnt = await dbContext.SmsCodes.Where(s => s.send_at < DateTime.UtcNow.AddMinutes(-authConfig.Value.SmsCodeExpireMinutes)).ExecuteDeleteAsync();
        if(cnt > 0) logger.LogInformation($"Removed {cnt} expired SMS codes.");
    }
}