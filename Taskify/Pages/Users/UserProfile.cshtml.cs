using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Taskify.Models;
using Taskify.Models.Enums.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TaskStatus = Taskify.Models.Enums.TaskStatus;

namespace Taskify.Pages.Users
{
    [AllowAnonymous]
    public class UserProfileModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly Data.ApplicationDbContext _context;
        private readonly Taskify.Services.INotificationService _notificationService;

        public UserProfileModel(UserManager<User> userManager, Data.ApplicationDbContext context, Taskify.Services.INotificationService notificationService)
        {
            _userManager = userManager;
            _context = context;
            _notificationService = notificationService;
        }

        public User? DisplayedUser { get; set; }
        public List<UserAchievement> TopAchievements { get; set; } = new List<UserAchievement>();
        public string UserRole { get; set; } = "Taskify Member";
        public int NextLevelPoints { get; set; }
        public bool IsMe { get; set; }
        public bool IsLockedOut { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        
        public IList<TaskItem> CreatedTasks { get; set; } = new List<TaskItem>();
        public IList<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();
        public IList<TaskItem> ExpiredTasks { get; set; } = new List<TaskItem>();
        public IList<TaskItem> ManagedTasks { get; set; } = new List<TaskItem>();

        public async Task<IActionResult> OnGetAsync(string username)
        {
            DisplayedUser = await _userManager.Users
                .Include(u => u.Achievements)
                    .ThenInclude(ua => ua.Achievement)
                .FirstOrDefaultAsync(u => u.UserName == username);

            if (DisplayedUser == null)
            {
                return Page();
            }

            // Výběr 6 nejvzácnějších získaných achievementů
            TopAchievements = DisplayedUser.Achievements
                .Where(ua => ua.IsUnlocked)
                .OrderByDescending(ua => ua.Achievement!.Rarity)
                .ThenByDescending(ua => ua.EarnedAt)
                .Take(6)
                .ToList();

            IsLockedOut = await _userManager.IsLockedOutAsync(DisplayedUser);
            LockoutEnd = DisplayedUser.LockoutEnd;
            
            NextLevelPoints = (int)(100 * Math.Pow(1.1, DisplayedUser.Level - 1));
            
            var roles = await _userManager.GetRolesAsync(DisplayedUser);
            var mainRole = roles.FirstOrDefault() ?? "Member";
            UserRole = $"Taskify {mainRole}";
            
            var currentUser = await _userManager.GetUserAsync(User);
            
            bool isTargetAdmin = roles.Contains("Admin");
            bool isVisitorAdmin = currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Admin");
            
            if (isTargetAdmin && !isVisitorAdmin)
            {
                DisplayedUser = null;
                return Page();
            }

            IsMe = currentUser != null && currentUser.Id == DisplayedUser.Id;
            
            // Aktivní vytvořené úkoly (nejsou po termínu)
            CreatedTasks = await _context.Tasks
                .Include(t => t.Category)
                .Include(t => t.Images)
                .Include(t => t.Location)
                .Where(t => t.CreatedById == DisplayedUser.Id && 
                           t.Status == Models.Enums.TaskStatus.Open && 
                           (t.Deadline == null || t.Deadline > DateTime.UtcNow))
                .OrderByDescending(t => t.CreatedAt)
                .Take(3)
                .ToListAsync();

            // Pokud je to můj profil, chci vidět i ty, co vypršely a nikdo je nepřijal
            if (IsMe)
            {
                ExpiredTasks = await _context.Tasks
                    .Include(t => t.Category)
                    .Include(t => t.Images)
                    .Include(t => t.Location)
                    .Where(t => t.CreatedById == DisplayedUser.Id && 
                               t.Status == Models.Enums.TaskStatus.Open && 
                               t.Deadline != null && t.Deadline <= DateTime.UtcNow)
                    .OrderByDescending(t => t.Deadline)
                    .ToListAsync();

                // Just-in-Time Notifikace pro vypršené úkoly
                foreach (var task in ExpiredTasks)
                {
                    string targetUrl = $"/Tasks/Detail/{task.Id}";
                    // Zkontrolujeme, zda uživatel už o tomto konkrétním úkolu dostal notifikaci
                    bool alreadyNotified = await _context.Notifications
                        .AnyAsync(n => n.UserId == currentUser!.Id && n.TargetUrl == targetUrl && n.Title == "Termín úkolu vypršel");

                    if (!alreadyNotified)
                    {
                        await _notificationService.SendNotificationAsync(
                            currentUser!.Id,
                            "Termín úkolu vypršel",
                            $"U vašeho úkolu \"{task.Title}\" vypršel termín a nikdo jej nepřijal. Prosím, prodlužte jej nebo archivujte.",
                            NotificationPriority.Warning,
                            targetUrl: targetUrl,
                            type: NotificationType.TaskResult);
                    }
                }
            }
            
            if (IsMe)
            {
                ManagedTasks = await _context.Tasks
                    .Include(t => t.Category)
                    .Include(t => t.Images)
                    .Include(t => t.Location)
                    .Where(t => t.CreatedById == DisplayedUser.Id && 
                                (t.Deadline == null || t.Deadline > DateTime.UtcNow) && t.Status != TaskStatus.Completed)
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(3)
                    .ToListAsync();
                
                AssignedTasks = await _context.Tasks
                    .Include(t => t.Category)
                    .Include(t => t.Images)
                    .Include(t => t.Location)
                    .Where(t => t.AssignedToId == DisplayedUser.Id && t.Status != TaskStatus.Completed)
                    .OrderBy(t => t.Deadline ?? DateTime.MaxValue)
                    .Take(3)
                    .ToListAsync();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostToggleLockAsync(string userId, int? days)
        {
            if (!User.IsInRole("Admin")) return Forbid();

            var userToLock = await _userManager.FindByIdAsync(userId);
            if (userToLock == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Id == userId)
            {
                TempData["ErrorMessage"] = "Nemůžete zamknout svůj vlastní účet.";
                return RedirectToPage(new { username = userToLock.UserName });
            }

            if (days.HasValue)
            {
                DateTimeOffset? end = days.Value == 0 ? DateTimeOffset.MaxValue : DateTimeOffset.UtcNow.AddDays(days.Value);
                await _userManager.SetLockoutEndDateAsync(userToLock, end);
                
                await UnassignTasksFromUser(userId);
                
                string timeMsg = days.Value == 0 ? "trvale" : $"na {days.Value} dní";
                
                await _notificationService.SendNotificationAsync(
                    userId, 
                    "Váš účet byl pozastaven", 
                    "Váš účet byl dočasně zablokován. Podívejte se na detaily v nastavení.", 
                    NotificationPriority.Important,
                    currentUser?.Id,
                    targetUrl: "/Identity/Account/Manage/Index",
                    type: NotificationType.Security);
                
                TempData["StatusMessage"] = $"Účet uživatele {userToLock.UserName} byl zablokován {timeMsg}.";
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(userToLock, null);
                TempData["StatusMessage"] = $"Účet uživatele {userToLock.UserName} byl odemčen.";
            }

            return RedirectToPage(new { username = userToLock.UserName });
        }
        
        private async Task UnassignTasksFromUser(string userId)
        {
            var tasksToRelease = await _context.Tasks
                .Where(t => t.AssignedToId == userId && t.Status == Models.Enums.TaskStatus.InProgress)
                .ToListAsync();

            foreach (var task in tasksToRelease)
            {
                task.AssignedToId = null;
                task.Status = Models.Enums.TaskStatus.Open;
            }

            if (tasksToRelease.Any())
            {
                await _context.SaveChangesAsync();
            }
        }
    }
}
