using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskify.Models;

public class UserAchievement
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [Required]
    public int AchievementId { get; set; }

    [ForeignKey(nameof(AchievementId))]
    public Achievement? Achievement { get; set; }

    public DateTime? EarnedAt { get; set; }
    
    public int CurrentProgress { get; set; }
    
    public bool IsUnlocked { get; set; } = false;
}
