using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Taskify.Data;
using Taskify.Models;

namespace Taskify.Pages.Tasks
{
    [AllowAnonymous]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public IndexModel(ApplicationDbContext context) => _context = context;

        public IList<TaskItem> Tasks { get; set; } = default!;
        public IList<Category> Categories { get; set; } = default!;
        
        [BindProperty(SupportsGet = true)]
        public string? SearchQuery { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public int? SelectedCategoryId { get; set; }
        
        public List<string> AvailableRegions { get; set; } = new();
        [BindProperty(SupportsGet = true)]
        public string? SelectedRegion { get; set; }
    
        public async Task OnGetAsync()
        {
            Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            
            AvailableRegions = await _context.Tasks
                .Where(t => t.Location.Region != null && t.Location.Region != "")
                .Select(t => t.Location.Region!)
                .Distinct()
                .OrderBy(r => r)
                .ToListAsync();
            
            var query = _context.Tasks
                .Include(t => t.Category)
                .Include(t => t.Images)
                .Include(t => t.Location)
                .Where(t => t.Status == Models.Enums.TaskStatus.Open)
                .AsQueryable();
            
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var searchLower = SearchQuery.ToLower();
                query = query.Where(t => t.Title.ToLower().Contains(searchLower) 
                                         || t.Description.ToLower().Contains(searchLower));
            }
            
            if (SelectedCategoryId.HasValue)
            {
                var categoryIds = await GetChildCategoryIds(SelectedCategoryId.Value);
                query = query.Where(t => categoryIds.Contains(t.CategoryId));
            }
            
            if (!string.IsNullOrWhiteSpace(SelectedRegion))
            {
                query = query.Where(t => t.Location.Region == SelectedRegion);
            }

            Tasks = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
        }
        
        private async Task<List<int>> GetChildCategoryIds(int parentId)
        {
            var ids = new List<int> { parentId };
            var children = await _context.Categories
                .Where(c => c.ParentId == parentId)
                .Select(c => c.Id)
                .ToListAsync();

            foreach (var childId in children)
            {
                ids.AddRange(await GetChildCategoryIds(childId));
            }
            return ids.Distinct().ToList();
        }
    }
}