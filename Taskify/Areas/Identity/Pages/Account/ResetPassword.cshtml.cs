using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Taskify.Constants;
using Taskify.Models;

namespace Taskify.Areas.Identity.Pages.Account
{
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<User> _userManager;

        public ResetPasswordModel(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required] 
            [EmailAddress]
            public string Email { get; set; }
            
            [Required(ErrorMessage = "Heslo je povinné.")]
            [StringLength(100, ErrorMessage = "{0} musí mít alespoň {2} a maximálně {1} znaků.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; }
            
            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "Hesla se neshodují.")]
            public string ConfirmPassword { get; set; }

            [Required]
            public string Code { get; set; }
        }

        public IActionResult OnGet(string code = null, string email = null)
        {
            if (code == null)
            {
                return BadRequest("Pro resetování hesla musí být k dispozici kód.");
            }
            else
            {
                Input = new InputModel
                {
                    Code = code,
                    Email = email
                };
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                return RedirectToPage("./ResetPasswordConfirmation");
            }
            
            string decodedCode;
            try
            {
                decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(Input.Code));
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Neplatný formát tokenu.");
                return Page();
            }
            
            var result = await _userManager.ResetPasswordAsync(user, decodedCode, Input.Password);
            
            if (result.Succeeded)
            {
                return RedirectToPage("./ResetPasswordConfirmation");
            }
            
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return Page();
        }
    }
}