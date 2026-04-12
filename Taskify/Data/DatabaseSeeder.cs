using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Taskify.Models;
using Taskify.Models.Enums;
using TaskStatus = Taskify.Models.Enums.TaskStatus;

namespace Taskify.Data;

public static class DatabaseSeeder
{
    public static async Task SeedDatabaseAsync(IServiceProvider serviceProvider)
    {
        Console.WriteLine("--- DatabaseSeeder: START ---");
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        if (await context.Tasks.AnyAsync() && await context.UserAchievements.AnyAsync()) 
        {
            Console.WriteLine("Data již existují, přeskakuji seedování.");
            return;
        }

        var faker = new Faker("cz");
        var allAchievements = await context.Achievements.ToListAsync();

        // 1. GENEROVÁNÍ UŽIVATELŮ
        var existingUserCount = await context.Users.CountAsync();
        var seededUsers = new List<User>();

        if (existingUserCount < 10)
        {
            Console.WriteLine("Generuji 50 nových uživatelů se smysluplným Bio...");
            for (int i = 0; i < 50; i++)
            {
                var firstName = faker.Name.FirstName();
                var lastName = faker.Name.LastName();
                var userName = faker.Internet.UserName(firstName, lastName) + faker.Random.Int(1000, 9999);
                int randomLevel = faker.Random.Int(1, 20);
                int minPoints = (randomLevel == 1) ? 0 : (int)(100 * Math.Pow(1.1, randomLevel - 2));
                int maxPoints = (int)(100 * Math.Pow(1.1, randomLevel - 1)) - 1;
                
                var user = new User
                {
                    Name = $"{firstName} {lastName}",
                    UserName = userName,
                    Email = faker.Internet.Email(firstName, lastName).ToLower(),
                    EmailConfirmed = true,
                    Bio = GenerateBio(faker),
                    Reputation = faker.Random.Int(0, 1000),
                    Level = randomLevel,
                    Points = faker.Random.Int(minPoints, Math.Max(minPoints, maxPoints)),
                    ProfilePictureUrl = $"https://i.pravatar.cc/150?u={userName}",
                    TotalTasksCompleted = faker.Random.Int(0, randomLevel * 5),
                    TotalTasksCreated = faker.Random.Int(0, 10)
                };
                user.PasswordHash = passwordHasher.HashPassword(user, "Heslo123!");
                
                var result = await userManager.CreateAsync(user);
                if (result.Succeeded) 
                {
                    await userManager.AddToRoleAsync(user, "User");
                    seededUsers.Add(user);
                }
            }
        }
        else
        {
            seededUsers = await context.Users.Where(u => u.UserName != "admin").ToListAsync();
        }

        // 2. GENEROVÁNÍ ACHIEVEMENTŮ
        Console.WriteLine("Přiřazuji achievementy na základě atributů...");
        foreach (var user in seededUsers)
        {
            var userAchievements = new List<UserAchievement>();
            
            AddAchievementIfMet(user, "První krok", allAchievements, userAchievements);
            
            if (user.Level >= 2) AddAchievementIfMet(user, "Učeň", allAchievements, userAchievements);
            if (user.Level >= 5) AddAchievementIfMet(user, "Pokročilý", allAchievements, userAchievements);
            if (user.Level >= 10) AddAchievementIfMet(user, "Expert", allAchievements, userAchievements);
            
            if (user.Reputation >= 100) AddAchievementIfMet(user, "Slušný občan", allAchievements, userAchievements);
            if (user.Reputation >= 500) AddAchievementIfMet(user, "Známá tvář", allAchievements, userAchievements);
            
            if (user.TotalTasksCompleted >= 1) AddAchievementIfMet(user, "Dobrý skutek", allAchievements, userAchievements);
            if (user.TotalTasksCompleted >= 20) AddAchievementIfMet(user, "Pomocná ruka", allAchievements, userAchievements);
            
            if (user.TotalTasksCreated >= 1) AddAchievementIfMet(user, "Iniciátor", allAchievements, userAchievements);
            if (user.TotalTasksCreated >= 10) AddAchievementIfMet(user, "Organizátor", allAchievements, userAchievements);
            
            if (faker.Random.Bool(0.15f)) AddAchievementIfMet(user, "Noční hrdina", allAchievements, userAchievements);
            if (faker.Random.Bool(0.15f)) AddAchievementIfMet(user, "Ranní ptáče", allAchievements, userAchievements);
            if (faker.Random.Bool(0.10f)) AddAchievementIfMet(user, "Na poslední chvíli", allAchievements, userAchievements);

            if (userAchievements.Any())
            {
                await context.UserAchievements.AddRangeAsync(userAchievements);
            }
        }
        await context.SaveChangesAsync();

        // 3. GENEROVÁNÍ ÚKOLŮ
        var categories = await context.Categories.ToListAsync();
        var regionalData = GetRegionalData();

        Console.WriteLine("Generuji 200 úkolů...");
        var tasks = new List<TaskItem>();

        for (int i = 0; i < 200; i++)
        {
            var creator = faker.PickRandom(seededUsers);
            var category = faker.PickRandom(categories);
            var region = faker.PickRandom(regionalData);
            var cityCoords = faker.PickRandom(region.Coords);
            
            var statusRoll = faker.Random.Int(1, 100);
            TaskStatus status = statusRoll switch {
                <= 40 => TaskStatus.Open,
                <= 60 => TaskStatus.InProgress,
                <= 90 => TaskStatus.Completed,
                _ => TaskStatus.Archived
            };

            User? assignedUser = (status != TaskStatus.Open) 
                ? faker.PickRandom(seededUsers.Where(u => u.Id != creator.Id)) 
                : null;

            var taskTitle = GenerateTitleForCategory(category.Name, faker);
            var taskDescription = GenerateDescriptionForTitle(taskTitle, faker);

            var task = new TaskItem
            {
                Title = taskTitle,
                Description = taskDescription,
                RewardPoints = 50,
                Status = status,
                CreatedAt = faker.Date.Past(1),
                Deadline = faker.Date.Future(1),
                CategoryId = category.Id,
                CreatedById = creator.Id,
                AssignedToId = assignedUser?.Id,
                Location = new AddressInfo
                {
                    Street = faker.Address.StreetName(),
                    StreetNumber = faker.Address.BuildingNumber(),
                    City = faker.Address.City(),
                    PostCode = region.PostCode,
                    Latitude = cityCoords.Item1 + faker.Random.Double(-0.05, 0.05),
                    Longitude = cityCoords.Item2 + faker.Random.Double(-0.05, 0.05)
                }
            };
            
            if (status == TaskStatus.Completed)
            {
                task.SubmittedAt = faker.Date.Between(task.CreatedAt, DateTime.Now);
            }

            task.Location.FullAddress = $"{task.Location.Street} {task.Location.StreetNumber}, {task.Location.City}";
            
            var photoUrls = GetPhotosForCategory(category.Name);
            int photoCount = faker.Random.Int(1, 2);
            var selectedPhotos = faker.PickRandom(photoUrls, photoCount).ToList();
            
            foreach (var url in selectedPhotos)
            {
                task.Images.Add(new TaskImage { Url = url });
            }
            tasks.Add(task);
        }

        await context.Tasks.AddRangeAsync(tasks);
        await context.SaveChangesAsync();

        // 4. GENEROVÁNÍ REPORTŮ (15)
        var reports = new List<Report>();
        var reasons = Enum.GetValues<ReportReason>();
        for (int i = 0; i < 15; i++)
        {
            var reportedTask = faker.PickRandom(tasks);
            var reporter = faker.PickRandom(seededUsers.Where(u => u.Id != reportedTask.CreatedById));
            reports.Add(new Report
            {
                ReporterId = reporter.Id,
                TaskItemId = reportedTask.Id,
                Reason = faker.PickRandom(reasons),
                Description = faker.Lorem.Sentence(),
                CreatedAt = faker.Date.Recent(10)
            });
        }

        await context.Reports.AddRangeAsync(reports);
        await context.SaveChangesAsync();
        Console.WriteLine("--- DatabaseSeeder: DOKONČENO ---");
    }

