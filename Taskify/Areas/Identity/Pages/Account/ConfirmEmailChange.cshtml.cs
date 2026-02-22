// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Taskify.Models;

namespace Taskify.Areas.Identity.Pages.Account
{
    public class ConfirmEmailChangeModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public ConfirmEmailChangeModel(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }
        
        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string userId, string email, string code)
        {
            if (userId == null || email == null || code == null)
            {
                return RedirectToPage("/Index");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Nepodařilo se načíst uživatele s ID '{userId}'.");
            }

            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ChangeEmailAsync(user, email, code);
            if (!result.Succeeded)
            {
                var error = result.Errors.FirstOrDefault();
                StatusMessage = error != null ? $"Chyba: {error.Description}" : "Chyba při změně e-mailu.";
                return Page();
            }
            
            var freshUser = await _userManager.FindByIdAsync(userId);

            if (freshUser != null)
            {
                freshUser.LastEmailChangeDate = DateTime.UtcNow;
                var updateResult = await _userManager.UpdateAsync(freshUser);
                if (!updateResult.Succeeded)
                {
                    throw new Exception("Nepodařilo se uložit datum změny: " + updateResult.Errors.First().Description);
                }
            }
            
            await _signInManager.SignOutAsync();
            
            HttpContext.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity());
            
            StatusMessage = "E-mail byl úspěšně změněn.";
            return Page();
        }
    }
}
