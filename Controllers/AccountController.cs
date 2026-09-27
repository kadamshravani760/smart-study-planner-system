using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SmartStudyPlannerSystem.Models;

namespace SmartStudyPlannerSystem.Controllers;

public class AccountController : Controller
{
    private readonly IConfiguration _configuration;

    public AccountController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private string ConnectionString =>
        _configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "DefaultConnection is not configured.");

    // =========================
    // LOGIN - GET
    // =========================
    [HttpGet]
    public IActionResult Login()
    {
        if (HttpContext.Session.GetInt32("UserId") != null)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View();
    }

    // =========================
    // LOGIN - POST
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        string email = model.Email.Trim().ToLowerInvariant();

        try
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            const string sql = """
                SELECT UserId, FullName, UserType, Role
                FROM dbo.Users
                WHERE LOWER(Email) = @Email
                  AND Password = @Password;
                """;

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.Add(
                new SqlParameter("@Email", System.Data.SqlDbType.NVarChar, 150)
                {
                    Value = email
                });

            command.Parameters.Add(
                new SqlParameter("@Password", System.Data.SqlDbType.NVarChar, 500)
                {
                    Value = model.Password
                });

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                int userId = reader.GetInt32(0);
                string fullName = reader.GetString(1);
                string userType = reader.GetString(2);
                string role = reader.GetString(3);

                HttpContext.Session.SetInt32("UserId", userId);
                HttpContext.Session.SetString("UserName", fullName);
                HttpContext.Session.SetString("UserType", userType);
                HttpContext.Session.SetString("Role", role);

                return RedirectToAction("Index", "Dashboard");
            }

            ModelState.AddModelError(
                "",
                "Invalid email or password.");

            return View(model);
        }
        catch (SqlException)
        {
            ModelState.AddModelError(
                "",
                "Unable to connect to the database. Please try again.");

            return View(model);
        }
        catch
        {
            ModelState.AddModelError(
                "",
                "Something went wrong. Please try again.");

            return View(model);
        }
    }

    // =========================
    // REGISTER - GET
    // =========================
    [HttpGet]
    public IActionResult Register()
    {
        if (HttpContext.Session.GetInt32("UserId") != null)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        string fullName = model.FullName.Trim();
        string email = model.Email.Trim().ToLowerInvariant();

        try
        {
            await using var connection = new SqlConnection(ConnectionString);

            // Prevent endless database waiting
            connection.ConnectionString += ";Connect Timeout=10";

            await connection.OpenAsync();

            // Check existing email
            const string checkSql = """
            SELECT COUNT(1)
            FROM dbo.Users
            WHERE LOWER(Email) = @Email;
            """;

            await using (var checkCommand =
                new SqlCommand(checkSql, connection))
            {
                checkCommand.CommandTimeout = 10;

                checkCommand.Parameters.Add(
                    new SqlParameter(
                        "@Email",
                        System.Data.SqlDbType.NVarChar, 150)
                    {
                        Value = email
                    });

                int existingUser = Convert.ToInt32(
                    await checkCommand.ExecuteScalarAsync());

                if (existingUser > 0)
                {
                    ModelState.AddModelError(
                        "Email",
                        "An account with this email already exists.");

                    return View(model);
                }
            }

            // Insert new student
            const string insertSql = """
            INSERT INTO dbo.Users
            (
                FullName,
                Email,
                Password,
                UserType,
                Role
            )
            VALUES
            (
                @FullName,
                @Email,
                @Password,
                'Student',
                'Student'
            );
            """;

            await using (var insertCommand =
                new SqlCommand(insertSql, connection))
            {
                insertCommand.CommandTimeout = 10;

                insertCommand.Parameters.Add(
                    new SqlParameter(
                        "@FullName",
                        System.Data.SqlDbType.NVarChar, 100)
                    {
                        Value = fullName
                    });

                insertCommand.Parameters.Add(
                    new SqlParameter(
                        "@Email",
                        System.Data.SqlDbType.NVarChar, 150)
                    {
                        Value = email
                    });

                insertCommand.Parameters.Add(
                    new SqlParameter(
                        "@Password",
                        System.Data.SqlDbType.NVarChar, 500)
                    {
                        Value = model.Password
                    });

                await insertCommand.ExecuteNonQueryAsync();
            }

            TempData["Success"] =
                "Account created successfully! Please sign in.";

            // IMPORTANT:
            // Do not automatically login here.
            // Go to Login after registration.
            return RedirectToAction(nameof(Login));
        }
        catch (SqlException ex)
        {
            ModelState.AddModelError(
                "",
                "Database error. Please check your database connection.");

            Console.WriteLine(
                "REGISTER SQL ERROR: " + ex.Message);

            return View(model);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(
                "",
                "Database connection is not configured correctly.");

            Console.WriteLine(
                "REGISTER CONFIG ERROR: " + ex.Message);

            return View(model);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(
                "",
                "Unable to create the account. Please try again.");

            Console.WriteLine(
                "REGISTER ERROR: " + ex.Message);

            return View(model);
        }
    }
    // ================= ADMIN LOGIN =================

    [HttpGet]
    public IActionResult AdminLogin()
    {
        if (HttpContext.Session.GetInt32("UserId") != null)
        {
            string? role = HttpContext.Session.GetString("Role");

            if (role != null &&
                role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "AdminDashboard");
            }

            return RedirectToAction("Index", "Dashboard");
        }

        return View(new LoginViewModel());
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdminLogin(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        string email = model.Email.Trim().ToLowerInvariant();

        // Fixed Admin Credentials
        const string adminEmail = "admin@smartstudy.com";
        const string adminPassword = "Admin@123";

        if (!email.Equals(adminEmail, StringComparison.OrdinalIgnoreCase) ||
            model.Password != adminPassword)
        {
            ModelState.AddModelError(
                "",
                "Invalid admin email or password.");

            return View(model);
        }

        try
        {
            await using var connection =
                new SqlConnection(ConnectionString);

            await connection.OpenAsync();

            const string sql = """
            SELECT UserId, FullName, UserType, Role
            FROM dbo.Users
            WHERE LOWER(Email) = @Email
              AND UserType = 'Admin'
              AND Role = 'Admin';
            """;

            await using var command =
                new SqlCommand(sql, connection);

            command.Parameters.Add(
                new SqlParameter(
                    "@Email",
                    System.Data.SqlDbType.NVarChar, 150)
                {
                    Value = adminEmail
                });

            await using var reader =
                await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                int userId = reader.GetInt32(0);
                string fullName = reader.GetString(1);
                string userType = reader.GetString(2);
                string role = reader.GetString(3);

                HttpContext.Session.SetInt32(
                    "UserId",
                    userId);

                HttpContext.Session.SetString(
                    "UserName",
                    fullName);

                HttpContext.Session.SetString(
                    "UserType",
                    userType);

                HttpContext.Session.SetString(
                    "Role",
                    role);

                return RedirectToAction(
                    "Index",
                    "AdminDashboard");
            }

            ModelState.AddModelError(
                "",
                "Admin account was not found.");

            return View(model);
        }
        catch (SqlException)
        {
            ModelState.AddModelError(
                "",
                "Unable to connect to the database.");

            return View(model);
        }
        catch
        {
            ModelState.AddModelError(
                "",
                "Something went wrong. Please try again.");

            return View(model);
        }
    }
    // =========================
    // LOGOUT
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();

        return RedirectToAction("Index", "Home");
    }
}