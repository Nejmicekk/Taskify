using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

    public void OnGet()
    {
    }
}