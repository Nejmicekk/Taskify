using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Taskify.Data;
using Taskify.Models;
using Taskify.Models.Enums;
using TaskStatus = Taskify.Models.Enums.TaskStatus;

namespace Taskify.Pages.Statistics
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int TotalCompletedTasks { get; set; }
        public int TotalCreatedTasks { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalDistributedXP { get; set; }
        
        public List<RegionStat> Top3Regions { get; set; } = new();
        public List<CategoryStat> Top3Categories { get; set; } = new();
        public List<CategoryStat> CompletedByCategory { get; set; } = new();
        
        public int[] CompletionByHour { get; set; } = new int[24];
        public int[] WeeklyActivity { get; set; } = new int[7]; // 0 = Pondělí, 6 = Neděle
        public List<TrendStat> TasksTrend { get; set; } = new();
        public List<StatusStat> StatusDistribution { get; set; } = new();
        public List<UserStat> TopPerformers { get; set; } = new();
        public List<UserStat> TopCreators { get; set; } = new();

        public class RegionStat { public string Region { get; set; } = string.Empty; public int Count { get; set; } }
        public class CategoryStat { public string Name { get; set; } = string.Empty; public int Count { get; set; } }
        public class TrendStat { public string Date { get; set; } = string.Empty; public int Count { get; set; } }
        public class StatusStat { public string Status { get; set; } = string.Empty; public int Count { get; set; } }
        public class UserStat { 
            public string UserName { get; set; } = string.Empty; 
            public string? ProfilePictureUrl { get; set; } 
            public int Count { get; set; } 
        }

        private string GetStatusLabel(TaskStatus status) => status switch
        {
            TaskStatus.PendingApproval => "Čeká na schválení",
            TaskStatus.Open => "Hledá dobrovolníka",
            TaskStatus.InProgress => "V procesu",
            TaskStatus.WaitingForReview => "Čeká na kontrolu",
            TaskStatus.Completed => "Dokončený",
            TaskStatus.Archived => "Archivovaný",
            _ => status.ToString()
        };

        public async Task OnGetAsync()
        {
            TotalCreatedTasks = await _context.Tasks.CountAsync();
            TotalCompletedTasks = await _context.Tasks.CountAsync(t => t.Status == Models.Enums.TaskStatus.Completed);
            ActiveUsers = await _context.Users.CountAsync();
            
            TotalDistributedXP = await _context.Tasks
                .Where(t => t.Status == Models.Enums.TaskStatus.Completed)
                .SumAsync(t => t.RewardPoints);

            
            Top3Regions = await _context.Tasks
                .Where(t => !string.IsNullOrEmpty(t.Location.Region))
                .GroupBy(t => t.Location.Region)
                .Select(g => new RegionStat { Region = g.Key!, Count = g.Count() })
                .OrderByDescending(r => r.Count).Take(3).ToListAsync();
            
            Top3Categories = await _context.Tasks.Include(t => t.Category)
                .GroupBy(t => t.Category!.Name)
                .Select(g => new CategoryStat { Name = g.Key, Count = g.Count() })
                .OrderByDescending(c => c.Count).Take(3).ToListAsync();
            
            CompletedByCategory = await _context.Tasks
                .Where(t => t.Status == Models.Enums.TaskStatus.Completed).Include(t => t.Category)
                .GroupBy(t => t.Category!.Name)
                .Select(g => new CategoryStat { Name = g.Key, Count = g.Count() })
                .OrderByDescending(c => c.Count).ToListAsync();
            
            var submissions = await _context.Tasks.Where(t => t.SubmittedAt.HasValue)
                .Select(t => t.SubmittedAt!.Value).ToListAsync();
            
            foreach (var sub in submissions) {
                CompletionByHour[sub.Hour]++;
                // převod DayOfWeek (Sunday=0) na 0=Po, 6=Ne
                int dayIndex = ((int)sub.DayOfWeek + 6) % 7;
                WeeklyActivity[dayIndex]++;
            }
            
            var startDate = DateTime.UtcNow.Date.AddDays(-9);
            var trendData = await _context.Tasks
                .Where(t => t.CreatedAt >= startDate)
                .GroupBy(t => t.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(g => g.Date).ToListAsync();

            for (int i = 0; i < 10; i++)
            {
                var date = startDate.AddDays(i);
                var match = trendData.FirstOrDefault(d => d.Date == date);
                TasksTrend.Add(new TrendStat { Date = date.ToString("dd.MM."), Count = match?.Count ?? 0 });
            }
            
            TopPerformers = await _context.Users
                .Select(u => new UserStat {
                    UserName = u.UserName!,
                    ProfilePictureUrl = u.ProfilePictureUrl,
                    Count = _context.Tasks.Count(t => t.AssignedToId == u.Id && t.Status == Models.Enums.TaskStatus.Completed)
                })
                .OrderByDescending(u => u.Count)
                .Take(5)
                .ToListAsync();
            
            TopCreators = await _context.Users
                .Select(u => new UserStat {
                    UserName = u.UserName!,
                    ProfilePictureUrl = u.ProfilePictureUrl,
                    Count = _context.Tasks.Count(t => t.CreatedById == u.Id)
                })
                .OrderByDescending(u => u.Count)
                .Take(5)
                .ToListAsync();
            
            var statusGroups = await _context.Tasks
                .GroupBy(t => t.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            StatusDistribution = statusGroups.Select(g => new StatusStat { 
                Status = GetStatusLabel(g.Status), 
                Count = g.Count 
            }).OrderByDescending(s => s.Count).ToList();
        }
    }
}
