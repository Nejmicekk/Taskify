using Taskify.Models;

namespace Taskify.Services;

public class LevelingService : ILevelingService
{
    public bool AddExperience(User user, int amount)
    {
        user.Points += amount;
        bool leveledUp = false;
        double pointsNeeded = GetExperienceRequiredForLevel(user.Level);

        while (user.Points >= pointsNeeded)
        {
            user.Points -= (int)pointsNeeded;
            user.Level++;
            leveledUp = true;
            pointsNeeded = GetExperienceRequiredForLevel(user.Level);
        }

        return leveledUp;
    }
    
    public int GetExperienceRequiredForLevel(int level)
    {
        return (int)(100 * Math.Pow(1.1, level - 1));
    }
}