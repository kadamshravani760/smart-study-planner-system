using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SmartStudyPlannerSystem.Models;

namespace SmartStudyPlannerSystem.Controllers;

public class AdminTasksController : Controller
{
    private readonly IConfiguration _configuration;

    public AdminTasksController(IConfiguration configuration)
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
        if (HttpContext.Session.GetInt32("UserId") == null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!IsAdmin())
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var tasks = new List<SmartStudyPlannerSystem.Models.Task>();

        try
        {
            await using var connection =
                new SqlConnection(ConnectionString);

            await connection.OpenAsync();

            const string sql = """
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
                ORDER BY DueDate ASC, TaskId DESC;
                """;

            await using var command =
                new SqlCommand(sql, connection);

            await using var reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                tasks.Add(new SmartStudyPlannerSystem.Models.Task
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

            return View(tasks);
        }
        catch (SqlException)
        {
            TempData["Error"] =
                "Unable to load student tasks.";

            return View(tasks);
        }
        catch
        {
            TempData["Error"] =
                "Something went wrong while loading tasks.";

            return View(tasks);
        }
    }
}