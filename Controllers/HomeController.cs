using Microsoft.AspNetCore.Mvc;
using SmartStudyPlannerSystem.Models;

namespace SmartStudyPlannerSystem.Controllers;

public class HomeController : Controller
{
    // HOME
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    // FEATURES
    [HttpGet]
    public IActionResult Features()
    {
        return View();
    }

    // ABOUT US
    [HttpGet]
    public IActionResult AboutUs()
    {
        return View();
    }

    // PRIVACY
    [HttpGet]
    public IActionResult Privacy()
    {
        return View();
    }

    // ERROR
    [ResponseCache(
        Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = HttpContext.TraceIdentifier
        });
    }
}