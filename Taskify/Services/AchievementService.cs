using Microsoft.EntityFrameworkCore;
using Taskify.Data;
using Taskify.Models;
using Taskify.Models.Enums.Achievements;
using Taskify.Models.Enums;

namespace Taskify.Services;

public class AchievementService : IAchievementService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public AchievementService(ApplicationDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task CheckAchievementsAsync(string userId, AchievementCategory category)
    {
        var user = await _context.Users
            .Include(u => u.Achievements)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return;

        // Načteme všechny achievementy v dané kategorii, které uživatel ještě nemá odemčené
        var lockedAchievements = await _context.Achievements
            .Where(a => a.Category == category)
            .Where(a => !user.Achievements.Any(ua => ua.AchievementId == a.Id && ua.IsUnlocked))
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

        // Odměna (XP)
        user.Points += achievement.XpReward;

        // Přepočet levelu (stejná logika jako v UserService pro konzistenci)
        int currentLvl = user.Level;
        double pointsNeededForNext = 100 * Math.Pow(1.1, currentLvl - 1);
        bool leveledUp = false;
        while (user.Points >= pointsNeededForNext)
        {
            currentLvl++;
            pointsNeededForNext = 100 * Math.Pow(1.1, currentLvl - 1);
            leveledUp = true;
        }

        if (leveledUp)
        {
            user.Level = currentLvl;
            // Notifikace o level-upu
            await _notificationService.SendNotificationAsync(
                user.Id,
                "Level Up! ✨",
                $"Gratulujeme! Dosáhl jsi levelu {user.Level}.",
                NotificationPriority.Success,
                null,
                "/Profile",
                NotificationType.General
            );
        }

        await _context.SaveChangesAsync();

        // Pokud došlo k level-upu, zkontrolujeme hned další achievementy pro LevelReached
        if (leveledUp)
        {
            await CheckAchievementsAsync(user.Id, AchievementCategory.LevelReached);
        }

        // Notifikace o achievementu
        await _notificationService.SendNotificationAsync(
            user.Id,
            "Nový achievement získán! 🏆",
            $"Gratulujeme! Získal jsi odznak: {achievement.Name}",
            NotificationPriority.Success,
            null,
            "/Achievements",
            NotificationType.General
        );
    }
}
