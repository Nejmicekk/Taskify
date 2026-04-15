using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Taskify.Data;
using Taskify.Models;
using Taskify.Models.Enums.Notifications;
using Taskify.Services;

namespace Taskify.Pages.Tasks
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly INotificationService _notificationService;
        private readonly IAchievementService _achievementService;
        private readonly IUserService _userService;

        public DetailsModel(
            ApplicationDbContext context, 
            UserManager<User> userManager, 
            INotificationService notificationService,
            IAchievementService achievementService,
            IUserService userService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _achievementService = achievementService;
            _userService = userService;
        }

        public TaskItem TaskItem { get; set; } = null!;
        public string TimeAgo { get; set; } = string.Empty;
        public string TimeLeft { get; set; } = string.Empty;
        public bool IsMyTask { get; set; } = false;
        public bool IsExpired { get; set; } = false;
        public string CurrentUserId { get; set; } = string.Empty;
        
        public string StatusBadgeText { get; set; } = string.Empty;
        public string StatusBadgeClass { get; set; } = string.Empty;

        [BindProperty]
        public string? ApprovalComment { get; set; }

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
                    
                    // Pokud je úkol stále jen "Open", ale vypršel, vizuálně změníme status
                    if (TaskItem.Status == Models.Enums.TaskStatus.Open)
                    {
                        StatusBadgeText = "Termín vypršel";
                        StatusBadgeClass = "bg-danger";
                    }
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
                NotificationPriority.Info,
                user.Id,
                targetUrl: $"/Tasks/Detail/{taskItem.Id}",
                type: NotificationType.TaskUpdate);

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
            
            var hour = DateTime.Now.Hour;
            if (hour >= 4 && hour <= 7) await _achievementService.CheckSpecialAchievementAsync(user.Id, "Ranní ptáče");
            if (hour >= 0 && hour <= 3) await _achievementService.CheckSpecialAchievementAsync(user.Id, "Noční hrdina");
            
            await _notificationService.SendNotificationAsync(
                taskItem.CreatedById, 
                "Úkol čeká na schválení", 
                "Uživatel dokončil váš úkol a čeká na schválení.", 
                NotificationPriority.Important,
                user.Id,
                targetUrl: $"/Tasks/Detail/{taskItem.Id}",
                type: NotificationType.TaskUpdate);

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
            taskItem.AuthorComment = ApprovalComment;
            
            if (taskItem.AssignedTo != null)
            {
                taskItem.AssignedTo.TotalTasksCompleted++;
                taskItem.AssignedTo.Reputation += 20;
                
                await _userService.AddXpAsync(taskItem.AssignedToId!, taskItem.RewardPoints);
                
                await _achievementService.CheckAchievementsAsync(taskItem.AssignedToId!, Models.Enums.Achievements.AchievementCategory.TasksCompleted);
                await _achievementService.CheckAchievementsAsync(taskItem.AssignedToId!, Models.Enums.Achievements.AchievementCategory.ReputationPoints);
                
                if (taskItem.SubmittedAt.HasValue)
                {
                    var timeToComplete = taskItem.SubmittedAt.Value - taskItem.CreatedAt;
                    if (timeToComplete.TotalHours <= 24)
                    {
                        taskItem.AssignedTo.QuickCompletionsCount++;
                        await _achievementService.CheckAchievementsAsync(taskItem.AssignedToId!, Models.Enums.Achievements.AchievementCategory.CompletionSpeed);
                    }
                }
                
                var hour = DateTime.Now.Hour;
                if (hour >= 4 && hour <= 7) await _achievementService.CheckSpecialAchievementAsync(user.Id, "Ranní ptáče");
                if (hour >= 0 && hour <= 3) await _achievementService.CheckSpecialAchievementAsync(user.Id, "Noční hrdina");
                
                if (taskItem.Deadline.HasValue && taskItem.SubmittedAt.HasValue && taskItem.SubmittedAt.Value.Date == taskItem.Deadline.Value.Date)
                {
                    await _achievementService.CheckSpecialAchievementAsync(taskItem.AssignedToId!, "Na poslední chvíli");
                }

                string notificationMsg = "Autor schválil vaše řešení úkolu.";
                if (!string.IsNullOrEmpty(ApprovalComment))
                {
                    notificationMsg = $"Autor schválil vaše řešení. Vzkaz: \"{ApprovalComment}\"";
                }

                await _notificationService.SendNotificationAsync(
                    taskItem.AssignedToId!, 
                    "Úkol schválen!", 
                    notificationMsg, 
                    NotificationPriority.Success,
                    user.Id,
                    targetUrl: $"/Tasks/Detail/{taskItem.Id}",
                    type: NotificationType.TaskResult);
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
            
            if (taskItem.AssignedToId != user.Id || 
                (taskItem.Status != Models.Enums.TaskStatus.InProgress && 
                 taskItem.Status != Models.Enums.TaskStatus.WaitingForReview)) 
            {
                return BadRequest();
            }

            taskItem.Status = Models.Enums.TaskStatus.Open;
            taskItem.AssignedToId = null;
            taskItem.SubmittedAt = null; 

            await _context.SaveChangesAsync();
            
            await _notificationService.SendNotificationAsync(
                taskItem.CreatedById, 
                "Dobrovolník odstoupil", 
                "Dobrovolník zrušil svou účast na úkolu.", 
                NotificationPriority.Info,
                user.Id,
                targetUrl: $"/Tasks/Detail/{taskItem.Id}",
                type: NotificationType.TaskUpdate);

            TempData["StatusMessage"] = "Zrušil jsi svou účast na úkolu.";
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostRevokeAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var taskItem = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id);

            if (user == null || taskItem == null) return NotFound();
            if (taskItem.AssignedToId != user.Id || taskItem.Status != Models.Enums.TaskStatus.WaitingForReview) return BadRequest();

            taskItem.Status = Models.Enums.TaskStatus.InProgress;
            taskItem.SubmittedAt = null;

            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Úkol byl vrácen k dopracování.";
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

            if (taskItem.AssignedToId != null && taskItem.AssignedToId != user.Id)
            {
                await _notificationService.SendNotificationAsync(
                    taskItem.AssignedToId, 
                    "Úkol byl smazán", 
                    "Úkol, na kterém jste pracovali, byl odstraněn.", 
                    NotificationPriority.Warning,
                    user.Id,
                    targetUrl: "/Tasks/Index",
                    type: NotificationType.TaskResult);
            }
            
            _context.Tasks.Remove(taskItem);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = isAdmin ? "Úkol byl odstraněn administrátorem." : "Tvůj úkol byl úspěšně smazán.";
            return RedirectToPage("/Tasks/Index");
        }

        public async Task<IActionResult> OnPostExtendAsync(int id, DateTime newDeadline)
        {
            var user = await _userManager.GetUserAsync(User);
            var taskItem = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id);

            if (user == null || taskItem == null) return NotFound();
            if (taskItem.CreatedById != user.Id || taskItem.Status != Models.Enums.TaskStatus.Open) return BadRequest();

            if (newDeadline <= DateTime.UtcNow)
            {
                TempData["ErrorMessage"] = "Nový termín musí být v budoucnosti.";
                return RedirectToPage(new { id });
            }

            taskItem.Deadline = newDeadline;
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Termín úkolu byl úspěšně prodloužen.";
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostArchiveAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var taskItem = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id);

            if (user == null || taskItem == null) return NotFound();
            if (taskItem.CreatedById != user.Id) return BadRequest();

            taskItem.Status = Models.Enums.TaskStatus.Archived;
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Úkol byl úspěšně archivován.";
            return RedirectToPage("/Users/UserProfile", new { username = user.UserName });
        }
    }
}
