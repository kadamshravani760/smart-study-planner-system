using System;
using System.Collections.Generic;

namespace SmartStudyPlannerSystem.Models;

public class ReminderViewModel
{
    public List<Task> UpcomingTasks { get; set; } = new();

    public List<StudyPlan> UpcomingPlans { get; set; } = new();
}