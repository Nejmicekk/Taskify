namespace Taskify.Models;

public class TaskImage
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    
    public int TaskItemId { get; set; }
    public TaskItem? TaskItem { get; set; }
}