using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OscarsBallot.Data;
using OscarsBallot.Infrastructure;
using OscarsBallot.Models;

namespace OscarsBallot.Controllers;

public class LeaderboardController(AppDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = HttpContext.Session.GetInt32(SessionKeys.UserId);
        if (userId is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var leaderboard = await db.Users
            .AsNoTracking()
            .OrderByDescending(u => u.Score)
            .ThenBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync();

        return View(leaderboard);
    }
}
