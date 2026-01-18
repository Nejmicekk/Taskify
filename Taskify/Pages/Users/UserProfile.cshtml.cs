using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Taskify.Models;
using Microsoft.AspNetCore.Authorization;

namespace Taskify.Pages.Users
{
    [AllowAnonymous]
    public class UserProfileModel : PageModel
    {
        private readonly UserManager<User> _userManager;

        public UserProfileModel(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public User? DisplayedUser { get; set; }
        public string UserRole { get; set; }
        public int NextLevelPoints { get; set; }
        public bool IsMe { get; set; }

        public async Task<IActionResult> OnGetAsync(string username)
        {
            DisplayedUser = await _userManager.FindByNameAsync(username);

            if (DisplayedUser == null)
            {
                return Page();
            }
            
            NextLevelPoints = (int)(100 * Math.Pow(1.3, DisplayedUser.Level - 1));
            
            var roles = await _userManager.GetRolesAsync(DisplayedUser);
            var mainRole = roles.FirstOrDefault() ?? "Member";
            UserRole = $"Taskify {mainRole}";
            
            var currentUser = await _userManager.GetUserAsync(User);
            IsMe = currentUser != null && currentUser.Id == DisplayedUser.Id;

            return Page();
        }
    }
}