using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BackendTemplate.Model;

public enum SmsOperation
{
    Register = 1,
    Login = 10,
}

[Table("sms_codes")]
[Index(nameof(mobile), nameof(operation))]
[Index(nameof(send_at))]
public class SmsCode
{
    [Key]
    public int id { get; set; }

    [MaxLength(20)]
    public required string mobile { get; set; }

    [MaxLength(6)]
    public required string code { get; set; }

    public required SmsOperation operation { get; set; }

    public DateTime send_at { get; set; } = DateTime.UtcNow;
}