    private static void AddAchievementIfMet(User user, string achievementName, List<Achievement> allAchievements, List<UserAchievement> userAchievements)
    {
        var achievement = allAchievements.FirstOrDefault(a => a.Name == achievementName);
        if (achievement != null)
        {
            userAchievements.Add(new UserAchievement
            {
                UserId = user.Id,
                AchievementId = achievement.Id,
                EarnedAt = DateTime.UtcNow.AddDays(-new Random().Next(1, 30)),
                IsUnlocked = true,
                CurrentProgress = achievement.TargetValue
            });
        }
    }

    private static string GenerateBio(Faker faker)
    {
        var templates = new[] {
            "Student {0}, baví mě {1} a ve volném čase rád pomáhám v okolí.",
            "Nabízím pomoc s {1}. Jsem {2} a spolehlivý.",
            "Hledám příležitosti k výpomoci s {1}. {2} přístup je pro mě základ.",
            "Aktivní soused z lokality {3}. Rád pomůžu s {1}.",
            "Mám volné víkendy a rád je využiju pro {1}. Jsem {2}."
        };

        var schools = new[] { "VUT", "MUNI", "střední školy", "gymnázia" };
        var hobbies = new[] { "venčením psů", "doučováním", "drobnými opravami", "nákupy", "zahradničením", "IT technikou" };
        var traits = new[] { "dochvilný", "přátelský", "pracovitý", "ochotný", "zručný" };
        var cities = new[] { "Brna", "Prahy", "Plzně", "Ostravy", "Olomouce" };

        var template = faker.PickRandom(templates);
        return string.Format(template, 
            faker.PickRandom(schools), 
            faker.PickRandom(hobbies), 
            faker.PickRandom(traits), 
            faker.PickRandom(cities));
    }

