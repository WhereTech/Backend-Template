# 后端开发约定与注意事项

## 应用入口与测试

入口是显式 `Program` class：

```csharp
var app = Program.BuildApp(args);
Program.StartApp(app);
```

`BuildApp` 完成配置、依赖注入、DbContext、中间件和路由构建，但不监听端口。`StartApp` 只负责调用 `Run`。

集成测试可以只构建应用并使用真实 DI：

```csharp
await using var app = Program.BuildApp(["--configFile", configPath]);
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
var auth = scope.ServiceProvider.GetRequiredService<AuthService>();
```

不要直接从根容器解析 Scoped 服务，必须先创建 Scope。

## 依赖注入

普通业务服务使用 `[InjectableService]`：

```csharp
[InjectableService]
public class OrderService(AppDbContext dbContext)
{
}
```

`AutoInjectCustomServices` 会扫描当前程序集：默认注册为 Scoped，也可指定 `InjectType.Singleton` 或 `InjectType.Transient`。

生命周期注意事项：

- 使用 `AppDbContext`、当前用户或请求数据的服务必须是 Scoped；
- Singleton 不得直接依赖 Scoped 服务；
- 只有无状态、线程安全且不持有请求资源的客户端适合 Singleton；
- 可选外部服务使用 `AddFileStorage`、`AddSms` 扩展显式注册和校验配置。

## 配置绑定与校验

通用配置在 `Options/Settings.cs` 定义，通过 `IOptions<T>` 注入。缺失后无法工作的配置应使用：

```csharp
services.AddOptions<ExampleConfig>()
    .Bind(configuration.GetRequiredSection("Example"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

不要等到第一次请求才发现配置错误。真实配置只进入 `appsettings.json`、环境变量或项目外配置文件；样例写入 `appsettings.sample.json`。

## 统一响应与异常

业务接口通常返回 `ServerResponse`：

```csharp
return ServerResponse.Ok(data);
throw new ServiceError(ErrorCode.ResourceNotFound, "找不到数据");
```

`ErrorHandlerMiddleware` 会把业务异常、参数验证和数据库更新错误转换为统一的 `errCode`、`errMsg`、`data` 结构。新增业务错误先扩展 `ErrorCode`，不要在每个 Controller 重复 try/catch。

当前约定沿用原项目：业务错误通常通过 `errCode` 表达。若客户端需要严格依赖 HTTP 4xx/5xx，应统一修改中间件，而不是在单个接口中改变规则。

## 事务一致性

需要一个 Action 内全部成功或全部回滚的 Controller 使用：

```csharp
[ServiceFilter<UseTransactionFilterService>]
public class OrderController : ControllerBase
{
}
```

过滤器在 Action 前开启事务；发生异常或返回的 `ServerResponse.errCode != Ok` 时回滚，否则提交。

注意事项：

- 不要在同一请求中再开启不协调的嵌套事务；
- 短信、对象存储和第三方 HTTP 不会随数据库事务回滚，需要 Outbox、补偿任务或幂等键保证最终一致性；
- 返回 `IResult`、文件流等非 `ServerResponse` 时，只能依据异常判断成功失败；
- 长任务不要占用请求事务，应拆成后台任务；
- `DbContext` 是 Scoped，不得保存到全局回调或 Singleton。

## DB Model 定义

实体放在 `Model/`，DbSet 放在 `Data/AppDbContext.cs`：

```csharp
[Table("orders")]
[Index(nameof(biz_id), IsUnique = true)]
public class Order
{
    [Key]
    public int id { get; set; }

    [MaxLength(100)]
    public required string biz_id { get; set; }

    public DateTime created_at { get; set; } = DateTime.UtcNow;
}
```

约定：

- 表名和字段名沿用 snake_case；
- 非空字符串使用 `required`，并明确 `[MaxLength]`；
- 唯一性和常用查询条件使用 `[Index]` 或 Fluent API；
- 时间统一保存 UTC `DateTime`，避免 SQLite 无法翻译部分 `DateTimeOffset` 排序；
- 导航属性明确外键字段，避免不可预期的 Shadow Property；
- 复杂业务中分离 API DTO 和数据库实体，避免暴露敏感列；
- 新增实体后把 `DbSet<T>` 加入 `AppDbContext`；复杂映射放到 `IEntityTypeConfiguration<T>`，由 `ApplyConfigurationsFromAssembly` 自动加载。

SQLite 的 `EnsureCreated` 只用于开箱测试，不等价于 Migration。正式开发应创建 Migration，并在 PostgreSQL 部署流程中显式应用。

## 手机登录与鉴权

流程为：发送验证码，调用注册或登录接口，服务端创建数据库 Session，后续请求携带 `Token` 或 `Authorization: Bearer`，由 `TokenAuthMiddleware` 加载当前用户。

未生成 SMS 服务时验证码写入 Warning 日志，只能用于本地开发。生产环境必须生成并配置 SMS 服务，或替换成其他真实发送方式。

Controller 中使用：

```csharp
authService.RequireLogin();
authService.RequireRole(UserRole.Admin);
```

Session 过期时间来自 `Auth.SessionExpireDays`。`is_apikey = true` 的 Session 不检查活跃时间，创建 API Key 时必须额外限制权限并提供撤销机制。

## Schedule 与数据库清理

模板使用 Hangfire InMemory 执行后台任务。`BuildApp` 只注册 Hangfire 服务，不启动任务；`StartApp` 调用 `StartSchedule` 后才注册 recurring job 并启动 Host，因此只 Build 的集成测试不会执行后台任务。

内置任务每分钟执行一次：

- `RemoveExpiredSessionJob`：删除超过 `Auth.SessionExpireDays` 且不是 API Key 的 Session；
- `RemoveExpiredSMSJob`：删除超过 `Auth.SmsCodeExpireMinutes` 的验证码。

新增任务：

```csharp
public class ExampleCleanupJob(AppDbContext db, ILogger<ExampleCleanupJob> logger) : IJob
{
    public async Task Execute()
    {
        var count = await db.Examples
            .Where(item => item.created_at < DateTime.UtcNow.AddDays(-30))
            .ExecuteDeleteAsync();
        logger.LogInformation("Removed {Count} expired examples.", count);
    }
}
```

然后在 `HangfireJobsCenter.StartJobs` 中调用 `AddCronJob<ExampleCleanupJob>(cron)`。Job 会在独立 DI Scope 中创建，因此可以安全使用 Scoped DbContext。

注意事项：

- Job 构造函数依赖会在启动时验证，缺少注册会导致启动失败；
- Job 异常必须继续抛出，Hangfire 才会按全局策略重试；模板默认重试 3 次，每次间隔 5 秒；
- 清理条件必须使用 UTC 时间，并为时间字段建立索引；
- 批量清理优先使用 `ExecuteDeleteAsync`，避免把所有行加载进内存；
- 大批量删除应分批处理，避免长事务和数据库锁；
- Job 必须幂等，因为失败重试或多实例部署都可能重复执行；
- Hangfire InMemory 不保存执行历史，进程重启后历史会丢失；recurring job 会在启动时重新注册；
- 多实例部署时每个实例都有独立 InMemory 调度器，可能重复执行同一任务。正式集群需要切换到共享 Hangfire Storage，或确保只有一个实例启动 Schedule。

## 可选服务与条件编译

可选目录由 `.template.config/template.json` 控制。SMS 还通过 `SMS` 编译常量切换真实发送和日志发送。

修改条件代码后，必须分别生成并编译：默认、`--FileStorage true`、`--SMS true`、两者同时启用。不要只编译模板源工程，因为源工程包含全部可选服务，覆盖不到条件删除后的错误。
