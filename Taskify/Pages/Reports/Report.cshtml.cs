using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Taskify.Data;
using Taskify.Models;

namespace Taskify.Pages.Reports
{
    [Authorize]
    public class ReportModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public ReportModel(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
    
        public async Task<IActionResult> OnPostAsync([FromBody] ReportInput input)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            var taskItem = await _context.Tasks.FindAsync(input.TaskId);

            if (taskItem == null) return NotFound();
            
            if (taskItem.CreatedById == user.Id)
            {
                return new JsonResult(new { 
                    success = false, 
                    message = "Nemůžete nahlásit svůj vlastní úkol." 
                });
            }
            
            var oneHourAgo = DateTime.Now.AddHours(-1);
            var reportCountInLastHour = await _context.Reports
                .CountAsync(r => r.ReporterId == user.Id && r.CreatedAt > oneHourAgo);

            if (reportCountInLastHour >= 5)
            {
                return new JsonResult(new { 
                    success = false, 
                    message = "Nahlásili jste příliš mnoho příspěvků. Zkuste to prosím později." 
                });
            }
            
            var alreadyReported = await _context.Reports
                .AnyAsync(r => r.ReporterId == user.Id && r.TaskItemId == input.TaskId && !r.IsResolved);

            if (alreadyReported)
            {
                return new JsonResult(new { 
                    success = false, 
                    message = "Tento úkol jste již nahlásil. V nejbližší době bude prověřen." 
                });
            }

            // Uložení reportu
            var report = new Report
            {
                ReporterId = user.Id,
                TaskItemId = input.TaskId,
                Reason = input.Reason,
                Description = input.Description,
                CreatedAt = DateTime.Now
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, message = "Děkujeme za nahlášení." });
        }
    }

    public class ReportInput {
        public int TaskId { get; set; }
        public ReportReason Reason { get; set; }
        public string Description { get; set; }
    }   
}