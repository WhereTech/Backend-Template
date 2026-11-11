using Hangfire;

namespace BackendTemplate.Schedule;

/// <summary>
/// 增加HangFire到单独配置
/// </summary>
public static class HangFireSetup
{
    public static void AddSchedule(this IServiceCollection services) {
        services.AddHangfire(x => x.UseInMemoryStorage());
        services.AddHangfireServer(options => {
            options.SchedulePollingInterval = TimeSpan.FromSeconds(15); //秒级任务需要配置短点，一般任务可以配置默认时间，默认15秒
            options.ShutdownTimeout = TimeSpan.FromMinutes(5); //超时时间
            options.WorkerCount = Math.Max(Environment.ProcessorCount, 4); //工作线程数，当前允许的最大线程，默认20
        });
    }

    public static void StartSchedule(this WebApplication app) {
        HangfireJobsCenter.StartJobs(app);
        GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = 3, DelayInSecondsByAttemptFunc = _ => 5 });
    }
}
