// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Taskify.Models;
using Taskify.Constants;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.WebUtilities;
using Taskify.Services;

namespace Taskify.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<IndexModel> _logger;
        private readonly IEmailSender _emailSender;

        public IndexModel(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IWebHostEnvironment webHostEnvironment,
            ILogger<IndexModel> logger,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _emailSender = emailSender;
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
        public PasswordModel PasswordInput { get; set; }
        
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
            
            [Required(ErrorMessage = "Email je povinný.")]
            [EmailAddress(ErrorMessage = "Zadejte platný formát emailu.")]
            [RegularExpression(AppConstants.AppRegex.Email, ErrorMessage = "Zadejte platný formát emailu.")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Display(Name = "Profilová fotka")]
            public IFormFile ProfilePicture { get; set; }
            
            public String ProfilePictureUrl { get; set; }
        }

        public class PasswordModel
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
            
            Input = new InputModel
            {
                Username = userName,
                PhoneNumber = phoneNumber,
                Email = email,
                Bio = user.Bio,
                ProfilePictureUrl = user.ProfilePictureUrl
            };
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
                var updatePhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!updatePhoneResult.Succeeded)
                {
                    StatusMessage = "Chyba při ukládání telefonního čísla.";
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
                    StatusMessage = "Chyba při ukládání bia.";
                    return RedirectToPage();
                }
            }
            
            // Změna emailu
            var email = await _userManager.GetEmailAsync(user);
            if (Input.Email != email)
            {
                if (user.LastEmailChangeDate != null)
                {
                    var daysSinceChange = (DateTime.UtcNow - user.LastEmailChangeDate.Value).TotalDays;
                    var daysLimit = 14;

                    if (daysSinceChange < daysLimit)
                    {
                        var daysLeft = Math.Ceiling(daysLimit - daysSinceChange);
                        StatusMessage = $"Chyba: E-mail lze změnit pouze jednou za {daysLimit} dní. Zkuste to znovu za {daysLeft} dní.";
                        return RedirectToPage();
                    }
                }
                
                user = await _userManager.GetUserAsync(User);
                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateChangeEmailTokenAsync(user, Input.Email);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
    
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmailChange",
                    pageHandler: null,
                    values: new { area = "Identity", userId = userId, email = Input.Email, code = code },
                    protocol: Request.Scheme);
                
                string htmlBody = EmailTemplates.GetHtmlTemplate(
                    title: "Změna e-mailu 📤",
                    message: "Obdrželi jsme žádost na změnu e-mailu u vašeho účtu Taskify. Pokud jste to nebyli vy, ignorujte tento email.",
                    buttonText: "Potvrdit nový e-mail",
                    buttonUrl: callbackUrl
                );
                
                await _emailSender.SendEmailAsync(
                    Input.Email,
                    "Potvrzení změny e-mailu",
                    htmlBody);
                
                StatusMessage = "Profil byl aktualizován. Na nový e-mail byl odeslán potvrzovací odkaz.";
                return RedirectToPage();
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
            
            if (PasswordInput.OldPassword == PasswordInput.NewPassword)
            {
                ModelState.AddModelError(string.Empty, "Nové heslo musí být odlišné od starého.");
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
                ViewData["ActiveTab"] = "security";
                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Heslo bylo úspěšně změněno.";
            return RedirectToPage();
        }
    }
}
