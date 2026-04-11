using Microsoft.EntityFrameworkCore;
using Taskify.Data;
using Taskify.Models;
using Taskify.Models.Enums.Achievements;
using Taskify.Models.Enums;

namespace Taskify.Services;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IAchievementService _achievementService;

    public UserService(
        ApplicationDbContext context, 
        INotificationService notificationService,
        IAchievementService achievementService)
    {
        _context = context;
        _notificationService = notificationService;
        _achievementService = achievementService;
    }

    public async Task AddXpAsync(string userId, int xpAmount)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return;

        user.Points += xpAmount;
        await UpdateLevelAsync(user);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateLevelAsync(User user)
    {
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
            
            await _notificationService.SendNotificationAsync(
                user.Id,
                "Level Up! ✨",
                $"Gratulujeme! Dosáhl jsi levelu {user.Level}.",
                NotificationPriority.Success,
                null,
                "/Profile",
                NotificationType.General
            );
            
            await _achievementService.CheckAchievementsAsync(user.Id, AchievementCategory.LevelReached);
        }
    }
}
