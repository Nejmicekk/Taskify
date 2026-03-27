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
        public string UserRole { get; set; } = "Taskify Member";
        public int NextLevelPoints { get; set; }
        public bool IsMe { get; set; }

        public async Task<IActionResult> OnGetAsync(string username)
        {
            DisplayedUser = await _userManager.FindByNameAsync(username);

            if (DisplayedUser == null)
            {
                return Page();
            }
            
            NextLevelPoints = (int)(100 * Math.Pow(1.1, DisplayedUser.Level - 1));
            
            var roles = await _userManager.GetRolesAsync(DisplayedUser);
            var mainRole = roles.FirstOrDefault() ?? "Member";
            UserRole = $"Taskify {mainRole}";
            
            var currentUser = await _userManager.GetUserAsync(User);
            
            bool isTargetAdmin = roles.Contains("Admin");
            bool isVisitorAdmin = currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Admin");
            
            if (isTargetAdmin && !isVisitorAdmin)
            {
                DisplayedUser = null;
                return Page();
            }

            IsMe = currentUser != null && currentUser.Id == DisplayedUser.Id;

            return Page();
        }
    }
}