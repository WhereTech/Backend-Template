using BackendTemplate.Options;

namespace BackendTemplate.Services;

public static class SmsServiceCollectionExtensions
{
    public static IServiceCollection AddSms(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<SMSConfig>()
            .Bind(configuration.GetRequiredSection("SMS"))
            .ValidateDataAnnotations()
            .Validate(config => config.TemplateCode.Values.All(value => !string.IsNullOrWhiteSpace(value)),
                "SMS template codes must not be empty.")
            .ValidateOnStart();
        services.AddSingleton<SMSService>();
        return services;
    }
}
