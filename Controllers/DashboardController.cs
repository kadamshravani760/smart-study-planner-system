using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SmartStudyPlannerSystem.Models;

namespace SmartStudyPlannerSystem.Controllers;

public class DashboardController : Controller
{
    private readonly IConfiguration _configuration;

    public DashboardController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private string ConnectionString =>
        _configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "DefaultConnection is not configured.");

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        int? userId = HttpContext.Session.GetInt32("UserId");

        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var model = new DashboardViewModel
        {
            UserName =
                HttpContext.Session.GetString("UserName")
                ?? "Student"
        };

        try
        {
            await using var connection =
                new SqlConnection(ConnectionString);

            await connection.OpenAsync();

            // =========================
            // STUDY PLAN COUNT
            // =========================
            const string planSql = """
                SELECT COUNT(*)
                FROM dbo.StudyPlans
                WHERE UserId = @UserId;
                """;

            await using var planCommand =
                new SqlCommand(planSql, connection);

            planCommand.Parameters.Add(
                new SqlParameter(
                    "@UserId",
                    System.Data.SqlDbType.Int)
                {
                    Value = userId.Value
                });

            model.StudyPlanCount =
                Convert.ToInt32(
                    await planCommand.ExecuteScalarAsync());

            // =========================
            // TASK COUNTS
            // =========================
            const string taskSql = """
                SELECT
                    COUNT(*) AS TotalTasks,
                    SUM(CASE
                        WHEN LOWER(Status) = 'completed'
                        THEN 1 ELSE 0
                    END) AS CompletedTasks,
                    SUM(CASE
                        WHEN LOWER(Status) <> 'completed'
                             OR Status IS NULL
                        THEN 1 ELSE 0
                    END) AS PendingTasks
                FROM dbo.StudyTasks
                WHERE UserId = @UserId;
                """;

            await using var taskCommand =
                new SqlCommand(taskSql, connection);

            taskCommand.Parameters.Add(
                new SqlParameter(
                    "@UserId",
                    System.Data.SqlDbType.Int)
                {
                    Value = userId.Value
                });

            await using var reader =
                await taskCommand.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                model.TaskCount =
                    reader.IsDBNull(0)
                        ? 0
                        : reader.GetInt32(0);

                model.CompletedTaskCount =
                    reader.IsDBNull(1)
                        ? 0
                        : reader.GetInt32(1);

                model.PendingTaskCount =
                    reader.IsDBNull(2)
                        ? 0
                        : reader.GetInt32(2);
            }

            // =========================
            // PROGRESS
            // =========================
            if (model.TaskCount > 0)
            {
                model.ProgressPercent =
                    (int)Math.Round(
                        (double)model.CompletedTaskCount
                        / model.TaskCount
                        * 100);
            }
            else
            {
                model.ProgressPercent = 0;
            }

            return View(model);
        }
        catch (SqlException)
        {
            TempData["Error"] =
                "Unable to load dashboard data.";

            return View(model);
        }
        catch
        {
            TempData["Error"] =
                "Something went wrong while loading the dashboard.";

            return View(model);
        }
    }
}