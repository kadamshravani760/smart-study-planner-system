using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SmartStudyPlannerSystem.Models;

namespace SmartStudyPlannerSystem.Controllers;

public class AdminStudyPlansController : Controller
{
    private readonly IConfiguration _configuration;

    public AdminStudyPlansController(IConfiguration configuration)
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

        var plans = new List<StudyPlan>();

        try
        {
            await using var connection =
                new SqlConnection(ConnectionString);

            await connection.OpenAsync();

            const string sql = """
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
                ORDER BY StudyDate DESC, StudyPlanId DESC;
                """;

            await using var command =
                new SqlCommand(sql, connection);

            await using var reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                plans.Add(new StudyPlan
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

            return View(plans);
        }
        catch (SqlException)
        {
            TempData["Error"] =
                "Unable to load study plans.";

            return View(plans);
        }
        catch
        {
            TempData["Error"] =
                "Something went wrong while loading study plans.";

            return View(plans);
        }
    }
}