    private static dynamic GetRegionalData()
    {
        return new[] {
            new { PostCode = "110 00", Coords = new[] { (50.08, 14.42), (49.97, 14.39), (50.10, 14.58), (50.05, 14.30), (50.12, 14.45) } }, // Praha
            new { PostCode = "250 01", Coords = new[] { (50.14, 14.10), (50.41, 14.90), (49.69, 14.01), (49.95, 15.26), (50.18, 15.04) } }, // Středočeský
            new { PostCode = "370 01", Coords = new[] { (48.97, 14.47), (49.41, 14.66), (49.30, 14.14), (49.01, 15.00), (49.12, 13.90) } }, // Jihočeský
            new { PostCode = "301 01", Coords = new[] { (49.74, 13.37), (49.39, 13.29), (49.44, 12.93), (49.89, 13.51), (49.49, 13.19) } }, // Plzeňský
            new { PostCode = "360 01", Coords = new[] { (50.23, 12.87), (50.07, 12.37), (50.18, 12.64), (50.03, 12.70), (50.25, 12.19) } }, // Karlovarský
            new { PostCode = "400 01", Coords = new[] { (50.66, 14.03), (50.50, 13.64), (50.64, 13.82), (50.46, 13.41), (50.53, 14.13) } }, // Ústecký
            new { PostCode = "460 01", Coords = new[] { (50.76, 15.05), (50.72, 15.17), (50.68, 14.53), (50.91, 15.07), (50.59, 15.34) } }, // Liberecký
            new { PostCode = "500 01", Coords = new[] { (50.21, 15.83), (50.56, 15.91), (50.43, 15.35), (50.35, 15.19), (50.09, 16.27) } }, // Královéhradecký
            new { PostCode = "530 01", Coords = new[] { (50.03, 15.77), (49.95, 15.79), (49.75, 16.46), (49.94, 16.15), (50.13, 15.54) } }, // Pardubický
            new { PostCode = "586 01", Coords = new[] { (49.39, 15.59), (49.21, 15.87), (49.60, 15.58), (49.29, 15.14), (49.36, 16.01) } }, // Vysočina
            new { PostCode = "602 00", Coords = new[] { (49.19, 16.60), (48.85, 16.05), (48.84, 17.12), (48.81, 16.64), (49.46, 16.59) } }, // Jihomoravský
            new { PostCode = "779 00", Coords = new[] { (49.59, 17.25), (49.45, 17.45), (49.96, 16.97), (49.53, 17.11), (50.22, 17.20) } }, // Olomoucký
            new { PostCode = "760 01", Coords = new[] { (49.22, 17.66), (49.06, 17.45), (49.33, 17.99), (49.47, 18.14), (49.02, 17.13) } }, // Zlínský
            new { PostCode = "702 00", Coords = new[] { (49.82, 18.26), (49.77, 18.43), (49.93, 17.90), (49.84, 18.49), (50.01, 17.66) } }  // Moravskoslezský
        };
    }

