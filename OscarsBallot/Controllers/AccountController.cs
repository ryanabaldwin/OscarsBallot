using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OscarsBallot.Data;
using OscarsBallot.Infrastructure;
using OscarsBallot.Models;
using OscarsBallot.ViewModels.Account;

namespace OscarsBallot.Controllers;

public class AccountController(AppDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = HttpContext.Session.GetInt32(SessionKeys.UserId);
        if (userId is null)
        {
            return RedirectToAction(nameof(Login));
        }

        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId.Value);
        if (user is null)
        {
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }

        var selections = await db.Ballots
            .AsNoTracking()
            .Where(b => b.UserId == userId.Value)
            .Join(db.Categories,
                b => b.CategoryId,
                c => c.CategoryId,
                (b, c) => new { b.CategoryId, c.CategoryName, c.Points, b.Rank, b.NomineeId })
            .Join(db.Nominees,
                x => x.NomineeId,
                n => n.NomineeId,
                (x, n) => new { x.CategoryId, x.CategoryName, x.Points, x.Rank, NomineeName = n.Name })
            .ToListAsync();

        var categories = selections
            .GroupBy(x => new { x.CategoryId, x.CategoryName, x.Points })
            .OrderBy(x => x.Key.CategoryId)
            .Select(group => new AccountBallotCategoryViewModel
            {
                CategoryName = group.Key.CategoryName,
                Points = group.Key.Points,
                FirstChoiceName = group.FirstOrDefault(x => x.Rank == 1)?.NomineeName ?? "-",
                SecondChoiceName = group.FirstOrDefault(x => x.Rank == 2)?.NomineeName ?? "-"
            })
            .ToList();

        var model = new AccountBallotViewModel
        {
            UserDisplayName = $"{user.FirstName} {user.LastName}",
            CurrentScore = user.Score,
            HasBallot = categories.Count > 0,
            Categories = categories
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var firstName = model.FirstName.Trim();
        var lastName = model.LastName.Trim();
        var firstLower = firstName.ToLower();
        var lastLower = lastName.ToLower();

        var exists = await db.Users
            .AnyAsync(u => u.FirstName.ToLower() == firstLower && u.LastName.ToLower() == lastLower);

        if (exists)
        {
            ModelState.AddModelError(string.Empty, "An account with this first and last name already exists.");
            return View(model);
        }

        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Admin = false
        };

        user.Pin = model.Pin;

        db.Users.Add(user);
        await db.SaveChangesAsync();

        SignInUser(user);
        return RedirectToAction("Index", "Ballot");
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var firstLower = model.FirstName.Trim().ToLower();
        var lastLower = model.LastName.Trim().ToLower();

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.FirstName.ToLower() == firstLower && u.LastName.ToLower() == lastLower);

        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid credentials.");
            return View(model);
        }

        if (user.Pin != model.Pin)
        {
            ModelState.AddModelError(string.Empty, "Invalid credentials.");
            return View(model);
        }

        SignInUser(user);
        return RedirectToAction("Index", "Ballot");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }

    private void SignInUser(User user)
    {
        HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserId);
        HttpContext.Session.SetString(SessionKeys.UserDisplayName, $"{user.FirstName} {user.LastName}");
        HttpContext.Session.SetInt32(SessionKeys.UserIsAdmin, user.Admin ? 1 : 0);
    }
}
