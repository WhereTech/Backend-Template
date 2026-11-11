using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BackendTemplate.Model;

public enum UserRole
{
    User = 0,
    Admin = 100,
}

[Table("users")]
[Index(nameof(mobile), IsUnique = true)]
public class User
{
    [Key]
    public int id { get; set; }

    [MaxLength(50)]
    public required string name { get; set; }

    [MaxLength(20)]
    public required string mobile { get; set; }

    public UserRole role { get; set; } = UserRole.User;

    public bool is_enabled { get; set; } = true;
}
