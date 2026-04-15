using Microsoft.EntityFrameworkCore;
using Taskify.Data;
using Taskify.Models;
using Taskify.Models.Enums.Achievements;
using Taskify.Models.Enums.Notifications;

namespace Taskify.Services;

public class AchievementService : IAchievementService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILevelingService _levelingService;

    public AchievementService(ApplicationDbContext context, INotificationService notificationService, ILevelingService levelingService)
    {
        _context = context;
        _notificationService = notificationService;
        _levelingService = levelingService;
    }

    public async Task CheckAchievementsAsync(string userId, AchievementCategory category)
    {
        var user = await _context.Users
            .Include(u => u.Achievements)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return;

        // streaky
        if (category == AchievementCategory.TasksCompleted)
        {
            var now = DateTime.UtcNow;
            if (user.LastStreakUpdate == null)
            {
                user.CurrentWeeklyStreak = 1;
                user.LastStreakUpdate = now;
            }
            else
            {
                var timeSinceLast = now - user.LastStreakUpdate.Value;
                if (timeSinceLast.TotalDays >= 7 && timeSinceLast.TotalDays <= 14)
                {
                    // streak pokračuje
                    user.CurrentWeeklyStreak++;
                    user.LastStreakUpdate = now;
                    await _context.SaveChangesAsync(); // Uložíme streak před kontrolou
                    await CheckAchievementsAsync(userId, AchievementCategory.WeeklyStreak);
                }
                else if (timeSinceLast.TotalDays > 14)
                {
                    // uživatel vynechal týden -> streak se resetuje
                    user.CurrentWeeklyStreak = 1;
                    user.LastStreakUpdate = now;
                }
                // pokud je to méně než 7 dní, neděláme nic (v rámci jednoho týdne se streak nezvyšuje)
            }
        }
        
        var unlockedAchievementIds = user.Achievements
            .Where(ua => ua.IsUnlocked)
            .Select(ua => ua.AchievementId)
            .ToList();
        
        var lockedAchievements = await _context.Achievements
            .Where(a => a.Category == category)
            .Where(a => !unlockedAchievementIds.Contains(a.Id))
            .ToListAsync();

        foreach (var achievement in lockedAchievements)
        {
            bool shouldUnlock = category switch
            {
                AchievementCategory.TasksCompleted => user.TotalTasksCompleted >= achievement.TargetValue,
                AchievementCategory.TasksCreated => user.TotalTasksCreated >= achievement.TargetValue,
                AchievementCategory.LevelReached => user.Level >= achievement.TargetValue,
                AchievementCategory.ReputationPoints => user.Reputation >= achievement.TargetValue,
                AchievementCategory.WeeklyStreak => user.CurrentWeeklyStreak >= achievement.TargetValue,
                _ => false
            };

            if (shouldUnlock)
            {
                await UnlockAchievement(user, achievement);
            }
        }
    }


    public async Task CheckSpecialAchievementAsync(string userId, string internalName)
    {
        var user = await _context.Users
            .Include(u => u.Achievements)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return;
        
        var achievement = await _context.Achievements
            .Where(a => a.Category == AchievementCategory.Special && a.Name == internalName)
            .FirstOrDefaultAsync();

        if (achievement != null && !user.Achievements.Any(ua => ua.AchievementId == achievement.Id && ua.IsUnlocked))
        {
            await UnlockAchievement(user, achievement);
        }
    }

    private async Task UnlockAchievement(User user, Achievement achievement)
    {
        var userAchievement = user.Achievements.FirstOrDefault(ua => ua.AchievementId == achievement.Id);

        if (userAchievement == null)
        {
            userAchievement = new UserAchievement
            {
                UserId = user.Id,
                AchievementId = achievement.Id,
                CurrentProgress = achievement.TargetValue
            };
            _context.UserAchievements.Add(userAchievement);
        }

        userAchievement.IsUnlocked = true;
        userAchievement.EarnedAt = DateTime.UtcNow;
        userAchievement.CurrentProgress = achievement.TargetValue;

        if (_levelingService.AddExperience(user, achievement.XpReward))
        {
            await _notificationService.SendNotificationAsync(
                user.Id,
                "Level Up! ✨",
                $"Gratulujeme! Dosáhl jsi levelu {user.Level}.",
                NotificationPriority.Success,
                null,
                $"/u/{user.UserName}",
                NotificationType.General
            );   
            await CheckAchievementsAsync(user.Id, AchievementCategory.LevelReached);
        }

        await _context.SaveChangesAsync();

        // Notifikace o achievementu
        await _notificationService.SendNotificationAsync(
            user.Id,
            "Nový achievement získán! 🏆",
            $"Gratulujeme! Získal jsi odznak: {achievement.Name}",
            NotificationPriority.Success,
            null,
            "/Achievements",
            NotificationType.Achievement,
            achievement.IconUrl
        );
    }
}
