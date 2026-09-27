using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SmartStudyPlannerSystem.Models;

namespace SmartStudyPlannerSystem.Controllers;

public class AdminDashboardController : Controller
{
    private readonly IConfiguration _configuration;

    public AdminDashboardController(IConfiguration configuration)
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


    // =========================================================
    // ADMIN DASHBOARD
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (HttpContext.Session.GetInt32("UserId") == null)
            return RedirectToAction("Login", "Account");

        if (!IsAdmin())
            return RedirectToAction("Index", "Dashboard");

        var model = new AdminDashboardViewModel();

        try
        {
            await using var connection =
                new SqlConnection(ConnectionString);

            await connection.OpenAsync();


            // Total Students
            const string studentsSql = """
                SELECT COUNT(1)
                FROM dbo.Users
                WHERE UserType = 'Student';
                """;

            await using (var command =
                new SqlCommand(studentsSql, connection))
            {
                model.TotalStudents =
                    Convert.ToInt32(
                        await command.ExecuteScalarAsync());
            }


            // Total Study Plans
            const string plansSql = """
                SELECT COUNT(1)
                FROM dbo.StudyPlans;
                """;

            await using (var command =
                new SqlCommand(plansSql, connection))
            {
                model.TotalStudyPlans =
                    Convert.ToInt32(
                        await command.ExecuteScalarAsync());
            }


            // Total Tasks
            const string tasksSql = """
                SELECT COUNT(1)
                FROM dbo.StudyTasks;
                """;

            await using (var command =
                new SqlCommand(tasksSql, connection))
            {
                model.TotalTasks =
                    Convert.ToInt32(
                        await command.ExecuteScalarAsync());
            }


            // Completed Tasks
            const string completedSql = """
                SELECT COUNT(1)
                FROM dbo.StudyTasks
                WHERE Status = 'Completed';
                """;

            await using (var command =
                new SqlCommand(completedSql, connection))
            {
                model.CompletedTasks =
                    Convert.ToInt32(
                        await command.ExecuteScalarAsync());
            }


            // Pending / In Progress Tasks
            const string pendingSql = """
                SELECT COUNT(1)
                FROM dbo.StudyTasks
                WHERE ISNULL(Status, 'Pending') <> 'Completed';
                """;

            await using (var command =
                new SqlCommand(pendingSql, connection))
            {
                model.PendingTasks =
                    Convert.ToInt32(
                        await command.ExecuteScalarAsync());
            }


            return View(model);
        }
        catch (SqlException)
        {
            TempData["Error"] =
                "Unable to load admin dashboard.";

            return View(model);
        }
    }
}