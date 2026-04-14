using Taskify.Models;

namespace Taskify.Services;

public interface ILevelingService
{
    bool AddExperience(User user, int amount);
    
    int GetExperienceRequiredForLevel(int level);
}