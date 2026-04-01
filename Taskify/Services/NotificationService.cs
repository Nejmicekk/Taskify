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

    public async Task SendNotificationAsync(string userId, string title, string message, NotificationPriority priority)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Priority = priority,
            CreatedAt = DateTime.Now,
            IsRead = false
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        if (priority == NotificationPriority.Important)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user?.Email != null)
            {
                string emailBody = EmailTemplates.GetHtmlTemplate(
                    title, 
                    message, 
                    "Přejít do aplikace", 
                    "https://taskify.cz" // TODO: Změnit na reálnou URL až bude VPS
                );
                
                await _emailSender.SendEmailAsync(user.Email, $"Důležité upozornění: {title}", emailBody);
            }
        }
    }

    public async Task<List<Notification>> GetRecentNotificationsAsync(string userId, int count = 5)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
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
