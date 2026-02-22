using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Taskify.Models;
using Taskify.Services;

namespace Taskify.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly IEmailSender _emailSender;

        public ForgotPasswordModel(UserManager<User> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Zadejte prosím e-mail.")]
            [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "Neplatný formát e-mailu.")]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                
                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", code, email = Input.Email },
                    protocol: Request.Scheme);
                
                string htmlBody = EmailTemplates.GetHtmlTemplate(
                    title: "Resetování hesla 🔒",
                    message: "Obdrželi jsme žádost o obnovení hesla pro váš účet. Klikněte na tlačítko níže pro nastavení nového hesla.",
                    buttonText: "Nastavit nové heslo",
                    buttonUrl: callbackUrl
                );

                await _emailSender.SendEmailAsync(
                    Input.Email,
                    "Reset hesla",
                    htmlBody);

                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            return Page();
        }
    }
}