using Taskify.Models.Enums;

namespace Taskify.Models;

public class Report
{
    public int Id { get; set; }
    
    public string ReporterId { get; set; } = null!;
    public User Reporter { get; set; } = null!;
    
    public int TaskItemId { get; set; }
    public TaskItem TaskItem { get; set; } = null!;
    
    public ReportReason Reason { get; set; }
    
    public string? Description { get; set; }
    
    public string? AdminNote { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsResolved { get; set; } = false;
}