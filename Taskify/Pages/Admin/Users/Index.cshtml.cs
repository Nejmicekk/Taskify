using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Taskify.Data;
using Taskify.Models;
using Taskify.Models.Enums;

namespace Taskify.Pages.Admin.Users;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly UserManager<User> _userManager;
    private readonly ApplicationDbContext _context;

    public IndexModel(UserManager<User> userManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public IList<UserViewModel> Users { get; set; } = default!;

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public string RoleFilter { get; set; } = "all";

    [BindProperty(SupportsGet = true)]
    public string StatusFilter { get; set; } = "all";

    [BindProperty(SupportsGet = true)]
    public string SortOrder { get; set; } = "default";

    public class UserViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsLockedOut { get; set; }
        public int Reputation { get; set; }
        public int Level { get; set; }
    }

    public async Task OnGetAsync()
    {
        var usersQuery = _userManager.Users.AsQueryable();

        // 1. Vyhledávání
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            usersQuery = usersQuery.Where(u => 
                (u.UserName != null && u.UserName.Contains(SearchTerm)) || 
                (u.Email != null && u.Email.Contains(SearchTerm)) ||
                u.Name.Contains(SearchTerm));
        }

        // 2. Načtení všech (IdentityUserManager nepodporuje komplexní joiny na role v EF bez manuálního SQL)
        var allUsers = await usersQuery.ToListAsync();
        var now = DateTimeOffset.UtcNow;
        
        var viewModels = new List<UserViewModel>();

        foreach (var user in allUsers)
        {
            bool isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            bool isLocked = user.LockoutEnd != null && user.LockoutEnd > now;
            
            // Filtrace v paměti (spolehlivější pro Identity a SQLite)
            if (RoleFilter == "admin" && !isAdmin) continue;
            if (RoleFilter == "user" && isAdmin) continue;
            
            if (StatusFilter == "locked" && !isLocked) continue;
            if (StatusFilter == "active" && isLocked) continue;

            viewModels.Add(new UserViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Name = user.Name,
                ProfilePictureUrl = user.ProfilePictureUrl,
                IsAdmin = isAdmin,
                IsLockedOut = isLocked,
                Reputation = user.Reputation,
                Level = user.Level
            });
        }

        // 3. Řazení
        Users = SortOrder switch
        {
            "rep_desc" => viewModels.OrderByDescending(u => u.Reputation).ToList(),
            "rep_asc" => viewModels.OrderBy(u => u.Reputation).ToList(),
            "lvl_desc" => viewModels.OrderByDescending(u => u.Level).ToList(),
            "lvl_asc" => viewModels.OrderBy(u => u.Level).ToList(),
            _ => viewModels.OrderBy(u => u.UserName).ToList()
        };
    }

    public async Task<IActionResult> OnPostToggleAdminAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser?.Id == userId)
        {
            TempData["ErrorMessage"] = "Nemůžete odebrat administrátorská práva sami sobě.";
            return RedirectToPage();
        }

        if (await _userManager.IsInRoleAsync(user, "Admin"))
        {
            await _userManager.RemoveFromRoleAsync(user, "Admin");
            TempData["StatusMessage"] = $"Role Admin byla odebrána uživateli {user.UserName}.";
        }
        else
        {
            await _userManager.AddToRoleAsync(user, "Admin");
            TempData["StatusMessage"] = $"Uživatel {user.UserName} byl jmenován administrátorem.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleLockAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser?.Id == userId)
        {
            TempData["ErrorMessage"] = "Nemůžete zamknout svůj vlastní účet.";
            return RedirectToPage();
        }

        var isLocked = await _userManager.IsLockedOutAsync(user);

        if (isLocked)
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
            TempData["StatusMessage"] = $"Účet uživatele {user.UserName} byl odemčen.";
        }
        else
        {
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            await UnassignTasksFromUser(userId);
            TempData["StatusMessage"] = $"Účet uživatele {user.UserName} byl zablokován a jeho úkoly, na kterých pracoval jsou opět aktivní pro další uživatele.";
        }

        return RedirectToPage();
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