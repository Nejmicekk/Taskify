using Taskify.Models;

namespace Taskify.Services;

public interface IUserService
{
    Task AddXpAsync(string userId, int xpAmount);
    
    Task UpdateLevelAsync(User user);
}
