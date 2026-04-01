using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Taskify.Data;
using Taskify.Models;
using Taskify.Services;
using TaskStatus = Taskify.Models.Enums.TaskStatus;

namespace Taskify.Pages.Admin.Reports;

[Authorize(Roles = "Admin")]
public class DetailModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly UserManager<User> _userManager;

    public DetailModel(ApplicationDbContext context, INotificationService notificationService, UserManager<User> userManager)
    {
        _context = context;
        _notificationService = notificationService;
        _userManager = userManager;
    }

    [BindProperty]
    public Report Report { get; set; } = default!;

    [BindProperty]
    public string TaskTitle { get; set; } = string.Empty;

    [BindProperty]
    public string TaskDescription { get; set; } = string.Empty;

    [BindProperty]
    public string AdminNote { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null) return NotFound();

        var report = await _context.Reports
            .Include(r => r.Reporter)
            .Include(r => r.TaskItem)
                .ThenInclude(t => t.Category)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (report == null) return NotFound();

        Report = report;
        TaskTitle = report.TaskItem.Title;
        TaskDescription = report.TaskItem.Description;
        AdminNote = report.AdminNote ?? "";

        return Page();
    }
    
    public async Task<IActionResult> OnPostDismissAsync(int id)
    {
        var report = await _context.Reports
            .Include(r => r.TaskItem)
            .FirstOrDefaultAsync(r => r.Id == id);
            
        if (report == null) return NotFound();

        report.IsResolved = true;
        report.AdminNote = string.IsNullOrEmpty(AdminNote) ? "Report byl zamítnut jako bezpředmětný." : AdminNote;

        await _context.SaveChangesAsync();
        
        var adminId = _userManager.GetUserId(User);
        await _notificationService.SendNotificationAsync(
            report.ReporterId, 
            "Report vyřešen", 
            "Vaše nahlášení bylo zpracováno. Podívejte se na výsledek.", 
            Models.Enums.NotificationPriority.Info,
            adminId,
            targetUrl: $"/Tasks/Detail/{report.TaskItem.Id}",
            type: Models.Enums.NotificationType.General);

        TempData["StatusMessage"] = "Report byl zamítnut a označen za vyřešený.";
        return RedirectToPage("./Index");
    }
    
    public async Task<IActionResult> OnPostArchiveAsync(int id)
    {
        var report = await _context.Reports
            .Include(r => r.TaskItem)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (report == null || report.TaskItem == null) return NotFound();

        report.IsResolved = true;
        report.AdminNote = string.IsNullOrEmpty(AdminNote) ? "Úkol byl smazán z důvodu porušení pravidel." : AdminNote;
        report.TaskItem.Status = TaskStatus.Archived;

        await _context.SaveChangesAsync();
        
        var adminId = _userManager.GetUserId(User);
        await _notificationService.SendNotificationAsync(
            report.TaskItem.CreatedById, 
            "Úkol smazán", 
            "Váš úkol byl smazán administrátorem z důvodu porušení pravidel.", 
            Models.Enums.NotificationPriority.Important,
            adminId,
            targetUrl: "/Tasks/Index",
            type: Models.Enums.NotificationType.TaskResult);
        
        await _notificationService.SendNotificationAsync(
            report.ReporterId, 
            "Report vyřešen", 
            "Vaše nahlášení bylo vyřešeno smazáním úkolu.", 
            Models.Enums.NotificationPriority.Info,
            adminId,
            targetUrl: "/Tasks/Index",
            type: Models.Enums.NotificationType.TaskResult);

        TempData["StatusMessage"] = "Úkol byl smazán a report uzavřen.";
        return RedirectToPage("./Index");
    }
    
    public async Task<IActionResult> OnPostUpdateAndApproveAsync(int id)
    {
        var report = await _context.Reports
            .Include(r => r.TaskItem)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (report == null || report.TaskItem == null) return NotFound();
        
        report.TaskItem.Title = TaskTitle;
        report.TaskItem.Description = TaskDescription;

        report.IsResolved = true;
        report.AdminNote = string.IsNullOrEmpty(AdminNote) ? "Úkol byl upraven administrátorem a schválen." : AdminNote;

        await _context.SaveChangesAsync();
        
        var adminId = _userManager.GetUserId(User);
        await _notificationService.SendNotificationAsync(
            report.TaskItem.CreatedById, 
            "Úkol upraven administrátorem", 
            "Váš úkol byl upraven administrátorem.", 
            Models.Enums.NotificationPriority.Warning,
            adminId,
            targetUrl: $"/Tasks/Detail/{report.TaskItem.Id}",
            type: Models.Enums.NotificationType.TaskResult);
        
        await _notificationService.SendNotificationAsync(
            report.ReporterId, 
            "Report vyřešen", 
            "Vaše nahlášení bylo vyřešeno úpravou úkolu.", 
            Models.Enums.NotificationPriority.Info,
            adminId,
            targetUrl: $"/Tasks/Detail/{report.TaskItem.Id}",
            type: Models.Enums.NotificationType.TaskResult);

        TempData["StatusMessage"] = "Úkol byl upraven a report byl úspěšně uzavřen.";
        return RedirectToPage("./Index");
    }
}