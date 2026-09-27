using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SmartStudyPlannerSystem.Models;

namespace SmartStudyPlannerSystem.Controllers;

public class RemindersController : Controller
{
    private readonly IConfiguration _configuration;

    public RemindersController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private string ConnectionString =>
        _configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "DefaultConnection is not configured.");

    private bool IsAdmin()
    {
        string? role = HttpContext.Session.GetString("Role");

        return !string.IsNullOrWhiteSpace(role) &&
               role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        int? userId = HttpContext.Session.GetInt32("UserId");

        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var model = new ReminderViewModel();

        bool admin = IsAdmin();

        try
        {
            await using var connection =
                new SqlConnection(ConnectionString);

            await connection.OpenAsync();


            // =====================================================
            // UPCOMING TASKS
            // Admin -> All Students
            // Student -> Own Tasks Only
            // =====================================================

            string tasksSql;

            if (admin)
            {
                tasksSql = """
                    SELECT
                        TaskId,
                        UserId,
                        TaskTitle,
                        Subject,
                        DueDate,
                        Status,
                        Priority,
                        CreatedAt
                    FROM dbo.StudyTasks
                    WHERE ISNULL(Status, 'Pending') <> 'Completed'
                      AND DueDate >= CAST(GETDATE() AS DATE)
                    ORDER BY DueDate ASC, TaskId DESC;
                    """;
            }
            else
            {
                tasksSql = """
                    SELECT
                        TaskId,
                        UserId,
                        TaskTitle,
                        Subject,
                        DueDate,
                        Status,
                        Priority,
                        CreatedAt
                    FROM dbo.StudyTasks
                    WHERE UserId = @UserId
                      AND ISNULL(Status, 'Pending') <> 'Completed'
                      AND DueDate >= CAST(GETDATE() AS DATE)
                    ORDER BY DueDate ASC, TaskId DESC;
                    """;
            }


            await using (var command =
                new SqlCommand(tasksSql, connection))
            {
                if (!admin)
                {
                    command.Parameters.Add(
                        new SqlParameter(
                            "@UserId",
                            System.Data.SqlDbType.Int)
                        {
                            Value = userId.Value
                        });
                }

                await using var reader =
                    await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    model.UpcomingTasks.Add(
                        new SmartStudyPlannerSystem.Models.Task
                        {
                            TaskId = reader.GetInt32(0),

                            UserId = reader.GetInt32(1),

                            TaskTitle = reader.GetString(2),

                            Subject = reader.IsDBNull(3)
                                ? ""
                                : reader.GetString(3),

                            DueDate = reader.GetDateTime(4),

                            Status = reader.IsDBNull(5)
                                ? "Pending"
                                : reader.GetString(5),

                            Priority = reader.IsDBNull(6)
                                ? "Medium"
                                : reader.GetString(6),

                            CreatedAt = reader.GetDateTime(7)
                        });
                }
            }


            // =====================================================
            // UPCOMING STUDY PLANS
            // Admin -> All Students
            // Student -> Own Plans Only
            // =====================================================

            string plansSql;

            if (admin)
            {
                plansSql = """
                    SELECT
                        StudyPlanId,
                        UserId,
                        Title,
                        StudyDate,
                        Description,
                        Subject,
                        Topic,
                        StartTime,
                        EndTime,
                        Notes,
                        CreatedAt
                    FROM dbo.StudyPlans
                    WHERE StudyDate >= CAST(GETDATE() AS DATE)
                    ORDER BY StudyDate ASC, StudyPlanId DESC;
                    """;
            }
            else
            {
                plansSql = """
                    SELECT
                        StudyPlanId,
                        UserId,
                        Title,
                        StudyDate,
                        Description,
                        Subject,
                        Topic,
                        StartTime,
                        EndTime,
                        Notes,
                        CreatedAt
                    FROM dbo.StudyPlans
                    WHERE UserId = @UserId
                      AND StudyDate >= CAST(GETDATE() AS DATE)
                    ORDER BY StudyDate ASC, StudyPlanId DESC;
                    """;
            }


            await using (var command =
                new SqlCommand(plansSql, connection))
            {
                if (!admin)
                {
                    command.Parameters.Add(
                        new SqlParameter(
                            "@UserId",
                            System.Data.SqlDbType.Int)
                        {
                            Value = userId.Value
                        });
                }

                await using var reader =
                    await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    model.UpcomingPlans.Add(
                        new StudyPlan
                        {
                            StudyPlanId = reader.GetInt32(0),

                            UserId = reader.GetInt32(1),

                            Title = reader.GetString(2),

                            StudyDate = reader.GetDateTime(3),

                            Description = reader.IsDBNull(4)
                                ? ""
                                : reader.GetString(4),

                            Subject = reader.IsDBNull(5)
                                ? ""
                                : reader.GetString(5),

                            Topic = reader.IsDBNull(6)
                                ? ""
                                : reader.GetString(6),

                            StartTime = reader.IsDBNull(7)
                                ? null
                                : reader.GetTimeSpan(7),

                            EndTime = reader.IsDBNull(8)
                                ? null
                                : reader.GetTimeSpan(8),

                            Notes = reader.IsDBNull(9)
                                ? ""
                                : reader.GetString(9),

                            CreatedAt = reader.GetDateTime(10)
                        });
                }
            }


            return View(model);
        }
        catch (SqlException)
        {
            TempData["Error"] =
                "Unable to load reminders.";

            return View(model);
        }
        catch
        {
            TempData["Error"] =
                "Something went wrong while loading reminders.";

            return View(model);
        }
    }
}