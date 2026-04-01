using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Taskify.Models.Enums;

namespace Taskify.Models;

public class Notification
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }

    public string? SenderId { get; set; }

    [ForeignKey(nameof(SenderId))]
    public virtual User? Sender { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    public string? TargetUrl { get; set; }

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Required]
    public NotificationPriority Priority { get; set; }

    [Required]
    public NotificationType Type { get; set; } = NotificationType.General;
}
