using System.ComponentModel.DataAnnotations;

namespace SmartStudyPlannerSystem.Models;

public class Task
{
    public int TaskId { get; set; }

    public int UserId { get; set; }

    [Required(ErrorMessage = "Task title is required.")]
    [StringLength(200, ErrorMessage = "Task title cannot exceed 200 characters.")]
    public string TaskTitle { get; set; } = "";

    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
    public string Subject { get; set; } = "";

    [Required(ErrorMessage = "Due date is required.")]
    [DataType(DataType.Date)]
    public DateTime DueDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Status is required.")]
    public string Status { get; set; } = "Pending";

    [Required(ErrorMessage = "Priority is required.")]
    public string Priority { get; set; } = "Medium";

    public DateTime CreatedAt { get; set; }
}