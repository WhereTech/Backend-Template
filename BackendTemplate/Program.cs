using BackendTemplate.Data;
using BackendTemplate.Options;
using BackendTemplate.Server;
using BackendTemplate.Schedule;
using BackendTemplate.Services;
using BackendTemplate.Services.utils;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace BackendTemplate;

public static class Program
{
    public static WebApplication BuildApp(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var configFile = builder.Configuration["configFile"] ?? "appsettings.json";
        builder.Configuration
            .AddJsonFile(configFile, optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args);

        builder.InjectConfig();
        var databaseConfig = builder.Configuration.GetRequiredSection("Database").Get<DatabaseConfig>()!;

        builder.Services.AddDbContext<AppDbContext>(options => ConfigureDatabase(options, databaseConfig));
        builder.Services.AddControllers(options =>
            {
                options.Filters.AddService<RequestLoggingFilterService>();
                options.Filters.Add<InvalidModelStateFilter>();
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                    ErrorHandlerMiddleware.OnInvalidModelState(context.ModelState);
            })
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();
        builder.Services.AddSchedule();
        builder.Services.AutoInjectCustomServices();
//#if (FileStorage)
        builder.Services.AddFileStorage(builder.Configuration);
//#endif
//#if (SMS)
        builder.Services.AddSms(builder.Configuration);
//#endif

        var app = builder.Build();

        if (databaseConfig.Provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            using var scope = app.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<ErrorHandlerMiddleware>();
        app.UseMiddleware<TokenAuthMiddleware>();
        app.MapControllers();
        app.MapHealthChecks("/health");
        app.MapGet("/", () => Results.Ok(new { service = "BackendTemplate", status = "ok" }));
        return app;
    }

    public static void StartApp(WebApplication app)
    {
        app.StartSchedule();
        var serverConfig = app.Services.GetConfig<ServerConfig>();
        app.Run(serverConfig.host);
    }

    public static void Main(string[] args) => StartApp(BuildApp(args));

    private static void ConfigureDatabase(DbContextOptionsBuilder options, DatabaseConfig config)
    {
        switch (config.Provider.ToLowerInvariant())
        {
            case "postgresql":
            case "postgres":
            case "npgsql":
                options.UseNpgsql(config.ConnectionString);
                break;
            case "sqlite":
                options.UseSqlite(config.ConnectionString);
                break;
            default:
                throw new InvalidOperationException(
                    $"Unsupported database provider '{config.Provider}'. Supported values: PostgreSql, Sqlite.");
        }
    }
}
