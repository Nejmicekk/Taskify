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

    public async Task OnGetAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (userId != null)
        {
            Notifications = await _notificationService.GetRecentNotificationsAsync(userId, 50);
        }
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
