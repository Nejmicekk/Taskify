using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Taskify.Models;
using Taskify.Services;

namespace Taskify.Pages.Notifications;

[Authorize]
public class IndexModel : PageModel
{
    private readonly INotificationService _notificationService;
    private readonly UserManager<User> _userManager;

    public IndexModel(INotificationService notificationService, UserManager<User> userManager)
    {
        _notificationService = notificationService;
        _userManager = userManager;
    }

    public List<Notification> Notifications { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string Filter { get; set; } = "all";

    public async Task OnGetAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (userId != null)
        {
            var notifications = await _notificationService.GetRecentNotificationsAsync(userId, 100);
            if (Filter == "unread")
            {
                Notifications = notifications.Where(n => !n.IsRead).ToList();
            }
            else
            {
                Notifications = notifications;
            }
        }
    }

    public async Task<PartialViewResult> OnGetLoadMoreAsync(int offset, int count = 5, string filter = "all")
    {
        var userId = _userManager.GetUserId(User);
        var notifications = await _notificationService.GetNotificationsAsync(userId, count, offset);
        
        if (filter == "unread")
        {
            notifications = notifications.Where(n => !n.IsRead).ToList();
        }
        
        return Partial("_NotificationItemsPartial", notifications);
    }

    public async Task<IActionResult> OnPostMarkAsReadAsync(int id)
    {
        await _notificationService.MarkAsReadAsync(id);
        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostMarkAllAsReadAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (userId != null)
        {
            await _notificationService.MarkAllAsReadAsync(userId);
            return new JsonResult(new { success = true });
        }
        return new JsonResult(new { success = false });
    }
}