    private static string GenerateTitleForCategory(string categoryName, Faker faker)
    {
        return categoryName switch
        {
            "Sběr odpadků" => faker.PickRandom("Úklid parku", "Sběr plastů", "Čistý les", "Sběr odpadu"),
            "Odstranění lokálního znečištění" => faker.PickRandom("Odstranění skvrn", "Úklid skládky", "Čištění chodníku"),
            "Odklízení sněhu a náledí" => faker.PickRandom("Odklízení sněhu", "Posypání cesty", "Záchrana chodníku"),
            "Hrabání a odvoz listí" => faker.PickRandom("Hrabání listí", "Odvoz bioodpadu", "Podzimní úklid"),
            "Oprava drobného mobiliáře" => faker.PickRandom("Oprava lavičky", "Upevnění koše", "Oprava hřiště"),
            "Nátěry a renovace" => faker.PickRandom("Nátěr plotu", "Obnova zábradlí", "Natření lavičky"),
            "Dětská hřiště a sportoviště" => faker.PickRandom("Oprava hřiště", "Údržba pískoviště", "Kontrola hřiště"),
            "Údržba komunitních prostor" => faker.PickRandom("Péče o záhony", "Úprava dvorku", "Osázení truhlíků"),
            "Sekání trávy a prostřih" => faker.PickRandom("Posekání trávy", "Stříhání keřů", "Úprava trávníku"),
            "Zalévání" => faker.PickRandom("Zalévání květin", "Závlaha stromků", "Voda pro park"),
            "Venčení psů" => faker.PickRandom("Venčení psa", "Procházka s mopsíkem", "Hlídání psa"),
            "Hlídání zvířat" => faker.PickRandom("Krmení kočky", "Pohlídání křečka", "Návštěva mazlíčka"),
            "Matematika a logika" => faker.PickRandom("Doučování matematiky", "Příprava na zkoušku", "Pomoc s logikou"),
            "Cizí jazyky a konverzace" => faker.PickRandom("Konverzace v AJ", "Doučování němčiny", "Základy španělštiny"),
            "IT a elektrotechnika" => faker.PickRandom("Pomoc s PC", "Instalace tiskárny", "Nastavení mobilu"),
            "Základní nákupy a léky" => faker.PickRandom("Nákup potravin", "Vyzvednutí léků", "Týdenní nákup"),
            "Vyzvednutí zásilek a pošta" => faker.PickRandom("Vyzvednutí balíku", "Cesta na poštu", "Doručení dopisu"),
            "Pomoc se stěhováním a odvozem" => faker.PickRandom("Stěhování skříně", "Odvoz krabic", "Pomoc se stěhováním"),
            _ => categoryName.Length > 25 ? categoryName.Substring(0, 25) : categoryName
        };
    }

