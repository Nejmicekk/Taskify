using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Taskify.Data;
using Taskify.Models;

namespace Taskify.Pages.Tasks;

[Authorize]
public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;

    public EditModel(ApplicationDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [BindProperty]
    public EditInputModel Input { get; set; } = new();
    
    public List<Category> Categories { get; set; } = new();
    
    public TaskItem DisplayTask { get; set; } = null!;
    public string? ReturnUrl { get; set; }
    

    public class EditInputModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nadpis je povinný.")]
        [StringLength(25, ErrorMessage = "Nadpis může mít maximálně 25 znaků.")]
        [Display(Name = "Název úkolu")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Popis je povinný, aby ostatní věděli, co přesně dělat.")]
        [StringLength(200, ErrorMessage = "Popis může mít maximálně 200 znaků.")]
        [Display(Name = "Popis problému")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vyberte kategorii.")]
        [Display(Name = "Kategorie")]
        public int CategoryId { get; set; }

        [Display(Name = "Termín splnění (volitelné)")]
        public DateTime? Deadline { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();
        
        var taskItem = await _context.Tasks
            .Include(t => t.Images)
            .Include(t => t.Location)
            .FirstOrDefaultAsync(t => t.Id == id);
            
        if (taskItem == null) return NotFound();

        bool isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

        if (taskItem.CreatedById != user.Id && !isAdmin) return Forbid();
        
        if (!isAdmin && taskItem.Status != Models.Enums.TaskStatus.Open)
        {
            TempData["ErrorMessage"] = "Tento úkol již nelze upravovat, protože na něm někdo pracuje nebo je uzavřen.";
            return RedirectToPage("/Tasks/Detail", new { id = taskItem.Id });
        }
        
        DisplayTask = taskItem;

        Input = new EditInputModel
        {
            Id = taskItem.Id,
            Title = taskItem.Title,
            Description = taskItem.Description,
            CategoryId = taskItem.CategoryId,
            Deadline = taskItem.Deadline?.ToLocalTime()
        };

        await LoadCategoriesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        if (!ModelState.IsValid)
        {
            DisplayTask = await _context.Tasks
                .Include(t => t.Images)
                .Include(t => t.Location)
                .FirstOrDefaultAsync(t => t.Id == Input.Id);
                
            await LoadCategoriesAsync();
            return Page();
        }

        var taskToUpdate = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == Input.Id);
        if (taskToUpdate == null) return NotFound();

        bool isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

        if (taskToUpdate.CreatedById != user.Id && !isAdmin)
        {
            return Forbid();
        }

        if (!isAdmin && taskToUpdate.Status != Models.Enums.TaskStatus.Open)
        {
            return Forbid();
        }

        taskToUpdate.Title = Input.Title;
        taskToUpdate.Description = Input.Description;
        taskToUpdate.CategoryId = Input.CategoryId;
        taskToUpdate.Deadline = Input.Deadline?.ToUniversalTime();

        await _context.SaveChangesAsync();
        TempData["StatusMessage"] = "Úkol byl úspěšně upraven!";
        
        return RedirectToPage("/Tasks/Detail", new { id = Input.Id });
    }
    
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var taskToDelete = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id);
        if (taskToDelete == null) return NotFound();
        
        bool isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
        bool isCreator = taskToDelete.CreatedById == user.Id;

        if (!isAdmin && !isCreator)
        {
            return Forbid();
        }

        // Pokud úkol někdo plní (InProgress) nebo čeká na kontrolu, budeme chtít v budoucnu poslat notifikaci
        // TODO: Až bude notifikační systém, poslat info uživateli (AssignedToId), že úkol byl autorem smazán.
        
        _context.Tasks.Remove(taskToDelete);
        await _context.SaveChangesAsync();
        
        TempData["StatusMessage"] = isAdmin ? "Úkol byl úspěšně odstraněn administrátorem." : "Tvůj úkol byl úspěšně smazán.";

        if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
        {
            return LocalRedirect(ReturnUrl);
        }
        
        return RedirectToPage("/Tasks/Index");
    }

    private async Task LoadCategoriesAsync()
    {
        Categories = await _context.Categories.ToListAsync();
    }
}