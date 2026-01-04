using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskify.Models;

public class Category
{
    [Key]
    public int Id { get; set; }

    // Pro stromovou strukturu
    public int? ParentId { get; set; }
    
    [ForeignKey("ParentId")]
    public Category? ParentCategory { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    public string? IconUrl { get; set; }
    
    public int LevelNumber { get; set; }

    // Vazba na úkoly
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}