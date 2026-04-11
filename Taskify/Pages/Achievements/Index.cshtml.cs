using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Taskify.Data;
using Taskify.Models;
using Taskify.Models.Enums.Achievements;

namespace Taskify.Pages.Achievements;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;

    public IndexModel(ApplicationDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public User? TargetUser { get; set; }
    public List<AchievementGroup> AchievementGroups { get; set; } = new();
    public int UnlockedCount { get; set; }
    public int TotalCount { get; set; }
    public double TotalProgressPercent { get; set; }
    public bool IsMe { get; set; }

    public class AchievementGroup
    {
        public AchievementCategory Category { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public List<AchievementViewModel> Achievements { get; set; } = new();
    }

    public class AchievementViewModel
    {
        public Achievement Achievement { get; set; } = default!;
        public UserAchievement? UserProgress { get; set; }
        public bool IsUnlocked => UserProgress?.IsUnlocked ?? false;
    }

    public async Task<IActionResult> OnGetAsync(string? username)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        
        if (string.IsNullOrEmpty(username))
        {
            TargetUser = currentUser;
            IsMe = true;
        }
        else
        {
            TargetUser = await _userManager.Users
                .Include(u => u.Achievements)
                .FirstOrDefaultAsync(u => u.UserName == username);
            IsMe = currentUser?.Id == TargetUser?.Id;
        }

        if (TargetUser == null) return NotFound();

        // Načteme všechny achievementy
        var allAchievements = await _context.Achievements
            .OrderBy(a => a.TargetValue)
            .ToListAsync();

        // Načteme progres uživatele
        var userAchievements = await _context.UserAchievements
            .Where(ua => ua.UserId == TargetUser.Id)
            .ToListAsync();

        TotalCount = allAchievements.Count;
        UnlockedCount = userAchievements.Count(ua => ua.IsUnlocked);
        TotalProgressPercent = TotalCount > 0 ? (double)UnlockedCount / TotalCount * 100 : 0;

        // Seskupíme podle kategorií
        var categories = Enum.GetValues<AchievementCategory>();
        foreach (AchievementCategory cat in categories)
        {
            var group = new AchievementGroup
            {
                Category = cat,
                DisplayName = GetCategoryDisplayName(cat)
            };

            var catAchievements = allAchievements.Where(a => a.Category == cat).ToList();
            foreach (var ach in catAchievements)
            {
                group.Achievements.Add(new AchievementViewModel
                {
                    Achievement = ach,
                    UserProgress = userAchievements.FirstOrDefault(ua => ua.AchievementId == ach.Id)
                });
            }

            if (group.Achievements.Any())
            {
                AchievementGroups.Add(group);
            }
        }

        return Page();
    }

    private string GetCategoryDisplayName(AchievementCategory category)
    {
        return category switch
        {
            AchievementCategory.TasksCompleted => "Splněné úkoly",
            AchievementCategory.CompletionSpeed => "Expresní plnění",
            AchievementCategory.WeeklyStreak => "Týdenní streaky",
            AchievementCategory.TasksCreated => "Tvorba úkolů",
            AchievementCategory.LevelReached => "Úrovně",
            AchievementCategory.ReputationPoints => "Reputace",
            AchievementCategory.Special => "Tajné",
            _ => category.ToString()
        };
    }
}
