using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Taskify.Data;

namespace Taskify.Pages.Admin;

[Authorize(Roles = "Admin")]
public class AdminDashboardModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public AdminDashboardModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public int UnresolvedReportsCount { get; set; }
    public int TotalTasksCount { get; set; }
    public int TotalUsersCount { get; set; }

    public async Task OnGetAsync()
    {
        UnresolvedReportsCount = await _context.Reports.CountAsync(r => !r.IsResolved);
        TotalTasksCount = await _context.Tasks.CountAsync();
        TotalUsersCount = await _context.Users.CountAsync();
    }
}