using System.ComponentModel.DataAnnotations;

namespace SmartStudyPlannerSystem.Models;

public class StudyPlan
{
    public int StudyPlanId { get; set; }

    public int UserId { get; set; }

    [Required(ErrorMessage = "Study plan title is required.")]
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
    public string Title { get; set; } = "";

    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
    public string Subject { get; set; } = "";

    [StringLength(300, ErrorMessage = "Topic cannot exceed 300 characters.")]
    public string Topic { get; set; } = "";

    [Required(ErrorMessage = "Study date is required.")]
    [DataType(DataType.Date)]
    public DateTime StudyDate { get; set; } = DateTime.Today;

    [DataType(DataType.Time)]
    public TimeSpan? StartTime { get; set; }

    [DataType(DataType.Time)]
    public TimeSpan? EndTime { get; set; }

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
    public string Description { get; set; } = "";

    [StringLength(3000, ErrorMessage = "Notes cannot exceed 3000 characters.")]
    public string Notes { get; set; } = "";

    public DateTime CreatedAt { get; set; }
}