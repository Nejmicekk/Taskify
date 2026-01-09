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

namespace Taskify.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public IndexModel(
            UserManager<User> userManager,
            SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }
        
        public string Username { get; set; }
        public int Level { get; set; }
        public int Reputation { get; set; }
        public int Points { get; set; }
        public string UserRole { get; set; }
        
        [TempData]
        public string StatusMessage { get; set; }
        
        [BindProperty]
        public InputModel Input { get; set; }
        
        [BindProperty]
        public PaswordModel PasswordInput { get; set; }
        
        // Třídy pro data formulářů
        public class InputModel
        {

            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; }
            
            [Display(Name = "O mně (Bio)")]
            public string Bio { get; set; }
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

            Username = userName;
            Level = user.Level;
            Reputation = user.Reputation;
            Points = user.Points;
            
            var roles = await _userManager.GetRolesAsync(user);
            var mainRole = roles.FirstOrDefault() ?? "Member";
            UserRole = $"Taskify {mainRole}";

            if (Input == null)
            {
                Input = new InputModel
                {
                    PhoneNumber = phoneNumber,
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
