using Hangfire;
using Hangfire.Storage;
using BackendTemplate.Schedule.Jobs;

namespace BackendTemplate.Schedule;

public class ScopedJob<T>(IServiceProvider _serviceProvider) where T : IJob
{
    public async Task ExecuteInScope() {
        var _logger = _serviceProvider.GetRequiredService<ILogger<T>>();
        using var scope = _serviceProvider.CreateScope();
        try {
            var jobImpl = ActivatorUtilities.CreateInstance<T>(scope.ServiceProvider);
            await jobImpl.Execute();
            _logger.LogInformation($"Job executed successfully.");
        }
        catch(Exception ex) {
            _logger.LogError(ex, $"Job execution failed");
            throw;
        }
    }

    public void ValidateDI() {
        using var scope = _serviceProvider.CreateScope();
        ActivatorUtilities.CreateInstance<T>(scope.ServiceProvider);
    }
}

public static class HangfireJobsCenter
{
    private static List<object> jobs = [];

    private static IServiceProvider serviceProvider = null!;

    private static void AddCronJob<T>(string cron, bool runNow = false) where T : IJob {
        var opt = new RecurringJobOptions() {
            TimeZone = TimeZoneInfo.Local,
            MisfireHandling = MisfireHandlingMode.Relaxed,
        };
        var recurringJob = serviceProvider.GetRequiredService<IRecurringJobManager>();
        var job = new ScopedJob<T>(serviceProvider);
        job.ValidateDI();
        jobs.Add(job);
        recurringJob.AddOrUpdate(typeof(T).Name, () => job.ExecuteInScope(), cron, opt);
        if(runNow) {
            recurringJob.Trigger(typeof(T).Name);
        }
    }

    public static void StartJobs(WebApplication app) {
        serviceProvider = app.Services;
        AddCronJob<RemoveExpiredSessionJob>(Cron.Minutely());
        AddCronJob<RemoveExpiredSMSJob>(Cron.Minutely());
    }
}