    private static string GenerateDescriptionForTitle(string title, Faker faker)
    {
        return title switch
        {
            "Úklid parku" or "Sběr plastů" or "Čistý les" or "Sběr odpadu" => "Hledáme dobrovolníky na pomoc s úklidem odpadků v okolí. Pytle a rukavice zajistíme, stačí jen dobrá nálada a chuť pomoci přírodě.",
            "Odstranění skvrn" or "Úklid skládky" or "Čištění chodníku" => "V dané lokalitě se nachází znečištění, které bychom rádi společně odstranili. Jedná se o fyzicky náročnější práci, ale výsledek bude stát za to.",
            "Odklízení sněhu" or "Posypání cesty" or "Záchrana chodníku" => "Napadlo hodně sněhu a chodníky jsou špatně schůdné. Potřebujeme pomoci s odházením sněhu před domy a posypáním namrzlých úseků solí.",
            "Hrabání listí" or "Odvoz bioodpadu" or "Podzimní úklid" => "Zahrada a přilehlé cesty jsou plné listí. Potřebujeme pomoc s jeho shrabáním na hromady a následným naložením do připravených kontejnerů.",
            "Oprava lavičky" or "Upevnění koše" or "Oprava hřiště" => "Některé prvky městského mobiliáře jsou uvolněné nebo poškozené. Pokud umíš vzít do ruky šroubovák nebo kladivo, tvoje pomoc se nám bude hodit.",
            "Nátěr plotu" or "Obnova zábradlí" or "Natření lavičky" => "Starý nátěr už dosloužil. Hledáme šikovné ruce, které by pomohly s broušením staré barvy a nanesením nové ochranné vrstvy.",
            "Údržba pískoviště" or "Kontrola hřiště" => "Dětské hřiště potřebuje pravidelnou údržbu. Jedná se o prosetí písku, odstranění plevele v okolí a kontrolu bezpečnosti herních prvků.",
            "Péče o záhony" or "Úprava dvorku" or "Osázení truhlíků" => "Hledáme milovníky zahradničení, kteří by pomohli s pletím záhonů, zastřižením okrasných keřů nebo výsadbou nových květin do truhlíků.",
            "Posekání trávy" or "Stříhání keřů" or "Úprava trávníku" => "Trávník už je příliš vysoký. Potřebujeme pomoci se sekáním a následným odklizením posekané trávy. Veškeré vybavení je k dispozici na místě.",
            "Zalévání květin" or "Závlaha stromků" or "Voda pro park" => "Kvůli suchu je potřeba pravidelně zalévat nově vysazené stromky a květinové záhony. Stačí pomoc s nošením konví nebo obsluhou hadice.",
            "Venčení psa" or "Procházka s mopsíkem" or "Hlídání psa" => "Hledám někoho spolehlivého, kdo by vzal mého čtyřnohého kamaráda na hodinovou procházku do parku. Pes je přátelský a zvyklý na lidi.",
            "Krmení kočky" or "Pohlídání křečka" or "Návštěva mazlíčka" => "Během mé nepřítomnosti potřebuji zkontrolovat mého domácího mazlíčka, dát mu čerstvou vodu, krmení a věnovat mu trochu pozornosti.",
            "Doučování matematiky" or "Příprava na zkoušku" or "Pomoc s logikou" => "Hledám trpělivého člověka, který by mi pomohl s pochopením látky a přípravou na blížící se test. Termín a místo dle domluvy.",
            "Konverzace v AJ" or "Doučování němčiny" or "Základy španělštiny" => "Rád bych si procvičil mluvení v cizím jazyce. Hledám někoho na nezávaznou konverzaci nad kávou nebo online, abych neztratil slovní zásobu.",
            "Pomoc s PC" or "Instalace tiskárny" or "Nastavení mobilu" => "Potřebuji technickou pomoc s nastavením nového zařízení nebo opravou drobné chyby v systému. Pokud se vyznáš v IT, budu vděčný za tvůj čas.",
            "Nákup potravin" or "Vyzvednutí léky" or "Týdenní nákup" => "Z důvodu nemoci nebo nedostatku času potřebuji pomoci s nákupem základních potravin nebo vyzvednutím receptu v lékárně. Seznam zašlu předem.",
            "Vyzvednutí balíku" or "Cesta na poštu" or "Doručení dopisu" => "Hledám někoho, kdo by za mě zašel na poštu vyzvednout zásilku nebo odeslat důležitý dopis. Průkaz pro vyzvednutí zajistím.",
            "Stěhování skříně" or "Odvoz krabic" or "Pomoc se stěhováním" => "Potřebuji silné ruce na pomoc s přenesením pár kusů nábytku nebo naložením krabic do auta. Práce na cca 2 hodiny, odměna jistá.",
            _ => "Hledáme ochotného dobrovolníka, který by nám pomohl s vyřešením tohoto úkolu. Všechny podrobnosti doladíme po přijetí úkolu."
        };
    }

private static string[] GetPhotosForCategory(string categoryName)
    {
        return categoryName switch
        {
            "Sběr odpadků" or "Odstranění lokálního znečištění" => new[] {
                "https://images.unsplash.com/photo-1532996122724-e3c354a0b15b?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1611284446314-60a58ac0deb9?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1567393528677-d6df27d999e5?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1618477461853-cf6ed80faba5?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1595278069441-2cf29f8005a4?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1611284446314-60a58ac0deb9?auto=format&fit=crop&w=800&q=80"
            },
            "Venčení psů" => new[] {
                "https://images.unsplash.com/photo-1516734212186-a967f81ad0d7?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1534361960057-19889db9621e?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1583337130417-3346a1be7dee?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1548199973-03cce0bbc87b?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1517849845537-4d257902454a?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1530281700549-e82e7bf110d6?auto=format&fit=crop&w=800&q=80"
            },
            "Hlídání zvířat" => new[] {
                "https://images.unsplash.com/photo-1514888286974-6c03e2ca1dba?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1450778869180-41d0601e046e?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1573865526739-10659fef78a5?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1543852786-1cf6624b9987?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1533738363-b7f9aef128ce?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1518717758536-85ae29035b6d?auto=format&fit=crop&w=800&q=80"
            },
            "Matematika a logika" or "Cizí jazyky a konverzace" => new[] {
                "https://images.unsplash.com/photo-1434030216411-0b793f4b4173?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1454165833767-027ffea9e77b?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1522202176988-66273c2fd55f?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1503676260728-1c00da094a0b?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1497633762265-9d179a990aa6?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1509062522246-3755977927d7?auto=format&fit=crop&w=800&q=80"
            },
            "IT a elektrotechnika" => new[] {
                "https://images.unsplash.com/photo-1484417894907-623942c8ee29?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1519389950473-47ba0277781c?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1550751827-4bd374c3f58b?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1517694712202-14dd9538aa97?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1531297484001-80022131f5a1?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1581091226825-a6a2a5aee158?auto=format&fit=crop&w=800&q=80"
            },
            "Základní nákupy a léky" or "Vyzvednutí zásilek a pošta" => new[] {
                "https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1573855619003-97b4799dcd8b?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1580913428735-bd3c269d6a82?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1506617564039-2f3b650ad701?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1604719312566-8912e9227c6a?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1586015555705-eb1f3e056ee2?auto=format&fit=crop&w=800&q=80"
            },
            "Sekání trávy a prostřih" or "Zalévání" or "Údržba komunitních prostor" => new[] {
                "https://images.unsplash.com/photo-1523348837708-15d4a09cfac2?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1585320806297-9794b3e4eeae?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1558905619-17355266324d?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1592419044706-39796d40f98c?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1599591037488-dc55759dc74d?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1598902133033-5e732df1bb17?auto=format&fit=crop&w=800&q=80"
            },
            "Odklízení sněhu a náledí" => new[] {
                "https://images.unsplash.com/photo-1453306458620-5bbef13a5bca?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1483921020237-2ff51e8e4b22?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1517299321609-52687d1bc55a?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1478265409102-310505c117f9?auto=format&fit=crop&w=800&q=80"
            },
            "Nátěry a renovace" or "Oprava drobného mobiliáře" or "Dětská hřiště a sportoviště" => new[] {
                "https://images.unsplash.com/photo-1581244277943-fe4a9c777189?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1504328345606-18bbc8c9d7d1?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1595814433015-e6f5cdabd197?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1589939705384-5185137a7f0f?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1562259949-e8e7689d7828?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1584622650114-ade6020a719f?auto=format&fit=crop&w=800&q=80"
            },
            _ => new[] {
                "https://images.unsplash.com/photo-1502082553048-f009c37129b9?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1464822759023-fed622ff2c3b?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1501785888041-af3ef285b470?auto=format&fit=crop&w=800&q=80"
            }
        };
    }
}
