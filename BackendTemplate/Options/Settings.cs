using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace BackendTemplate.Options;

public class ServerConfig
{
    public string? host { get; set; } = null;
}

public class DatabaseConfig
{
    public required string Provider { get; set; }
    public required string ConnectionString { get; set; }
}

public class AuthConfig
{
    [Range(1, 1440)]
    public int SmsCodeExpireMinutes { get; set; } = 10;

    [Range(1, 3650)]
    public int SessionExpireDays { get; set; } = 7;
}

public static class WebApplicationBuilderExt
{
    public static void InjectConfig(this WebApplicationBuilder builder) {
        builder.Services.Configure<ServerConfig>(builder.Configuration.GetSection("Server"));
        builder.Services.Configure<DatabaseConfig>(builder.Configuration.GetSection("Database"));
        builder.Services.AddOptions<AuthConfig>()
            .Bind(builder.Configuration.GetRequiredSection("Auth"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    public static ServerConfig GetServerConfig(this WebApplicationBuilder builder) {
        return builder.Configuration.GetSection("Server").Get<ServerConfig>()!;
    }
}

public static class ConfigExt
{
    public static T GetConfig<T>(this WebApplication app) where T : class {
        return app.Services.GetConfig<T>();
    }

    public static T GetConfig<T>(this IServiceProvider services) where T : class {
        return services.GetRequiredService<IOptions<T>>().Value;
    }
}
