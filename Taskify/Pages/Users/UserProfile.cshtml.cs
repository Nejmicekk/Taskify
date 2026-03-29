using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Taskify.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Taskify.Pages.Users
{
    [AllowAnonymous]
    public class UserProfileModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly Data.ApplicationDbContext _context;

        public UserProfileModel(UserManager<User> userManager, Data.ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public User? DisplayedUser { get; set; }
        public string UserRole { get; set; } = "Taskify Member";
        public int NextLevelPoints { get; set; }
        public bool IsMe { get; set; }
        public bool IsLockedOut { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        
        public IList<TaskItem> CreatedTasks { get; set; } = new List<TaskItem>();
        public IList<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();

        public async Task<IActionResult> OnGetAsync(string username)
        {
            DisplayedUser = await _userManager.Users
                .FirstOrDefaultAsync(u => u.UserName == username);

            if (DisplayedUser == null)
            {
                return Page();
            }

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
            
            CreatedTasks = await _context.Tasks
                .Include(t => t.Category)
                .Include(t => t.Images)
                .Include(t => t.Location)
                .Where(t => t.CreatedById == DisplayedUser.Id)
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .ToListAsync();
            
            if (IsMe)
            {
                AssignedTasks = await _context.Tasks
                    .Include(t => t.Category)
                    .Include(t => t.Images)
                    .Include(t => t.Location)
                    .Where(t => t.AssignedToId == DisplayedUser.Id && t.Status != Models.Enums.TaskStatus.Completed)
                    .OrderBy(t => t.Deadline ?? DateTime.MaxValue)
                    .Take(5)
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
                
                string timeMsg = days.Value == 0 ? "trvale" : $"na {days.Value} dní";
                TempData["StatusMessage"] = $"Účet uživatele {userToLock.UserName} byl zablokován {timeMsg}.";
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(userToLock, null);
                TempData["StatusMessage"] = $"Účet uživatele {userToLock.UserName} byl odemčen.";
            }

            return RedirectToPage(new { username = userToLock.UserName });
        }
    }
}