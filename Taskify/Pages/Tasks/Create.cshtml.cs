using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Taskify.Data;
using Taskify.Models;

namespace Taskify.Pages.Tasks;

[Authorize]
public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;

    public CreateModel(ApplicationDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [BindProperty]
    public TaskInputModel Input { get; set; } = new();
    
    public List<Category> AllCategories { get; set; } = new();

    public class TaskInputModel
    {
        [Required(ErrorMessage = "Nadpis je povinný.")]
        [StringLength(100, ErrorMessage = "Nadpis může mít maximálně 100 znaků.")]
        [Display(Name = "Název úkolu")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Popis je povinný, aby ostatní věděli, co přesně dělat.")]
        [Display(Name = "Popis problému")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vyberte kategorii.")]
        [Display(Name = "Kategorie")]
        public int CategoryId { get; set; }

        [Display(Name = "Termín splnění (volitelné)")]
        public DateTime? Deadline { get; set; }
        
        [Required(ErrorMessage = "Musíte vybrat místo na mapě!")]
        [Range(-90, 90, ErrorMessage = "Neplatná zeměpisná šířka.")]
        public double LocationLatitude { get; set; }

        [Required(ErrorMessage = "Musíte vybrat místo na mapě!")]
        [Range(-180, 180, ErrorMessage = "Neplatná zeměpisná délka.")]
        public double LocationLongitude { get; set; }
        
        public string? FullAddress { get; set; }
        public string? City { get; set; }
    }

    public async Task OnGetAsync()
    {
        await LoadCategoriesAsync();
    }

    public async Task<IActionResult> OnPostAsync(List<IFormFile> imageUploads)
    {
        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync();
            return Page();
        }

        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null) return NotFound("Uživatel nebyl nalezen.");
        
        var newTask = new TaskItem
        {
            Title = Input.Title,
            Description = Input.Description,
            CategoryId = Input.CategoryId,
            RewardPoints = 50,
            Deadline = Input.Deadline,
            Location = new AddressInfo
            {
                Latitude = Input.LocationLatitude,
                Longitude = Input.LocationLongitude,
                FullAddress = Input.FullAddress,
                City = Input.City
            },
            CreatedById = currentUser.Id,
            CreatedAt = DateTime.UtcNow,
            Status = Models.Enums.TaskStatus.Open,
            Images = new List<TaskImage>()
        };
        
        _context.Tasks.Add(newTask);
        await _context.SaveChangesAsync();
        
        if (imageUploads != null && imageUploads.Count > 0)
        {
            string taskFolderName = newTask.Id.ToString();
            string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "tasks", taskFolderName);
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }
            
            foreach (var file in imageUploads)
            {
                if (file.Length > 0)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string filePath = Path.Combine(uploadPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    
                    newTask.Images.Add(new TaskImage { 
                        Url = $"/uploads/tasks/{taskFolderName}/{fileName}" 
                    });
                }
            }
            await _context.SaveChangesAsync();
        }

        TempData["StatusMessage"] = "Úkol byl úspěšně vytvořen a zveřejněn na mapě!";
        return RedirectToPage("/Index");  //potom budeme presmerovavat na index toho ukolu - na vlastni stranku
    }

    private async Task LoadCategoriesAsync()
    {
        AllCategories = await _context.Categories.ToListAsync();
    }
}