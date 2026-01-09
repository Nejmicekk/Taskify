using Microsoft.AspNetCore.Identity;
using Taskify.Models;

namespace Taskify.Data;

public static class RoleSeeder
{
    public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
        
        string[] roleNames = { "Admin", "User" };
        
        foreach (var roleName in roleNames)
        {
            var roleExist = await roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
        
        // Defaultní admin
        var adminUser = await userManager.FindByNameAsync("admin");
        
        if (adminUser == null)
        {
            var newAdmin = new User
            {
                UserName = "admin",
                Email = "admin@taskify.local", // Email je v Identity povinný, takže jsem přiřadil custom nějaký
                EmailConfirmed = true
            };

            // Vytvoření uživatele s heslem
            var result = await userManager.CreateAsync(newAdmin, "devAdminTaskify333");

            if (result.Succeeded)
            {
                // Pokud se vytvořil, dáme mu roli Admin
                await userManager.AddToRoleAsync(newAdmin, "Admin");
            }
        }
    }
}