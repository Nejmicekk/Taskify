using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Taskify.Data;
using Taskify.Models;
using Taskify.Models.Enums;
using TaskStatus = Taskify.Models.Enums.TaskStatus;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    public IndexModel(ApplicationDbContext context) => _context = context;

    public IList<TaskItem> Tasks { get; set; } = default!;

    public async Task OnGetAsync()
    {
        Tasks = await _context.Tasks
            .Include(t => t.Category)
            .Include(t => t.Images)
            .Include(t => t.CreatedBy)
            .Where(t => t.Status == TaskStatus.Open)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }
}