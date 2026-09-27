using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SmartStudyPlannerSystem.Models;

namespace SmartStudyPlannerSystem.Controllers;

public class TasksController : Controller
{
    private readonly IConfiguration _configuration;

    public TasksController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private string ConnectionString =>
        _configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "DefaultConnection is not configured.");

    private int? CurrentUserId =>
        HttpContext.Session.GetInt32("UserId");


    // =========================================================
    // READ - All Tasks
    // =========================================================
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (CurrentUserId == null)
            return RedirectToAction("Login", "Account");

        var tasks = new List<SmartStudyPlannerSystem.Models.Task>();

        try
        {
            await using var connection = new SqlConnection(ConnectionString);
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
                WHERE UserId = @UserId
                ORDER BY DueDate ASC;
                """;

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.Add(
                new SqlParameter("@UserId", System.Data.SqlDbType.Int)
                {
                    Value = CurrentUserId.Value
                });

            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                tasks.Add(new SmartStudyPlannerSystem.Models.Task
                {
                    TaskId = reader.GetInt32(0),
                    UserId = reader.GetInt32(1),
                    TaskTitle = reader.IsDBNull(2)
                        ? ""
                        : reader.GetString(2),
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
                "Unable to load tasks. Please try again.";

            return View(tasks);
        }
    }


    // =========================================================
    // CREATE - GET
    // =========================================================
    [HttpGet]
    public IActionResult Create()
    {
        if (CurrentUserId == null)
            return RedirectToAction("Login", "Account");

        var model = new SmartStudyPlannerSystem.Models.Task
        {
            DueDate = DateTime.Today,
            Status = "Pending",
            Priority = "Medium"
        };

        return View(model);
    }


    // =========================================================
    // CREATE - POST
    // =========================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        SmartStudyPlannerSystem.Models.Task model)
    {
        if (CurrentUserId == null)
            return RedirectToAction("Login", "Account");

        if (!ModelState.IsValid)
            return View(model);

        model.TaskTitle = model.TaskTitle.Trim();
        model.Subject = model.Subject.Trim();
        model.Status = model.Status.Trim();
        model.Priority = model.Priority.Trim();

        try
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            const string sql = """
                INSERT INTO dbo.StudyTasks
                (
                    UserId,
                    TaskTitle,
                    Subject,
                    DueDate,
                    Status,
                    Priority,
                    CreatedAt
                )
                VALUES
                (
                    @UserId,
                    @TaskTitle,
                    @Subject,
                    @DueDate,
                    @Status,
                    @Priority,
                    SYSUTCDATETIME()
                );
                """;

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.Add(
                new SqlParameter("@UserId", System.Data.SqlDbType.Int)
                {
                    Value = CurrentUserId.Value
                });

            command.Parameters.Add(
                new SqlParameter("@TaskTitle", System.Data.SqlDbType.NVarChar, 200)
                {
                    Value = model.TaskTitle
                });

            command.Parameters.Add(
                new SqlParameter("@Subject", System.Data.SqlDbType.NVarChar, 200)
                {
                    Value = model.Subject
                });

            command.Parameters.Add(
                new SqlParameter("@DueDate", System.Data.SqlDbType.DateTime2)
                {
                    Value = model.DueDate
                });

            command.Parameters.Add(
                new SqlParameter("@Status", System.Data.SqlDbType.NVarChar, 50)
                {
                    Value = model.Status
                });

            command.Parameters.Add(
                new SqlParameter("@Priority", System.Data.SqlDbType.NVarChar, 50)
                {
                    Value = model.Priority
                });

            await command.ExecuteNonQueryAsync();

            TempData["Success"] =
                "Task created successfully.";

            return RedirectToAction(nameof(Index));
        }
        catch (SqlException)
        {
            ModelState.AddModelError(
                "",
                "Unable to save the task. Please try again.");

            return View(model);
        }
    }


    // =========================================================
    // EDIT - GET
    // =========================================================
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (CurrentUserId == null)
            return RedirectToAction("Login", "Account");

        try
        {
            await using var connection = new SqlConnection(ConnectionString);
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
                WHERE TaskId = @TaskId
                  AND UserId = @UserId;
                """;

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.Add(
                new SqlParameter("@TaskId", System.Data.SqlDbType.Int)
                {
                    Value = id
                });

            command.Parameters.Add(
                new SqlParameter("@UserId", System.Data.SqlDbType.Int)
                {
                    Value = CurrentUserId.Value
                });

            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return NotFound();

            var model =
                new SmartStudyPlannerSystem.Models.Task
                {
                    TaskId = reader.GetInt32(0),
                    UserId = reader.GetInt32(1),
                    TaskTitle = reader.IsDBNull(2)
                        ? ""
                        : reader.GetString(2),
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
                };

            return View(model);
        }
        catch (SqlException)
        {
            TempData["Error"] =
                "Unable to load the task.";

            return RedirectToAction(nameof(Index));
        }
    }


    // =========================================================
    // EDIT - POST
    // =========================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        SmartStudyPlannerSystem.Models.Task model)
    {
        if (CurrentUserId == null)
            return RedirectToAction("Login", "Account");

        model.TaskId = id;

        if (!ModelState.IsValid)
            return View(model);

        model.TaskTitle = model.TaskTitle.Trim();
        model.Subject = model.Subject.Trim();
        model.Status = model.Status.Trim();
        model.Priority = model.Priority.Trim();

        try
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            const string sql = """
                UPDATE dbo.StudyTasks
                SET
                    TaskTitle = @TaskTitle,
                    Subject = @Subject,
                    DueDate = @DueDate,
                    Status = @Status,
                    Priority = @Priority
                WHERE TaskId = @TaskId
                  AND UserId = @UserId;
                """;

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.Add(
                new SqlParameter("@TaskId", System.Data.SqlDbType.Int)
                {
                    Value = id
                });

            command.Parameters.Add(
                new SqlParameter("@UserId", System.Data.SqlDbType.Int)
                {
                    Value = CurrentUserId.Value
                });

            command.Parameters.Add(
                new SqlParameter("@TaskTitle", System.Data.SqlDbType.NVarChar, 200)
                {
                    Value = model.TaskTitle
                });

            command.Parameters.Add(
                new SqlParameter("@Subject", System.Data.SqlDbType.NVarChar, 200)
                {
                    Value = model.Subject
                });

            command.Parameters.Add(
                new SqlParameter("@DueDate", System.Data.SqlDbType.DateTime2)
                {
                    Value = model.DueDate
                });

            command.Parameters.Add(
                new SqlParameter("@Status", System.Data.SqlDbType.NVarChar, 50)
                {
                    Value = model.Status
                });

            command.Parameters.Add(
                new SqlParameter("@Priority", System.Data.SqlDbType.NVarChar, 50)
                {
                    Value = model.Priority
                });

            int rows = await command.ExecuteNonQueryAsync();

            if (rows == 0)
                return NotFound();

            TempData["Success"] =
                "Task updated successfully.";

            return RedirectToAction(nameof(Index));
        }
        catch (SqlException)
        {
            ModelState.AddModelError(
                "",
                "Unable to update the task. Please try again.");

            return View(model);
        }
    }


    // =========================================================
    // DELETE
    // =========================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (CurrentUserId == null)
            return RedirectToAction("Login", "Account");

        try
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            const string sql = """
                DELETE FROM dbo.StudyTasks
                WHERE TaskId = @TaskId
                  AND UserId = @UserId;
                """;

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.Add(
                new SqlParameter("@TaskId", System.Data.SqlDbType.Int)
                {
                    Value = id
                });

            command.Parameters.Add(
                new SqlParameter("@UserId", System.Data.SqlDbType.Int)
                {
                    Value = CurrentUserId.Value
                });

            int rows = await command.ExecuteNonQueryAsync();

            if (rows > 0)
            {
                TempData["Success"] =
                    "Task deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }
        catch (SqlException)
        {
            TempData["Error"] =
                "Unable to delete the task. Please try again.";

            return RedirectToAction(nameof(Index));
        }
    }
}