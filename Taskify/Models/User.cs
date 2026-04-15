using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskify.Models;

public class User : IdentityUser
{
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(255)]
    public string? ProfilePictureUrl { get; set; }
    
    [StringLength(500)]
    public string? Bio { get; set; }
    
    // Statistiky
    public int Reputation { get; set; } = 0;
    public int Level { get; set; } = 1;
    public int Points { get; set; } = 0;
    
    // Čítače pro achievementy
    public int TotalTasksCompleted { get; set; } = 0;
    public int TotalTasksCreated { get; set; } = 0;
    public int CurrentWeeklyStreak { get; set; } = 0;
    public int QuickCompletionsCount { get; set; } = 0;
    public DateTime? LastStreakUpdate { get; set; }
    
    public DateTime? LastEmailChangeDate { get; set; }

    // E-mailová oznámení
    public bool EnableEmailNotifications { get; set; } = false;
    public bool EmailOnTaskUpdates { get; set; } = false; // Přijetí, dokončení, zrušení úkolu
    public bool EmailOnTaskResults { get; set; } = false;  // Schválení, smazání
    public bool EmailOnAccountSecurity { get; set; } = false; // Pozastavení účtu, změny hesla

    // -- VAZBY --
    // Seznam úkolů, které uživatel založil
    [InverseProperty("CreatedBy")]
    public ICollection<TaskItem> CreatedTasks { get; set; } = new List<TaskItem>();
    
    // Seznam úkolů, které uživatel plní (má přiřazené)
    [InverseProperty("AssignedTo")]
    public ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();
    
    // Co uživatel reportnul
    public virtual ICollection<Report> SentReports { get; set; } = new List<Report>();

    // Achievementy uživatele
    public virtual ICollection<UserAchievement> Achievements { get; set; } = new List<UserAchievement>();
    }