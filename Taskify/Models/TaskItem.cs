using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TaskStatus = Taskify.Models.Enums.TaskStatus;

namespace Taskify.Models;

public class TaskItem
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Nadpis je povinný")]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Popis je povinný, aby ostatní věděli, co přesně dělat.")]
    public string Description { get; set; } = string.Empty;

    public int RewardPoints { get; set; }

    [Required]
    public TaskStatus Status { get; set; } = TaskStatus.Open;
    
    public AddressInfo Location { get; set; } = new AddressInfo();
    
    public ICollection<TaskImage> Images { get; set; } = new List<TaskImage>();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? Deadline { get; set; }

    // Vazby na kategorie
    [Required(ErrorMessage = "Kategorie je povinná")]
    public int CategoryId { get; set; }
    
    [ForeignKey("CategoryId")]
    public Category? Category { get; set; }

    // Kdo to založil
    [Required]
    public string CreatedById { get; set; } = string.Empty;
    
    [ForeignKey("CreatedById")]
    [InverseProperty("CreatedTasks")]
    public User? CreatedBy { get; set; }

    // Kdo to plní
    public string? AssignedToId { get; set; }
    
    [ForeignKey("AssignedToId")]
    [InverseProperty("AssignedTasks")]
    public User? AssignedTo { get; set; }

    public DateTime? SubmittedAt { get; set; }
}