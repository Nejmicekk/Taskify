#nullable disable

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Taskify.Models;

namespace Taskify.Areas.Identity.Pages.Account.Manage
{
    public class NotificationsModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public NotificationsModel(
            UserManager<User> userManager,
            SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Display(Name = "Povolit e-mailová oznámení")]
            public bool EnableEmailNotifications { get; set; }

            [Display(Name = "Aktualizace úkolů")]
            public bool EmailOnTaskUpdates { get; set; }

            [Display(Name = "Výsledky úkolů")]
            public bool EmailOnTaskResults { get; set; }

            [Display(Name = "Zabezpečení účtu")]
            public bool EmailOnAccountSecurity { get; set; }
        }

        private async Task LoadAsync(User user)
        {
            Input = new InputModel
            {
                EnableEmailNotifications = user.EnableEmailNotifications,
                EmailOnTaskUpdates = user.EmailOnTaskUpdates,
                EmailOnTaskResults = user.EmailOnTaskResults,
                EmailOnAccountSecurity = user.EmailOnAccountSecurity
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Nebylo možné načíst uživatele s ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Nebylo možné načíst uživatele s ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            user.EnableEmailNotifications = Input.EnableEmailNotifications;
            user.EmailOnTaskUpdates = Input.EmailOnTaskUpdates;
            user.EmailOnTaskResults = Input.EmailOnTaskResults;
            user.EmailOnAccountSecurity = Input.EmailOnAccountSecurity;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                StatusMessage = "Chyba při ukládání nastavení oznámení.";
                return RedirectToPage();
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Vaše nastavení oznámení bylo aktualizováno.";
            return RedirectToPage();
        }
    }
}
