using Taskify.Models;
using Taskify.Models.Enums;
using Taskify.Models.Enums.Notifications;

namespace Taskify.Services;

public interface INotificationService
{
    Task SendNotificationAsync(string userId, string title, string message, NotificationPriority priority, string? senderId = null, string? targetUrl = null, NotificationType type = NotificationType.General, string? iconUrl = null);
    Task MarkAllAsReadAsync(string userId);
    Task MarkAsReadAsync(int notificationId);
    Task<List<Notification>> GetRecentNotificationsAsync(string userId, int count = 5);
    Task<List<Notification>> GetNotificationsAsync(string userId, int count, int offset);
    Task<int> GetUnreadCountAsync(string userId);
}
