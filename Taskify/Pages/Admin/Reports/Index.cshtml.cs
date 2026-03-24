using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Taskify.Data;
using Taskify.Models;
using Taskify.Models.Enums;

namespace Taskify.Pages.Admin.Reports;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public IList<Report> Reports { get; set; } = default!;

    [BindProperty(SupportsGet = true)]
    public ReportReason? SelectedReason { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; } = "unresolved";

    public async Task OnGetAsync()
    {
        var query = _context.Reports
            .Include(r => r.Reporter)
            .Include(r => r.TaskItem)
            .OrderByDescending(r => r.CreatedAt)
            .AsQueryable();
        
        if (StatusFilter == "unresolved")
        {
            query = query.Where(r => !r.IsResolved);
        }
        else if (StatusFilter == "resolved")
        {
            query = query.Where(r => r.IsResolved);
        }
        
        if (SelectedReason.HasValue)
        {
            query = query.Where(r => r.Reason == SelectedReason.Value);
        }

        Reports = await query.ToListAsync();
    }

    public string GetReasonLabel(ReportReason reason) => reason switch
    {
        ReportReason.Podvod => "Podvod",
        ReportReason.UrážlivýObsah => "Urážlivý obsah",
        ReportReason.Spam => "Spam",
        ReportReason.Neaktuální => "Neaktuální",
        ReportReason.Jiné => "Jiné",
        _ => reason.ToString()
    };
}