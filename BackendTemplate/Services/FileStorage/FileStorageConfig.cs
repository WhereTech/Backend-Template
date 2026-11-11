using System.ComponentModel.DataAnnotations;

namespace BackendTemplate.Options;

public sealed class FileStorageConfig
{
    [Required, Url]
    public string Endpoint { get; init; } = string.Empty;

    [Required]
    public string AccessKey { get; init; } = string.Empty;

    [Required]
    public string SecretKey { get; init; } = string.Empty;

    [Required]
    public string Bucket { get; init; } = string.Empty;
}
