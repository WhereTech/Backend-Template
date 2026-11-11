using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BackendTemplate.Model;

[Table("sessions")]
[Index(nameof(token), IsUnique = true)]
[Index(nameof(last_active_at))]
public class Session
{
    [Key]
    public int id { get; set; }

    public int user_id { get; set; }

    [ForeignKey(nameof(user_id))]
    public required User user { get; set; }

    [MaxLength(50)]
    public required string token { get; set; }

    public DateTime last_active_at { get; set; } = DateTime.UtcNow;

    public bool is_apikey { get; set; } = false;
}
