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
        public bool IsExpired { get; set; } = false;
        public string CurrentUserId { get; set; } = string.Empty;
        
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
            if (currentUserId != null)
            {
                CurrentUserId = currentUserId;
                if (currentUserId == TaskItem.CreatedById)
                {
                    IsMyTask = true;
                }
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
                case Models.Enums.TaskStatus.WaitingForReview:
                    StatusBadgeText = "Čeká na schválení";
                    StatusBadgeClass = "bg-info text-dark"; 
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
                    IsExpired = true;
                    TimeLeft = "Termín vypršel";
                }
            }

            return Page();
        }
        
        // uživatel přijme úkol
        public async Task<IActionResult> OnPostAcceptAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var taskItem = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id);

            if (user == null || taskItem == null) return NotFound();
            
            if (taskItem.Deadline.HasValue && taskItem.Deadline.Value <= DateTime.UtcNow)
            {
                TempData["ErrorMessage"] = "Tento úkol je již po termínu.";
                return RedirectToPage(new { id });
            }

            if (taskItem.CreatedById == user.Id || taskItem.Status != Models.Enums.TaskStatus.Open) return BadRequest();

            taskItem.Status = Models.Enums.TaskStatus.InProgress;
            taskItem.AssignedToId = user.Id;

            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = "Úspěšně jsi přijal úkol! Pusť se do toho.";
            return RedirectToPage(new { id });
        }

        // uživatel označí úkol jako dokončený
        public async Task<IActionResult> OnPostFinishAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var taskItem = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id);

            if (user == null || taskItem == null) return NotFound();
            
            if (taskItem.AssignedToId != user.Id || taskItem.Status != Models.Enums.TaskStatus.InProgress) return BadRequest();

            taskItem.Status = Models.Enums.TaskStatus.WaitingForReview;
            taskItem.SubmittedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = "Skvělá práce! Nyní počkej, až autor řešení schválí.";
            return RedirectToPage(new { id });
        }

        // autor úkolu schválí, že byl správně splněn
        public async Task<IActionResult> OnPostApproveAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            
            var taskItem = await _context.Tasks
                .Include(t => t.AssignedTo)
                .Include(t => t.CreatedBy) 
                .FirstOrDefaultAsync(t => t.Id == id);

            if (user == null || taskItem == null) return NotFound();
            
            if (taskItem.CreatedById != user.Id || taskItem.Status != Models.Enums.TaskStatus.WaitingForReview) return BadRequest();

            taskItem.Status = Models.Enums.TaskStatus.Completed;
            
            if (taskItem.AssignedTo != null)
            {
                taskItem.AssignedTo.Points += taskItem.RewardPoints;
                taskItem.AssignedTo.Reputation += 20;
                
                int currentLvl = taskItem.AssignedTo.Level;
                
                double pointsNeededForNext = 100 * Math.Pow(1.1, currentLvl - 1);
                
                while (taskItem.AssignedTo.Points >= pointsNeededForNext)
                {
                    currentLvl++;
                    
                    pointsNeededForNext = 100 * Math.Pow(1.1, currentLvl - 1);
                }
                
                taskItem.AssignedTo.Level = currentLvl;
            }
            
            if (taskItem.CreatedBy != null)
            {
                taskItem.CreatedBy.Reputation += 10;
            }

            await _context.SaveChangesAsync();
            
            TempData["StatusMessage"] = $"Úkol úspěšně uzavřen!";
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            if (!User.IsInRole("Admin")) return Forbid();

            var taskItem = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id);
            if (taskItem == null) return NotFound();

            _context.Tasks.Remove(taskItem);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Úkol byl úspěšně odstraněn administrátorem.";
            return RedirectToPage("/Tasks/Index");
        }
    }
}