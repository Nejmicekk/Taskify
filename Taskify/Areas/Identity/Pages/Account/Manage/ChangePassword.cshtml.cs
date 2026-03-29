using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Taskify.Models;

namespace Taskify.Areas.Identity.Pages.Account.Manage
{
    public class ChangePasswordModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<ChangePasswordModel> _logger;

        public ChangePasswordModel(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ILogger<ChangePasswordModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public class InputModel
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

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Nebylo možné načíst uživatele s ID '{_userManager.GetUserId(User)}'.");
            }

            var hasPassword = await _userManager.HasPasswordAsync(user);
            if (!hasPassword)
            {
                return RedirectToPage("./SetPassword");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Nebylo možné načíst uživatele s ID '{_userManager.GetUserId(User)}'.");
            }

            if (Input.OldPassword == Input.NewPassword)
            {
                ModelState.AddModelError(string.Empty, "Nové heslo musí být odlišné od starého.");
                return Page();
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("Uživatel úspěšně změnil heslo.");
            StatusMessage = "Heslo bylo úspěšně změněno.";

            return RedirectToPage();
        }
    }
}
