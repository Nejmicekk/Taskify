// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Taskify.Models;
using Taskify.Constants;

namespace Taskify.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IWebHostEnvironment webHostEnvironment,
            ILogger<IndexModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }
        
        public string Username { get; set; }
        public int Level { get; set; }
        public int Reputation { get; set; }
        public int Points { get; set; }
        public int NextLevelPoints { get; set; } 
        public string UserRole { get; set; }
        public string ProfilePictureUrl { get; set; }
        
        [TempData]
        public string StatusMessage { get; set; }
        
        [BindProperty]
        public InputModel Input { get; set; }
        
        [BindProperty]
        public PaswordModel PasswordInput { get; set; }
        
        // Třídy pro data formulářů
        public class InputModel
        {
            [Display(Name = "Uživatelské jméno")]
            public string Username { get; set; }
            
            [Phone]
            [RegularExpression(AppConstants.AppRegex.Phone, ErrorMessage = "Zadejte platné české číslo (např. 777 123 456).")]
            [Display(Name = "Telefonní číslo")]
            public string PhoneNumber { get; set; }
            
            [Display(Name = "O mně (Bio)")]
            public string Bio { get; set; }
            
            [EmailAddress]
            [RegularExpression(AppConstants.AppRegex.Email, ErrorMessage = "Zadejte platný formát emailu.")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Display(Name = "Profilová fotka")]
            public IFormFile ProfilePicture { get; set; }
        }

        public class PaswordModel
        {
            [Required(ErrorMessage = "Musíte zadat současné heslo.")]
            [DataType(DataType.Password)]
            [Display(Name = "Současné heslo")]
            public string OldPassword { get; set; }

            [Required(ErrorMessage = "Musíte zadat nové heslo.")]
            [StringLength(100, MinimumLength = 4, ErrorMessage = "{0} musí mít alespoň {2} znaky.")]
            [DataType(DataType.Password)]
            [Display(Name = "Nové heslo")]
            public string NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Potvrzení hesla")]
            [Compare("NewPassword", ErrorMessage = "Nové heslo a potvrzení se neshodují.")]
            public string ConfirmPassword { get; set; }
        }

        // Načtení dat
        private async Task LoadAsync(User user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            var email = await _userManager.GetEmailAsync(user);

            Username = userName;
            Level = user.Level;
            Reputation = user.Reputation;
            Points = user.Points;
            ProfilePictureUrl = user.ProfilePictureUrl;
            
            NextLevelPoints = (int)(100 * Math.Pow(1.3, Level - 1));
            
            var roles = await _userManager.GetRolesAsync(user);
            var mainRole = roles.FirstOrDefault() ?? "Member";
            UserRole = $"Taskify {mainRole}";

            if (Input == null)
            {
                Input = new InputModel
                {
                    Username = userName,
                    PhoneNumber = phoneNumber,
                    Email = email,
                    Bio = user.Bio
                };
            }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound($"Nebylo možné načíst uživatele s ID '{_userManager.GetUserId(User)}'.");

            await LoadAsync(user);
            return Page();
        }

        // 1. Handler - uložení profilu
        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound($"Nebylo možné načíst uživatele s ID '{_userManager.GetUserId(User)}'.");
            
            ModelState.Clear();
            
            if (!TryValidateModel(Input, nameof(Input)))
            {
                await LoadAsync(user);
                return Page();
            }
            
            // Změna profilového obrázku
            if (Input.ProfilePicture != null)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "profiles");
                Directory.CreateDirectory(uploadsFolder); // Vytvoří složku, pokud neexistuje

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Input.ProfilePicture.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await Input.ProfilePicture.CopyToAsync(fileStream);
                }
                
                string oldProfilePictureUrl = user.ProfilePictureUrl;

                user.ProfilePictureUrl = "/uploads/profiles/" + uniqueFileName;
                await _userManager.UpdateAsync(user);
                
                if (!string.IsNullOrEmpty(oldProfilePictureUrl))
                {
                    string oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, oldProfilePictureUrl.TrimStart('/'));

                    if (System.IO.File.Exists(oldFilePath))
                    {
                        try
                        {
                            System.IO.File.Delete(oldFilePath);
                            _logger.LogInformation("Stará profilová fotka byla smazána: {Path}", oldFilePath);
                        }
                        catch (IOException e)
                        {
                            _logger.LogWarning("Nepodařilo se smazat starou profilovku. Soubor: {Path}. Chyba: {Message}", oldFilePath, e.Message);
                        }
                    }
                }
            }
            
            // Změna username
            var currentUsername = await _userManager.GetUserNameAsync(user);
            if (Input.Username != currentUsername)
            {
                var setUserNameResult = await _userManager.SetUserNameAsync(user, Input.Username);
                if (!setUserNameResult.Succeeded)
                {
                    StatusMessage = "Chyba: Toto uživatelské jméno je již obsazené nebo neplatné.";
                    return RedirectToPage();
                }
            }

            // Změna telefonního čísla
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Chyba při ukládání telefoního čísla.";
                    return RedirectToPage();
                }
            }
            
            // Změna emailu
            var email = await _userManager.GetEmailAsync(user);
            if (Input.Email != email)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, Input.Email);
                if (!setEmailResult.Succeeded)
                {
                    StatusMessage = "Chyba při ukládání emailu.";
                    return RedirectToPage();
                }
            }
            
            // Změna bia
            if (user.Bio != Input.Bio)
            {
                user.Bio = Input.Bio;
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    StatusMessage = "Chyba při ukládání profilu.";
                    return RedirectToPage();
                }
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Váš profil byl aktualizován!";
            return RedirectToPage();
        }
        
        //2. Handler - změna hesla
        public async Task<IActionResult> OnPostChangePasswordAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound($"Nebylo možné načíst uživatele s ID '{_userManager.GetUserId(User)}'.");
            
            ModelState.Clear();
            
            if (!TryValidateModel(PasswordInput, nameof(PasswordInput)))
            {
                await LoadAsync(user);
                return Page();
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, PasswordInput.OldPassword, PasswordInput.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                await LoadAsync(user);
                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Heslo bylo úspěšně změněno.";
            return RedirectToPage();
        }
    }
}
