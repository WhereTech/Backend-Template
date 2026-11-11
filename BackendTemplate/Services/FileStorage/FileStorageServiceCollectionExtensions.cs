using BackendTemplate.Options;

namespace BackendTemplate.Services;

public static class FileStorageServiceCollectionExtensions
{
    public static IServiceCollection AddFileStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<FileStorageConfig>()
            .Bind(configuration.GetRequiredSection("FileStorage"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<FileStorageService>();
        services.AddScoped<FileService>();
        return services;
    }
}
