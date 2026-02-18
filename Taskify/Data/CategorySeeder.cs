using Microsoft.EntityFrameworkCore;
using Taskify.Models;

namespace Taskify.Data;

public class CategorySeeder
{
    public static async Task SeedCategoriesAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (await context.Categories.AnyAsync()) return;
        
        var parentCategories = new List<Category>()
        {
            new Category { Name = "Úklid okolí", IconUrl = "bi-trash3-fill", LevelNumber = 1 },
            new Category { Name = "Drobné opravy a údržba", IconUrl = "bi-hammer", LevelNumber = 1 },
            new Category { Name = "Zeleň a komunitní prvky", IconUrl = "bi-tree-fill", LevelNumber = 1 },
            new Category { Name = "Sousedská výpomoc", IconUrl = "bi-people-fill", LevelNumber = 1 }
        };
        
        await context.Categories.AddRangeAsync(parentCategories);
        await context.SaveChangesAsync();
        
        var subCategories = new List<Category>();
        
        var uklidId = parentCategories.First(c => c.Name == "Úklid okolí").Id;
        var sezonniUklidCategory = new Category { Name = "Sezónní úklid cest a chodníků", ParentId = uklidId, LevelNumber = 2, IconUrl = "bi-wind" };
        
        subCategories.AddRange(new[] {
            new Category { Name = "Sběr odpadků", ParentId = uklidId, LevelNumber = 2, IconUrl = "bi-bag-x-fill" },
            new Category { Name = "Odstranění lokálního znečištění", ParentId = uklidId, LevelNumber = 2, IconUrl = "bi-stars" },
            sezonniUklidCategory
        });
        
        var opravyId = parentCategories.First(c => c.Name == "Drobné opravy a údržba").Id;
        var verejnePrvkyCategory = new Category { Name = "Oprava veřejných prvků", ParentId = opravyId, LevelNumber = 2, IconUrl = "bi-sign-stop-fill" };
        
        subCategories.AddRange(new[] {
            new Category { Name = "Oprava drobného mobiliáře", ParentId = opravyId, LevelNumber = 2, IconUrl = "bi-tools" },
            new Category { Name = "Nátěry a renovace", ParentId = opravyId, LevelNumber = 2, IconUrl = "bi-paint-bucket" },
            verejnePrvkyCategory
        });
        
        var zelenId = parentCategories.First(c => c.Name == "Zeleň a komunitní prvky").Id;
        var verejnaZelenCategory = new Category { Name = "Péče o veřejnou zeleň", ParentId = zelenId, LevelNumber = 2, IconUrl = "bi-droplet-fill" };
        
        subCategories.AddRange(new[] {
            new Category { Name = "Údržba komunitních prostor", ParentId = zelenId, LevelNumber = 2, IconUrl = "bi-flower1" },
            new Category { Name = "Organizace komunitních akcí", ParentId = zelenId, LevelNumber = 2, IconUrl = "bi-book" },
            verejnaZelenCategory
        });
        
        var sousedskaId = parentCategories.First(c => c.Name == "Sousedská výpomoc").Id;
        var mazlicciCategory = new Category { Name = "Domácí mazlíčci", ParentId = sousedskaId, LevelNumber = 2, IconUrl = "bi-tencent-qq" };
        var doucovaniCategory = new Category { Name = "Doučování a sdílení znalostí", ParentId = sousedskaId, LevelNumber = 2, IconUrl = "bi-book-fill" };
        var nakupyCategory = new Category { Name = "Nákupy a pochůzky", ParentId = sousedskaId, LevelNumber = 2, IconUrl = "bi-basket-fill" };
        
        subCategories.AddRange(new[] {
            mazlicciCategory,
            doucovaniCategory,
            nakupyCategory,
            new Category { Name = "Pomoc se stěhováním a odvozem", ParentId = sousedskaId, LevelNumber = 2, IconUrl = "bi-truck" },
        });
        
        await context.Categories.AddRangeAsync(subCategories);
        await context.SaveChangesAsync();
        
        var level3Categories = new List<Category>()
        {
            new Category { Name = "Odklízení sněhu a náledí", ParentId = sezonniUklidCategory.Id, LevelNumber = 3, IconUrl = "bi-snow" },
            new Category { Name = "Hrabání a odvoz listí", ParentId = sezonniUklidCategory.Id, LevelNumber = 3, IconUrl = "bi-tree" },

            new Category { Name = "Dětská hřiště a sportoviště", ParentId = verejnePrvkyCategory.Id, LevelNumber = 3, IconUrl = "bi-bicycle" },
            new Category { Name = "Zábradlí, ploty a svodidla", ParentId = verejnePrvkyCategory.Id, LevelNumber = 3, IconUrl = "bi-segmented-nav" },

            new Category { Name = "Zalévání", ParentId = verejnaZelenCategory.Id, LevelNumber = 3, IconUrl = "bi-cloud-rain-fill" },
            new Category { Name = "Sekání trávy a prostřih", ParentId = verejnaZelenCategory.Id, LevelNumber = 3, IconUrl = "bi-scissors" },

            new Category { Name = "Venčení psů", ParentId = mazlicciCategory.Id, LevelNumber = 3, IconUrl = "bi-tencent-qq" },
            new Category { Name = "Hlídání zvířat", ParentId = mazlicciCategory.Id, LevelNumber = 3, IconUrl = "bi-house-heart" },
            
            new Category { Name = "Matematika a logika", ParentId = doucovaniCategory.Id, LevelNumber = 3, IconUrl = "bi-calculator-fill" },
            new Category { Name = "Cizí jazyky a konverzace", ParentId = doucovaniCategory.Id, LevelNumber = 3, IconUrl = "bi-translate" },
            new Category { Name = "Přírodovědné předměty", ParentId = doucovaniCategory.Id, LevelNumber = 3, IconUrl = "bi-globe-americas" },
            new Category { Name = "IT a elektrotechnika", ParentId = doucovaniCategory.Id, LevelNumber = 3, IconUrl = "bi-laptop" },
            
            new Category { Name = "Základní nákupy a léky", ParentId = nakupyCategory.Id, LevelNumber = 3, IconUrl = "bi-cart-fill" },
            new Category { Name = "Vyzvednutí zásilek a pošta", ParentId = nakupyCategory.Id, LevelNumber = 3, IconUrl = "bi-envelope-fill" }
        };

        await context.Categories.AddRangeAsync(level3Categories);
        await context.SaveChangesAsync();
    }
}