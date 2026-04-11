using Taskify.Models;

namespace Taskify.Services;

public interface IUserService
{
    /// <summary>
    /// Přidá uživateli body (XP), přepočítá level a zkontroluje achievementy za level-up.
    /// </summary>
    Task AddXpAsync(string userId, int xpAmount);

    /// <summary>
    /// Přepočítá level uživatele na základě jeho aktuálních bodů.
    /// </summary>
    Task UpdateLevelAsync(User user);

    /// <summary>
    /// Změní reputaci uživatele a zkontroluje achievementy.
    /// </summary>
    Task ChangeReputationAsync(string userId, int amount);
}
