using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Taskify.Data;
using Taskify.Models;
using TaskStatus = Taskify.Models.Enums.TaskStatus;

namespace Taskify.Pages.Admin.Tasks;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public IList<TaskItem> Tasks { get; set; } = default!;

    [BindProperty(SupportsGet = true)]
    public TaskStatus? StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchQuery { get; set; }

    public async Task OnGetAsync()
    {
        var query = _context.Tasks
            .Include(t => t.CreatedBy)
            .Include(t => t.Category)
            .OrderByDescending(t => t.CreatedAt)
            .AsQueryable();

        if (StatusFilter.HasValue)
        {
            query = query.Where(t => t.Status == StatusFilter.Value);
        }

        if (!string.IsNullOrEmpty(SearchQuery))
        {
            query = query.Where(t => t.Title.Contains(SearchQuery) || t.Description.Contains(SearchQuery));
        }

        Tasks = await query.ToListAsync();
    }

    public string GetStatusLabel(TaskStatus status) => status switch
    {
        TaskStatus.PendingApproval => "Čeká na schválení",
        TaskStatus.Open => "Hledá dobrovolníka",
        TaskStatus.InProgress => "V procesu",
        TaskStatus.WaitingForReview => "Čeká na kontrolu",
        TaskStatus.Completed => "Dokončený",
        TaskStatus.Archived => "Archivovaný",
        _ => status.ToString()
    };

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task != null)
        {
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = "Úkol byl úspěšně smazán.";
        }
        return RedirectToPage();
    }
}