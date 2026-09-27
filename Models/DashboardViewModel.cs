namespace SmartStudyPlannerSystem.Models;

public class DashboardViewModel
{
    public string UserName { get; set; } = "";

    public int StudyPlanCount { get; set; }

    public int TaskCount { get; set; }

    public int CompletedTaskCount { get; set; }

    public int PendingTaskCount { get; set; }

    public int ProgressPercent { get; set; }
}