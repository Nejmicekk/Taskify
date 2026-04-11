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
        private readonly IAchievementService _achievementService;

        public IndexModel(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IWebHostEnvironment webHostEnvironment,
            ILogger<IndexModel> logger,
            IEmailSender emailSender,
            IAchievementService _achievementService)
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
        
        public class InputModel
        {
            [Required(ErrorMessage = "Zobrazované jméno je povinné.")]
            [StringLength(100, ErrorMessage = "Zobrazované jméno může mít maximálně 100 znaků.")]
            [Display(Name = "Zobrazované jméno")]
            public string Name { get; set; }

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
            
            NextLevelPoints = (int)(100 * Math.Pow(1.1, Level - 1));
            
            var roles = await _userManager.GetRolesAsync(user);
            var mainRole = roles.FirstOrDefault() ?? "Member";
            UserRole = $"Taskify {mainRole}";
            
            Input = new InputModel
            {
                Name = user.Name,
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

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToPage("/Users/UserProfile", new { area = "", username = user.UserName });
            }

            await LoadAsync(user);
            return Page();
        }
        
        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound($"Nebylo možné načíst uživatele s ID '{_userManager.GetUserId(User)}'.");
            
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToPage("/Users/UserProfile", new { area = "", username = user.UserName });
            }
            
            if (string.IsNullOrEmpty(Input.Username)) Input.Username = user.UserName;
            if (string.IsNullOrEmpty(Input.Name)) Input.Name = user.Name;
            if (string.IsNullOrEmpty(Input.Email)) Input.Email = user.Email;
            if (Input.Bio == null) Input.Bio = user.Bio;
            if (string.IsNullOrEmpty(Input.PhoneNumber)) Input.PhoneNumber = user.PhoneNumber;

            ModelState.Clear();
            
            if (!TryValidateModel(Input, nameof(Input)))
            {
                await LoadAsync(user);
                return Page();
            }
            
            if (Input.ProfilePicture != null)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "profiles");
                Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Input.ProfilePicture.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await Input.ProfilePicture.CopyToAsync(fileStream);
                }
                
                string oldProfilePictureUrl = user.ProfilePictureUrl;

                user.ProfilePictureUrl = "/uploads/profiles/" + uniqueFileName;
                await _userManager.UpdateAsync(user);
                
                await _achievementService.CheckSpecialAchievementAsync(user.Id, "První krok");
                
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

            if (user.Name != Input.Name)
            {
                user.Name = Input.Name;
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    StatusMessage = "Chyba při ukládání zobrazovaného jména.";
                    return RedirectToPage();
                }
            }
            
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
                    title: "Změna e-mailu",
                    message: "Obdrželi jsme žádost na změnu e-mailu u vašeho účtu Taskify. Pokud jste to nebyli vy, ignorujte tento email.",
                    buttonText: "Potvrdit nový e-mail",
                    buttonUrl: callbackUrl,
                    icon: "📧"
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
    }
}
