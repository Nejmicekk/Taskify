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
    
    public DateTime? LastEmailChangeDate { get; set; }

    // E-mailová oznámení
    public bool EnableEmailNotifications { get; set; } = true;
    public bool EmailOnTaskUpdates { get; set; } = true; // Přijetí, dokončení, zrušení úkolu
    public bool EmailOnTaskResults { get; set; } = true;  // Schválení, smazání
    public bool EmailOnAccountSecurity { get; set; } = true; // Pozastavení účtu, změny hesla

    // -- VAZBY --
    // Seznam úkolů, které uživatel založil
    [InverseProperty("CreatedBy")]
    public ICollection<TaskItem> CreatedTasks { get; set; } = new List<TaskItem>();
    
    // Seznam úkolů, které uživatel plní (má přiřazené)
    [InverseProperty("AssignedTo")]
    public ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();
    
    // Co uživatel reportnul
    public virtual ICollection<Report> SentReports { get; set; } = new List<Report>();
}