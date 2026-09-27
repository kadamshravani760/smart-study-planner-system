using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SmartStudyPlannerSystem.Models;

namespace SmartStudyPlannerSystem.Controllers;

public class AdminStudentsController : Controller
{
    private readonly IConfiguration _configuration;

    public AdminStudentsController(IConfiguration configuration)
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

        var students = new List<User>();

        try
        {
            await using var connection =
                new SqlConnection(ConnectionString);

            await connection.OpenAsync();

            const string sql = """
                SELECT
                    UserId,
                    FullName,
                    Email,
                    UserType,
                    Role
                FROM dbo.Users
                WHERE UserType = 'Student'
                ORDER BY UserId DESC;
                """;

            await using var command =
                new SqlCommand(sql, connection);

            await using var reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                students.Add(new User
                {
                    UserId = reader.GetInt32(0),
                    FullName = reader.GetString(1),
                    Email = reader.GetString(2),
                    UserType = reader.GetString(3),
                    Role = reader.GetString(4)
                });
            }

            return View(students);
        }
        catch (SqlException)
        {
            TempData["Error"] =
                "Unable to load student information.";

            return View(students);
        }
        catch
        {
            TempData["Error"] =
                "Something went wrong while loading students.";

            return View(students);
        }
    }
}