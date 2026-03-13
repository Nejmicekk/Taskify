using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Taskify.Data;
using Taskify.Models;

namespace Taskify.Pages.Tasks
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public DetailsModel(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public TaskItem TaskItem { get; set; } = null!;
        public string TimeAgo { get; set; } = string.Empty;
        public string TimeLeft { get; set; } = string.Empty;
        public bool IsMyTask { get; set; } = false;
        
        public string StatusBadgeText { get; set; } = string.Empty;
        public string StatusBadgeClass { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var taskitem = await _context.Tasks
                .Include(t => t.Images)
                .Include(t => t.Category)
                .Include(t => t.CreatedBy)
                .Include(t => t.AssignedTo)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (taskitem == null)
            {
                return NotFound();
            }

            TaskItem = taskitem;
            
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId != null && currentUserId == TaskItem.CreatedById)
            {
                IsMyTask = true;
            }
            
            switch (TaskItem.Status)
            {
                case Models.Enums.TaskStatus.Open:
                    StatusBadgeText = "Hledá se dobrovolník";
                    StatusBadgeClass = "bg-success";
                    break;
                case Models.Enums.TaskStatus.InProgress:
                    StatusBadgeText = "Řeší se";
                    StatusBadgeClass = "bg-warning text-dark";
                    break;
                case Models.Enums.TaskStatus.Completed:
                    StatusBadgeText = "Hotovo";
                    StatusBadgeClass = "bg-primary";
                    break;
                case Models.Enums.TaskStatus.Archived:
                    StatusBadgeText = "Archivováno";
                    StatusBadgeClass = "bg-secondary";
                    break;
                default:
                    StatusBadgeText = "Neznámý stav";
                    StatusBadgeClass = "bg-dark";
                    break;
            }
            
            var timeSpanCreated = DateTime.UtcNow - TaskItem.CreatedAt;
            if (timeSpanCreated.TotalDays >= 1)
                TimeAgo = $"před {(int)timeSpanCreated.TotalDays} dny";
            else if (timeSpanCreated.TotalHours >= 1)
                TimeAgo = $"před {(int)timeSpanCreated.TotalHours} hodinami";
            else
                TimeAgo = "před chvílí";
            
            if (TaskItem.Deadline.HasValue)
            {
                var timeSpanDeadline = TaskItem.Deadline.Value - DateTime.UtcNow;
                if (timeSpanDeadline.TotalSeconds > 0)
                {
                    int days = (int)timeSpanDeadline.TotalDays;
                    int hours = timeSpanDeadline.Hours;
                    
                    if (days > 0)
                        TimeLeft = $"za {days} dny a {hours} hodin";
                    else
                        TimeLeft = $"za {hours} hodin";
                }
                else
                {
                    TimeLeft = "Termín vypršel";
                }
            }

            return Page();
        }
    }
}