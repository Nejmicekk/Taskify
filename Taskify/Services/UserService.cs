using Microsoft.EntityFrameworkCore;
using Taskify.Data;
using Taskify.Models;
using Taskify.Models.Enums.Achievements;
using Taskify.Models.Enums;
using Taskify.Models.Enums.Notifications;

namespace Taskify.Services;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IAchievementService _achievementService;
    private readonly ILevelingService _levelingService;

    public UserService(
        ApplicationDbContext context, 
        INotificationService notificationService,
        IAchievementService achievementService,
        ILevelingService levelingService)
    {
        _context = context;
        _notificationService = notificationService;
        _achievementService = achievementService;
        _levelingService = levelingService;
    }

    public async Task AddXpAsync(string userId, int xpAmount)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return;

        if (_levelingService.AddExperience(user, xpAmount))
        {
            await _notificationService.SendNotificationAsync(user.Id,
                "Level Up! ✨",
                $"Gratulujeme! Dosáhl jsi levelu {user.Level}.",
                NotificationPriority.Success,
                null,
                "/Profile",
                NotificationType.General);
            await _achievementService.CheckAchievementsAsync(user.Id, AchievementCategory.LevelReached);
        }
        await _context.SaveChangesAsync();
    }

    public async Task ChangeReputationAsync(string userId, int amount)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return;

        user.Reputation += amount;

        // Notifikace o změně reputace
        string title = amount > 0 ? "Reputace zvýšena! 📈" : "Reputace snížena! 📉";
        string message = amount > 0 
            ? $"Získal jsi {amount} bodů reputace." 
            : $"Ztratil jsi {Math.Abs(amount)} bodů reputace.";

        await _notificationService.SendNotificationAsync(
            user.Id,
            title,
            message,
            amount > 0 ? NotificationPriority.Info : NotificationPriority.Warning,
            null,
            "/Profile",
            NotificationType.General
        );

        // Zkontrolujeme achievementy za reputaci (pokud se zvýšila)
        if (amount > 0)
        {
            await _achievementService.CheckAchievementsAsync(user.Id, AchievementCategory.ReputationPoints);
        }

        await _context.SaveChangesAsync();
    }
}
