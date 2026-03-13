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
    
    public List<Category> Categories { get; set; } = new();

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
        
        public string? ImageUploadsValidation { get; set; }
        
        [Required(ErrorMessage = "Musíte vybrat místo na mapě!")]
        public string LocationLatitude { get; set; }

        [Required(ErrorMessage = "Musíte vybrat místo na mapě!")]
        public string LocationLongitude { get; set; }
        
        public string? FullAddress { get; set; }
        
        public string? Region { get; set; }
        public string? City { get; set; }
        public string? Street { get; set; }
        public string? StreetNumber { get; set; }
        public string? PostCode { get; set; }
    }

    public async Task OnGetAsync()
    {
        await LoadCategoriesAsync();
    }

    public async Task<IActionResult> OnPostAsync(List<IFormFile> imageUploads)
    {
        string rawLat = Input.LocationLatitude?.Trim() ?? "";
        string rawLng = Input.LocationLongitude?.Trim() ?? "";

        double lat = 0;
        double lng = 0;
        bool isLatOk = false;
        bool isLngOk = false;
        
        if (!string.IsNullOrEmpty(rawLat) && !string.IsNullOrEmpty(rawLng))
        {
            isLatOk = double.TryParse(rawLat.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out lat);
            isLngOk = double.TryParse(rawLng.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out lng);
            
            if (!isLatOk)
            {
                isLatOk = double.TryParse(rawLat.Replace('.', ','), out lat);
            }
            if (!isLngOk)
            {
                isLngOk = double.TryParse(rawLng.Replace('.', ','), out lng);
            }
        }
        
        if (!isLatOk || lat < -90 || lat > 90)
        {
            ModelState.AddModelError("Input.LocationLatitude", "Neplatná šířka (lat).");
        }
        if (!isLngOk || lng < -180 || lng > 180)
        {
            ModelState.AddModelError("Input.LocationLongitude", "Neplatná délka (lng).");
        }
        
        const int MaxFileCount = 10;
        const long MaxFileSize = 5 * 1024 * 1024;
        Console.WriteLine();
        Console.WriteLine();

        foreach (var keyValuePair in ModelState)
        {
            Console.WriteLine($"{keyValuePair.Key}:");
            foreach (var valueError in keyValuePair.Value.Errors)
            {
                Console.WriteLine(valueError.ErrorMessage);
            }
        }

        Console.WriteLine();
        Console.WriteLine();

        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync();
            return Page();
        }
        
        if (imageUploads == null || imageUploads.Count == 0)
        {
            ModelState.AddModelError("Input.ImageUploadsValidation", "Musíte nahrát alespoň jeden obrázek!");
            await LoadCategoriesAsync();
            return Page();
        }

        if (imageUploads.Count > MaxFileCount)
        {
            ModelState.AddModelError("Input.ImageUploadsValidation", $"Maximum je {MaxFileCount} fotek!");
            await LoadCategoriesAsync();
            return Page();
        }

        if (imageUploads.Any(f => f.Length > MaxFileSize))
        {
            ModelState.AddModelError("Input.ImageUploadsValidation", "Jedna z fotek je moc velká (max 5MB)!");
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
                Latitude = lat,
                Longitude = lng,
                FullAddress = Input.FullAddress,
                City = Input.City,
                Street = Input.Street,
                StreetNumber = Input.StreetNumber,
                PostCode = Input.PostCode
            },
            CreatedById = currentUser.Id,
            CreatedAt = DateTime.UtcNow,
            Status = Models.Enums.TaskStatus.Open,
            Images = new List<TaskImage>()
        };
        
        _context.Tasks.Add(newTask);
        await _context.SaveChangesAsync();
        
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

        TempData["StatusMessage"] = "Úkol byl úspěšně vytvořen a zveřejněn na mapě!";
        return RedirectToPage("/Index");
    }

    private async Task LoadCategoriesAsync()
    {
        Categories = await _context.Categories.ToListAsync();
    }
}