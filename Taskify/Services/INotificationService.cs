using Taskify.Models;
using Taskify.Models.Enums;

namespace Taskify.Services;

public interface INotificationService
{
    Task SendNotificationAsync(string userId, string title, string message, NotificationPriority priority);
    Task MarkAllAsReadAsync(string userId);
    Task MarkAsReadAsync(int notificationId);
    Task<List<Notification>> GetRecentNotificationsAsync(string userId, int count = 5);
    Task<int> GetUnreadCountAsync(string userId);
}
