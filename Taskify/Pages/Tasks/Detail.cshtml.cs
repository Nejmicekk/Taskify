using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Taskify.Data;
using Taskify.Models;
using Taskify.Services;

namespace Taskify.Pages.Tasks
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly INotificationService _notificationService;

        public DetailsModel(ApplicationDbContext context, UserManager<User> userManager, INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
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
            
            await _notificationService.SendNotificationAsync(
                taskItem.CreatedById, 
                "Úkol přijat", 
                "Někdo přijal váš úkol. Podívejte se na detaily.", 
                Models.Enums.NotificationPriority.Info,
                user.Id,
                targetUrl: $"/Tasks/Detail/{taskItem.Id}");

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
            
            await _notificationService.SendNotificationAsync(
                taskItem.CreatedById, 
                "Úkol čeká na schválení", 
                "Uživatel dokončil váš úkol a čeká na schválení.", 
                Models.Enums.NotificationPriority.Important,
                user.Id,
                targetUrl: $"/Tasks/Detail/{taskItem.Id}");

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
                int oldLvl = taskItem.AssignedTo.Level;
                int oldRep = taskItem.AssignedTo.Reputation;

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
                
                await _notificationService.SendNotificationAsync(
                    taskItem.AssignedToId!, 
                    "Úkol schválen!", 
                    "Autor schválil vaše řešení úkolu. Podívejte se na detaily.", 
                    Models.Enums.NotificationPriority.Success,
                    user.Id,
                    targetUrl: $"/Tasks/Detail/{taskItem.Id}");
                
                if (taskItem.AssignedTo.Level > oldLvl)
                {
                    await _notificationService.SendNotificationAsync(
                        taskItem.AssignedToId!, 
                        "Nová úroveň!", 
                        $"Gratulujeme! Dosáhli jste úrovně {taskItem.AssignedTo.Level}!", 
                        Models.Enums.NotificationPriority.Success,
                        targetUrl: $"/u/{taskItem.AssignedTo.UserName}");
                }
                
                int[] milestones = { 100, 500, 1000 };
                if (milestones.Any(m => oldRep < m && taskItem.AssignedTo.Reputation >= m))
                {
                    int reachedMilestone = milestones.First(m => taskItem.AssignedTo.Reputation >= m && oldRep < m);
                    await _notificationService.SendNotificationAsync(
                        taskItem.AssignedToId!, 
                        "Milník reputace", 
                        $"Skvělé! Dosáhli jste milníku {reachedMilestone} bodů reputace!", 
                        Models.Enums.NotificationPriority.Info,
                        targetUrl: $"/u/{taskItem.AssignedTo.UserName}");
                }
            }
            
            if (taskItem.CreatedBy != null)
            {
                taskItem.CreatedBy.Reputation += 10;
            }

            await _context.SaveChangesAsync();
            
            TempData["StatusMessage"] = $"Úkol úspěšně uzavřen!";
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostCancelAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var taskItem = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id);

            if (user == null || taskItem == null) return NotFound();
            if (taskItem.AssignedToId != user.Id || taskItem.Status != Models.Enums.TaskStatus.InProgress) return BadRequest();

            taskItem.Status = Models.Enums.TaskStatus.Open;
            taskItem.AssignedToId = null;

            await _context.SaveChangesAsync();
            
            await _notificationService.SendNotificationAsync(
                taskItem.CreatedById, 
                "Dobrovolník odstoupil", 
                "Dobrovolník zrušil svou účast na úkolu.", 
                Models.Enums.NotificationPriority.Info,
                user.Id,
                targetUrl: $"/Tasks/Detail/{taskItem.Id}");

            TempData["StatusMessage"] = "Zrušil jsi svou účast na úkolu.";
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var taskItem = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id);
            if (taskItem == null) return NotFound();

            bool isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            bool isCreator = taskItem.CreatedById == user.Id;

            if (!isAdmin && !isCreator) return Forbid();

            // Notify solver if task is deleted by someone else
            if (taskItem.AssignedToId != null && taskItem.AssignedToId != user.Id)
            {
                await _notificationService.SendNotificationAsync(
                    taskItem.AssignedToId, 
                    "Úkol byl smazán", 
                    "Úkol, na kterém jste pracovali, byl odstraněn.", 
                    Models.Enums.NotificationPriority.Warning,
                    user.Id,
                    targetUrl: "/Tasks/Index");
            }
            
            _context.Tasks.Remove(taskItem);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = isAdmin ? "Úkol byl odstraněn administrátorem." : "Tvůj úkol byl úspěšně smazán.";
            return RedirectToPage("/Tasks/Index");
        }
    }
}