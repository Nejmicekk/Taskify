using Microsoft.EntityFrameworkCore;
using Taskify.Models;
using Taskify.Models.Enums.Achievements;

namespace Taskify.Data;

public static class AchievementSeeder
{
    public static async Task SeedAchievementsAsync(IServiceProvider serviceProvider)
    {
        using var context = new ApplicationDbContext(
            serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>());

        var achievements = new List<Achievement>();

        // A) Počet splněných úkolů
        achievements.Add(new Achievement { Name = "Dobrý skutek", Description = "Splň svůj první úkol", Category = AchievementCategory.TasksCompleted, TargetValue = 1, XpReward = 50, Rarity = AchievementRarity.Common });
        achievements.Add(new Achievement { Name = "Pomocná ruka", Description = "Splň 20 úkolů", Category = AchievementCategory.TasksCompleted, TargetValue = 20, XpReward = 200, Rarity = AchievementRarity.Common });
        achievements.Add(new Achievement { Name = "Spasitel", Description = "Splň 50 úkolů", Category = AchievementCategory.TasksCompleted, TargetValue = 50, XpReward = 500, Rarity = AchievementRarity.Rare });
        achievements.Add(new Achievement { Name = "Lokální hrdina", Description = "Splň 100 úkolů", Category = AchievementCategory.TasksCompleted, TargetValue = 100, XpReward = 1000, Rarity = AchievementRarity.Epic });
        achievements.Add(new Achievement { Name = "Státní legenda", Description = "Splň 250 úkolů", Category = AchievementCategory.TasksCompleted, TargetValue = 250, XpReward = 2500, Rarity = AchievementRarity.Legendary });

        // B) Rychlost (do 24h)
        achievements.Add(new Achievement { Name = "Blesk", Description = "Splň 1x úkol do 24 hodin", Category = AchievementCategory.CompletionSpeed, TargetValue = 1, XpReward = 500, Rarity = AchievementRarity.Rare });
        achievements.Add(new Achievement { Name = "Sprinter", Description = "Splň 20x úkol do 24 hodin", Category = AchievementCategory.CompletionSpeed, TargetValue = 5, XpReward = 2000, Rarity = AchievementRarity.Epic });
        achievements.Add(new Achievement { Name = "Teleport", Description = "Splň 50x úkol do 24 hodin", Category = AchievementCategory.CompletionSpeed, TargetValue = 20, XpReward = 5000, Rarity = AchievementRarity.Legendary });

        // C) Streak (1 úkol týdně)
        achievements.Add(new Achievement { Name = "Vytrvalec", Description = "Udrž si streak aspoň jeden úkol týdně po dobu 2 týdnů", Category = AchievementCategory.WeeklyStreak, TargetValue = 2, XpReward = 150, Rarity = AchievementRarity.Common });
        achievements.Add(new Achievement { Name = "Nezastavitelný", Description = "Udrž si streak po dobu 12 týdnů", Category = AchievementCategory.WeeklyStreak, TargetValue = 12, XpReward = 1000, Rarity = AchievementRarity.Rare });
        achievements.Add(new Achievement { Name = "Perpetuum Mobile", Description = "Udrž si streak po dobu 24 týdnů", Category = AchievementCategory.WeeklyStreak, TargetValue = 24, XpReward = 2500, Rarity = AchievementRarity.Epic });

        // D) Tvorba úkolů
        achievements.Add(new Achievement { Name = "Iniciátor", Description = "Vytvoř svůj první úkol", Category = AchievementCategory.TasksCreated, TargetValue = 1, XpReward = 50, Rarity = AchievementRarity.Common });
        achievements.Add(new Achievement { Name = "Organizátor", Description = "Vytvoř 10 úkolů", Category = AchievementCategory.TasksCreated, TargetValue = 10, XpReward = 250, Rarity = AchievementRarity.Common });
        achievements.Add(new Achievement { Name = "Hlas lidu", Description = "Vytvoř 25 úkolů", Category = AchievementCategory.TasksCreated, TargetValue = 25, XpReward = 600, Rarity = AchievementRarity.Rare });
        achievements.Add(new Achievement { Name = "Starosta", Description = "Vytvoř 50 úkolů", Category = AchievementCategory.TasksCreated, TargetValue = 50, XpReward = 1200, Rarity = AchievementRarity.Epic });
        achievements.Add(new Achievement { Name = "Architekt komunity", Description = "Vytvoř 100 úkolů", Category = AchievementCategory.TasksCreated, TargetValue = 100, XpReward = 3000, Rarity = AchievementRarity.Legendary });

        // F) Level milník
        achievements.Add(new Achievement { Name = "Učeň", Description = "Dosáhni Levelu 2", Category = AchievementCategory.LevelReached, TargetValue = 2, XpReward = 100, Rarity = AchievementRarity.Common });
        achievements.Add(new Achievement { Name = "Pokročilý", Description = "Dosáhni Levelu 5", Category = AchievementCategory.LevelReached, TargetValue = 5, XpReward = 300, Rarity = AchievementRarity.Common });
        achievements.Add(new Achievement { Name = "Expert", Description = "Dosáhni Levelu 10", Category = AchievementCategory.LevelReached, TargetValue = 10, XpReward = 700, Rarity = AchievementRarity.Rare });
        achievements.Add(new Achievement { Name = "Mistr", Description = "Dosáhni Levelu 25", Category = AchievementCategory.LevelReached, TargetValue = 25, XpReward = 2000, Rarity = AchievementRarity.Epic });
        achievements.Add(new Achievement { Name = "Velmistr", Description = "Dosáhni Levelu 50", Category = AchievementCategory.LevelReached, TargetValue = 50, XpReward = 5000, Rarity = AchievementRarity.Legendary });

        // G) Reputace milník
        achievements.Add(new Achievement { Name = "Slušný občan", Description = "Získej 100 bodů reputace", Category = AchievementCategory.ReputationPoints, TargetValue = 100, XpReward = 100, Rarity = AchievementRarity.Common });
        achievements.Add(new Achievement { Name = "Známá tvář", Description = "Získej 500 bodů reputace", Category = AchievementCategory.ReputationPoints, TargetValue = 500, XpReward = 400, Rarity = AchievementRarity.Common });
        achievements.Add(new Achievement { Name = "Vlivná osoba", Description = "Získej 2000 bodů reputace", Category = AchievementCategory.ReputationPoints, TargetValue = 2000, XpReward = 1500, Rarity = AchievementRarity.Rare });
        achievements.Add(new Achievement { Name = "Celebrita", Description = "Získej 5000 bodů reputace", Category = AchievementCategory.ReputationPoints, TargetValue = 5000, XpReward = 4000, Rarity = AchievementRarity.Epic });
        achievements.Add(new Achievement { Name = "Bůh Taskify", Description = "Získej 10000 bodů reputace", Category = AchievementCategory.ReputationPoints, TargetValue = 10000, XpReward = 10000, Rarity = AchievementRarity.Legendary });

        // H) Secret (Special)
        achievements.Add(new Achievement { Name = "Ranní ptáče", Description = "Splň úkol mezi 04:00 a 07:00", Category = AchievementCategory.Special, TargetValue = 1, XpReward = 250, IsSecret = true, Rarity = AchievementRarity.Rare });
        achievements.Add(new Achievement { Name = "Noční hrdina", Description = "Proveď akci mezi 00:00 a 03:00", Category = AchievementCategory.Special, TargetValue = 1, XpReward = 250, IsSecret = true, Rarity = AchievementRarity.Rare });
        achievements.Add(new Achievement { Name = "První krok", Description = "Přidej si profilovou fotku", Category = AchievementCategory.Special, TargetValue = 1, XpReward = 100, IsSecret = true, Rarity = AchievementRarity.Common });
        achievements.Add(new Achievement { Name = "Paparazzi", Description = "Přidej 10 fotek k jednomu úkolu", Category = AchievementCategory.Special, TargetValue = 10, XpReward = 500, IsSecret = true, Rarity = AchievementRarity.Rare });
        achievements.Add(new Achievement { Name = "Na poslední chvíli", Description = "Splň úkol v den deadline", Category = AchievementCategory.Special, TargetValue = 1, XpReward = 300, IsSecret = true, Rarity = AchievementRarity.Common });

        // Mapa existujících SVG souborů
        var svgMapping = new Dictionary<string, string>
        {
            { "Dobrý skutek", "dobrý_skutek.svg" },
            { "Pomocná ruka", "pomocná_ruka.svg" },
            { "Spasitel", "spasitel.svg" },
            { "Lokální hrdina", "lokální_hrdina.svg" },
            { "Státní legenda", "státní_legenda.svg" },
            { "Blesk", "blesk.svg" },
            { "Sprinter", "sprinter.svg" },
            { "Expresní doručení", "expresní_doručení.svg" },
            { "Rychle a zběsile", "rychle_a_zběsile.svg" },
            { "Teleport", "teleport.svg" },
            { "Noční hrdina", "night_hero.svg" },
            { "První krok", "první_krok.svg" }
        };

        foreach (var a in achievements)
        {
            if (svgMapping.TryGetValue(a.Name, out var svgFile))
            {
                a.IconUrl = $"/images/achievements/{svgFile}";
            }
            else
            {
                a.IconUrl = "/images/achievements/placeholder.svg";
            }
            
            var existing = await context.Achievements.FirstOrDefaultAsync(dbA => dbA.Name == a.Name);
            if (existing == null)
            {
                context.Achievements.Add(a);
            }
            else
            {
                // Aktualizace existujícího (ikonka, reward atd.)
                existing.IconUrl = a.IconUrl;
                existing.Description = a.Description;
                existing.TargetValue = a.TargetValue;
                existing.XpReward = a.XpReward;
                existing.Rarity = a.Rarity;
                existing.IsSecret = a.IsSecret;
                existing.Category = a.Category;
            }
        }

        await context.SaveChangesAsync();
    }
}
