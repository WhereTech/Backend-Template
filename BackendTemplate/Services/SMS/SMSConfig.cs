using System.ComponentModel.DataAnnotations;

namespace BackendTemplate.Options;

public sealed class SMSConfig
{
    [Required]
    public string AccessKeyId { get; init; } = string.Empty;

    [Required]
    public string AccessKeySecret { get; init; } = string.Empty;

    [Required]
    public string SignName { get; init; } = string.Empty;

    [Required]
    public string SdkRegion { get; init; } = string.Empty;

    [Required, MinLength(1)]
    public Dictionary<string, string> TemplateCode { get; init; } = new();
}
