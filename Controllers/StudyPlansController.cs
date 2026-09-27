using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SmartStudyPlannerSystem.Models;

namespace SmartStudyPlannerSystem.Controllers;

public class StudyPlansController : Controller
{
    private readonly IConfiguration _configuration;

    public StudyPlansController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private string ConnectionString =>
        _configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "DefaultConnection is not configured.");

    private int? CurrentUserId =>
        HttpContext.Session.GetInt32("UserId");

    // =========================
    // READ - Study Plans List
    // =========================
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (CurrentUserId == null)
            return RedirectToAction("Login", "Account");

        var studyPlans = new List<StudyPlan>();

        try
        {
            await using var connection = new SqlConnection(ConnectionString);
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
                WHERE UserId = @UserId
                ORDER BY StudyDate ASC, StartTime ASC;
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
                studyPlans.Add(new StudyPlan
                {
                    StudyPlanId = reader.GetInt32(0),
                    UserId = reader.GetInt32(1),
                    Title = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    StudyDate = reader.GetDateTime(3),
                    Description = reader.IsDBNull(4)
                        ? ""
                        : reader.GetString(4),
                    Subject = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    Topic = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    StartTime = reader.IsDBNull(7)
                        ? null
                        : reader.GetTimeSpan(7),
                    EndTime = reader.IsDBNull(8)
                        ? null
                        : reader.GetTimeSpan(8),
                    Notes = reader.IsDBNull(9) ? "" : reader.GetString(9),
                    CreatedAt = reader.GetDateTime(10)
                });
            }

            return View(studyPlans);
        }
        catch (SqlException)
        {
            TempData["Error"] =
                "Unable to load study plans. Please try again.";

            return View(studyPlans);
        }
    }

    // =========================
    // CREATE - GET
    // =========================
    [HttpGet]
    public IActionResult Create()
    {
        if (CurrentUserId == null)
            return RedirectToAction("Login", "Account");

        var model = new StudyPlan
        {
            StudyDate = DateTime.Today
        };

        return View(model);
    }

    // =========================
    // CREATE - POST
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StudyPlan model)
    {
        if (CurrentUserId == null)
            return RedirectToAction("Login", "Account");

        ValidateTime(model);

        if (!ModelState.IsValid)
            return View(model);

        model.Title = model.Title.Trim();
        model.Subject = model.Subject?.Trim() ?? "";
        model.Topic = model.Topic?.Trim() ?? "";
        model.Description = model.Description?.Trim() ?? "";
        model.Notes = model.Notes?.Trim() ?? "";

        try
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            const string sql = """
                INSERT INTO dbo.StudyPlans
                (
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
                )
                VALUES
                (
                    @UserId,
                    @Title,
                    @StudyDate,
                    @Description,
                    @Subject,
                    @Topic,
                    @StartTime,
                    @EndTime,
                    @Notes,
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
                new SqlParameter("@Title", System.Data.SqlDbType.NVarChar, 200)
                {
                    Value = model.Title
                });

            command.Parameters.Add(
                new SqlParameter("@StudyDate", System.Data.SqlDbType.DateTime2)
                {
                    Value = model.StudyDate
                });

            command.Parameters.Add(
                new SqlParameter("@Description", System.Data.SqlDbType.NVarChar, 2000)
                {
                    Value = string.IsNullOrWhiteSpace(model.Description)
                        ? DBNull.Value
                        : model.Description
                });

            command.Parameters.Add(
                new SqlParameter("@Subject", System.Data.SqlDbType.NVarChar, 200)
                {
                    Value = string.IsNullOrWhiteSpace(model.Subject)
                        ? DBNull.Value
                        : model.Subject
                });

            command.Parameters.Add(
                new SqlParameter("@Topic", System.Data.SqlDbType.NVarChar, 300)
                {
                    Value = string.IsNullOrWhiteSpace(model.Topic)
                        ? DBNull.Value
                        : model.Topic
                });

            command.Parameters.Add(
                new SqlParameter("@StartTime", System.Data.SqlDbType.Time)
                {
                    Value = model.StartTime.HasValue
                        ? model.StartTime.Value
                        : DBNull.Value
                });

            command.Parameters.Add(
                new SqlParameter("@EndTime", System.Data.SqlDbType.Time)
                {
                    Value = model.EndTime.HasValue
                        ? model.EndTime.Value
                        : DBNull.Value
                });

            command.Parameters.Add(
                new SqlParameter("@Notes", System.Data.SqlDbType.NVarChar, 3000)
                {
                    Value = string.IsNullOrWhiteSpace(model.Notes)
                        ? DBNull.Value
                        : model.Notes
                });

            await command.ExecuteNonQueryAsync();

            TempData["Success"] = "Study plan created successfully.";

            return RedirectToAction(nameof(Index));
        }
        catch (SqlException)
        {
            ModelState.AddModelError(
                "",
                "Unable to save the study plan. Please try again.");

            return View(model);
        }
    }

    // =========================
    // EDIT - GET
    // =========================
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
                WHERE StudyPlanId = @StudyPlanId
                  AND UserId = @UserId;
                """;

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.Add(
                new SqlParameter("@StudyPlanId", System.Data.SqlDbType.Int)
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

            var model = new StudyPlan
            {
                StudyPlanId = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                Title = reader.IsDBNull(2) ? "" : reader.GetString(2),
                StudyDate = reader.GetDateTime(3),
                Description = reader.IsDBNull(4) ? "" : reader.GetString(4),
                Subject = reader.IsDBNull(5) ? "" : reader.GetString(5),
                Topic = reader.IsDBNull(6) ? "" : reader.GetString(6),
                StartTime = reader.IsDBNull(7)
                    ? null
                    : reader.GetTimeSpan(7),
                EndTime = reader.IsDBNull(8)
                    ? null
                    : reader.GetTimeSpan(8),
                Notes = reader.IsDBNull(9) ? "" : reader.GetString(9),
                CreatedAt = reader.GetDateTime(10)
            };

            return View(model);
        }
        catch (SqlException)
        {
            TempData["Error"] =
                "Unable to load the study plan.";

            return RedirectToAction(nameof(Index));
        }
    }

    // =========================
    // EDIT - POST
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, StudyPlan model)
    {
        if (CurrentUserId == null)
            return RedirectToAction("Login", "Account");

        model.StudyPlanId = id;

        ValidateTime(model);

        if (!ModelState.IsValid)
            return View(model);

        model.Title = model.Title.Trim();
        model.Subject = model.Subject?.Trim() ?? "";
        model.Topic = model.Topic?.Trim() ?? "";
        model.Description = model.Description?.Trim() ?? "";
        model.Notes = model.Notes?.Trim() ?? "";

        try
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            const string sql = """
                UPDATE dbo.StudyPlans
                SET
                    Title = @Title,
                    StudyDate = @StudyDate,
                    Description = @Description,
                    Subject = @Subject,
                    Topic = @Topic,
                    StartTime = @StartTime,
                    EndTime = @EndTime,
                    Notes = @Notes
                WHERE StudyPlanId = @StudyPlanId
                  AND UserId = @UserId;
                """;

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.Add(
                new SqlParameter("@StudyPlanId", System.Data.SqlDbType.Int)
                {
                    Value = id
                });

            command.Parameters.Add(
                new SqlParameter("@UserId", System.Data.SqlDbType.Int)
                {
                    Value = CurrentUserId.Value
                });

            command.Parameters.Add(
                new SqlParameter("@Title", System.Data.SqlDbType.NVarChar, 200)
                {
                    Value = model.Title
                });

            command.Parameters.Add(
                new SqlParameter("@StudyDate", System.Data.SqlDbType.DateTime2)
                {
                    Value = model.StudyDate
                });

            command.Parameters.Add(
                new SqlParameter("@Description", System.Data.SqlDbType.NVarChar, 2000)
                {
                    Value = string.IsNullOrWhiteSpace(model.Description)
                        ? DBNull.Value
                        : model.Description
                });

            command.Parameters.Add(
                new SqlParameter("@Subject", System.Data.SqlDbType.NVarChar, 200)
                {
                    Value = string.IsNullOrWhiteSpace(model.Subject)
                        ? DBNull.Value
                        : model.Subject
                });

            command.Parameters.Add(
                new SqlParameter("@Topic", System.Data.SqlDbType.NVarChar, 300)
                {
                    Value = string.IsNullOrWhiteSpace(model.Topic)
                        ? DBNull.Value
                        : model.Topic
                });

            command.Parameters.Add(
                new SqlParameter("@StartTime", System.Data.SqlDbType.Time)
                {
                    Value = model.StartTime.HasValue
                        ? model.StartTime.Value
                        : DBNull.Value
                });

            command.Parameters.Add(
                new SqlParameter("@EndTime", System.Data.SqlDbType.Time)
                {
                    Value = model.EndTime.HasValue
                        ? model.EndTime.Value
                        : DBNull.Value
                });

            command.Parameters.Add(
                new SqlParameter("@Notes", System.Data.SqlDbType.NVarChar, 3000)
                {
                    Value = string.IsNullOrWhiteSpace(model.Notes)
                        ? DBNull.Value
                        : model.Notes
                });

            int rows = await command.ExecuteNonQueryAsync();

            if (rows == 0)
                return NotFound();

            TempData["Success"] = "Study plan updated successfully.";

            return RedirectToAction(nameof(Index));
        }
        catch (SqlException)
        {
            ModelState.AddModelError(
                "",
                "Unable to update the study plan. Please try again.");

            return View(model);
        }
    }

    // =========================
    // DELETE
    // =========================
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
                DELETE FROM dbo.StudyPlans
                WHERE StudyPlanId = @StudyPlanId
                  AND UserId = @UserId;
                """;

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.Add(
                new SqlParameter("@StudyPlanId", System.Data.SqlDbType.Int)
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
                TempData["Success"] = "Study plan deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
        catch (SqlException)
        {
            TempData["Error"] =
                "Unable to delete the study plan. Please try again.";

            return RedirectToAction(nameof(Index));
        }
    }

    // =========================
    // TIME VALIDATION
    // =========================
    private void ValidateTime(StudyPlan model)
    {
        if (model.StartTime.HasValue &&
            model.EndTime.HasValue &&
            model.StartTime.Value >= model.EndTime.Value)
        {
            ModelState.AddModelError(
                "EndTime",
                "End time must be later than start time.");
        }
    }
}