using Taskify.Models.Enums.Achievements;

namespace Taskify.Services;

public interface IAchievementService
{
    Task CheckAchievementsAsync(string userId, AchievementCategory category);
    
    Task CheckSpecialAchievementAsync(string userId, string internalName);
}
