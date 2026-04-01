using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Taskify.Data;
using Taskify.Models;
using Taskify.Models.Enums;

namespace Taskify.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly UserManager<User> _userManager;

    public NotificationService(
        ApplicationDbContext context, 
        IEmailSender emailSender,
        UserManager<User> userManager)
    {
        _context = context;
        _emailSender = emailSender;
        _userManager = userManager;
    }

    public async Task SendNotificationAsync(string userId, string title, string message, NotificationPriority priority, string? senderId = null, string? targetUrl = null, NotificationType type = NotificationType.General)
    {
        var notification = new Notification
        {
            UserId = userId,
            SenderId = senderId,
            Title = title,
            Message = message,
            Priority = priority,
            TargetUrl = targetUrl,
            Type = type,
            CreatedAt = DateTime.Now,
            IsRead = false
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        var user = await _userManager.FindByIdAsync(userId);
        if (user?.Email != null && user.EnableEmailNotifications)
        {
            bool shouldSendEmail = type switch
            {
                NotificationType.TaskUpdate => user.EmailOnTaskUpdates,
                NotificationType.TaskResult => user.EmailOnTaskResults,
                NotificationType.Security => user.EmailOnAccountSecurity,
                NotificationType.General => priority == NotificationPriority.Important,
                _ => false
            };

            if (shouldSendEmail)
            {
                string icon = type switch
                {
                    NotificationType.Security => "❗",
                    NotificationType.TaskUpdate => "📋",
                    NotificationType.TaskResult => priority == NotificationPriority.Success ? "✅" : "❗",
                    _ => priority == NotificationPriority.Important ? "❗" : "🔔"
                };

                string emailBody = EmailTemplates.GetHtmlTemplate(
                    title, 
                    message, 
                    "Přejít do aplikace", 
                    "https://taskify.cz",
                    icon
                );
                
                await _emailSender.SendEmailAsync(user.Email, $"Taskify: {title}", emailBody);
            }
        }
    }

    public async Task<List<Notification>> GetRecentNotificationsAsync(string userId, int count = 5)
    {
        return await GetNotificationsAsync(userId, count, 0);
    }

    public async Task<List<Notification>> GetNotificationsAsync(string userId, int count, int offset)
    {
        return await _context.Notifications
            .Include(n => n.Sender)
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip(offset)
            .Take(count)
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        return await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task MarkAsReadAsync(int notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification != null)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        var unreadNotifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
        }

        await _context.SaveChangesAsync();
    }
}
