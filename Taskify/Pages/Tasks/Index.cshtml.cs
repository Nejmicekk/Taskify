using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Taskify.Data;
using Taskify.Models;

namespace Taskify.Pages.Tasks
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public IndexModel(ApplicationDbContext context) => _context = context;

        public IList<TaskItem> Tasks { get; set; } = default!;
        public IList<Category> Categories { get; set; } = default!;
    
        public async Task OnGetAsync()
        {
            Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            
            Tasks = await _context.Tasks
                .Include(t => t.Category)
                .Include(t => t.Images)
                .Where(t => t.Status == Models.Enums.TaskStatus.Open)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }
    }
}