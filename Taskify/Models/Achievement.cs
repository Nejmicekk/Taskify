using System.ComponentModel.DataAnnotations;
using Taskify.Models.Enums.Achievements;

namespace Taskify.Models;

public class Achievement
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public AchievementCategory Category { get; set; }

    [Required]
    public AchievementRarity Rarity { get; set; } = AchievementRarity.Common;
    
    [Required]
    public int TargetValue { get; set; }

    [StringLength(255)]
    public string? IconUrl { get; set; }

    public bool IsSecret { get; set; } = false;
    
    public int XpReward { get; set; } = 0;

    // Vazba na uživatele, kteří tento achievement mají nebo na něm pracují
    public ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
}
