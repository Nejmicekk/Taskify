using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Taskify.Models;

namespace Taskify.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginWithRecoveryCodeModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<LoginWithRecoveryCodeModel> _logger;

        public LoginWithRecoveryCodeModel(
            SignInManager<User> signInManager,
            UserManager<User> userManager,
            ILogger<LoginWithRecoveryCodeModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [BindProperty]
            [Required(ErrorMessage = "Zadejte obnovovací kód.")]
            [DataType(DataType.Text)]
            [Display(Name = "Obnovovací kód")]
            public string RecoveryCode { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
        {
            // Ensure the user has gone through the username & password screen first
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new InvalidOperationException($"Nepodařilo se načíst uživatele pro dvoufázové ověření.");
            }

            ReturnUrl = returnUrl;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new InvalidOperationException($"Nepodařilo se načíst uživatele pro dvoufázové ověření.");
            }

            var recoveryCode = Input.RecoveryCode.Replace(" ", string.Empty);

            var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

            var userId = await _userManager.GetUserIdAsync(user);

            if (result.Succeeded)
            {
                _logger.LogInformation("User with ID '{UserId}' logged in with a recovery code.", userId);
                return LocalRedirect(returnUrl ?? Url.Content("~/"));
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning("User with ID '{UserId}' account locked out.", userId);
                return RedirectToPage("./Lockout");
            }
            else
            {
                _logger.LogWarning("Invalid recovery code entered for user with ID '{UserId}'", userId);
                ModelState.AddModelError(string.Empty, "Neplatný obnovovací kód.");
                return Page();
            }
        }
    }
}
