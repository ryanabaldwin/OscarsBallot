using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OscarsBallot.Data;
using OscarsBallot.Infrastructure;
using OscarsBallot.Models;
using OscarsBallot.ViewModels.Admin;

namespace OscarsBallot.Controllers;

public class AdminController(AppDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var adminUser = await GetCurrentAdminUserAsync();
        if (adminUser is null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!adminUser.Admin)
        {
            return Forbid();
        }

        var model = await BuildWinnersModelAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(AdminWinnersViewModel model)
    {
        var adminUser = await GetCurrentAdminUserAsync();
        if (adminUser is null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!adminUser.Admin)
        {
            return Forbid();
        }

        var categories = await db.Categories
            .AsNoTracking()
            .Include(c => c.Nominees)
            .OrderBy(c => c.CategoryId)
            .ToListAsync();

        var categoryLookup = categories.ToDictionary(
            c => c.CategoryId,
            c => c.Nominees.Select(n => n.NomineeId).ToHashSet());

        var submitted = model.Categories;

        foreach (var category in submitted)
        {
            if (!category.SelectedWinnerNomineeId.HasValue)
            {
                continue;
            }

            if (!categoryLookup.TryGetValue(category.CategoryId, out var nomineesForCategory) ||
                !nomineesForCategory.Contains(category.SelectedWinnerNomineeId!.Value))
            {
                ModelState.AddModelError(string.Empty, "Invalid winner selection submitted.");
                break;
            }
        }

        if (!ModelState.IsValid)
        {
            var refreshed = await BuildWinnersModelAsync(model);
            return View(refreshed);
        }

        var categoryIds = submitted.Select(c => c.CategoryId).Distinct().ToArray();
        var existingByCategory = await db.Winners
            .Where(w => categoryIds.Contains(w.CategoryId))
            .ToDictionaryAsync(w => w.CategoryId);

        var hasChanges = false;
        foreach (var category in submitted)
        {
            if (existingByCategory.TryGetValue(category.CategoryId, out var existingWinner))
            {
                if (!category.SelectedWinnerNomineeId.HasValue)
                {
                    db.Winners.Remove(existingWinner);
                    hasChanges = true;
                    continue;
                }

                var winnerNomineeId = category.SelectedWinnerNomineeId.Value;
                if (existingWinner.WinnerNomineeId != winnerNomineeId)
                {
                    existingWinner.WinnerNomineeId = winnerNomineeId;
                    hasChanges = true;
                }
                continue;
            }

            if (!category.SelectedWinnerNomineeId.HasValue)
            {
                continue;
            }

            db.Winners.Add(new Winner
            {
                CategoryId = category.CategoryId,
                WinnerNomineeId = category.SelectedWinnerNomineeId.Value
            });
            hasChanges = true;
        }

        if (!hasChanges)
        {
            TempData["InfoMessage"] = "No winner changes were submitted.";
            return RedirectToAction(nameof(Index));
        }

        await db.SaveChangesAsync();
        await RecalculateUserScoresAsync();
        TempData["SuccessMessage"] = "Winner selections updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetBallotEditingMode(string mode)
    {
        var adminUser = await GetCurrentAdminUserAsync();
        if (adminUser is null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!adminUser.Admin)
        {
            return Forbid();
        }

        var settings = await GetOrCreateAppSettingAsync();
        settings.BallotsLockedOverride = mode switch
        {
            "lock" => true,
            "unlock" => false,
            _ => null
        };

        await db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Ballot editing mode updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Categories()
    {
        var adminUser = await GetCurrentAdminUserAsync();
        if (adminUser is null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!adminUser.Admin)
        {
            return Forbid();
        }

        var model = await BuildCategoriesModelAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Categories(AdminCategoriesViewModel model)
    {
        var adminUser = await GetCurrentAdminUserAsync();
        if (adminUser is null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!adminUser.Admin)
        {
            return Forbid();
        }

        if (model.Categories.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "No categories were submitted.");
        }

        var duplicateNameExists = model.Categories
            .GroupBy(c => c.CategoryName.Trim(), StringComparer.OrdinalIgnoreCase)
            .Any(g => g.Count() > 1);
        if (duplicateNameExists)
        {
            ModelState.AddModelError(string.Empty, "Category names must be unique.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var categoryIds = model.Categories.Select(c => c.CategoryId).ToArray();
        var existingCategories = await db.Categories
            .Where(c => categoryIds.Contains(c.CategoryId))
            .ToDictionaryAsync(c => c.CategoryId);

        foreach (var item in model.Categories)
        {
            if (!existingCategories.TryGetValue(item.CategoryId, out var category))
            {
                ModelState.AddModelError(string.Empty, "Invalid category submission.");
                return View(model);
            }

            category.CategoryName = item.CategoryName.Trim();
            category.Points = item.Points;
        }

        await db.SaveChangesAsync();
        await RecalculateUserScoresAsync();

        TempData["SuccessMessage"] = "Categories updated successfully.";
        return RedirectToAction(nameof(Categories));
    }

    private async Task<User?> GetCurrentAdminUserAsync()
    {
        var userId = HttpContext.Session.GetInt32(SessionKeys.UserId);
        if (userId is null)
        {
            return null;
        }

        return await db.Users.FirstOrDefaultAsync(u => u.UserId == userId.Value);
    }

    private async Task<AdminWinnersViewModel> BuildWinnersModelAsync(AdminWinnersViewModel? posted = null)
    {
        var categories = await db.Categories
            .AsNoTracking()
            .Include(c => c.Nominees)
            .OrderBy(c => c.CategoryId)
            .ToListAsync();

        var existingWinners = await db.Winners
            .AsNoTracking()
            .ToDictionaryAsync(w => w.CategoryId, w => w.WinnerNomineeId);

        var postedByCategory = posted?.Categories.ToDictionary(c => c.CategoryId) ?? [];
        var settings = await db.AppSettings.AsNoTracking().FirstOrDefaultAsync(x => x.AppSettingId == 1);
        var isLocked = BallotEditingPolicy.IsBallotEditingLocked(settings?.BallotsLockedOverride, DateTime.UtcNow);

        return new AdminWinnersViewModel
        {
            IsBallotEditingLocked = isLocked,
            BallotsLockedOverride = settings?.BallotsLockedOverride,
            CeremonyStartMountain = BallotEditingPolicy.CeremonyStartMountain,
            Categories = categories.Select(category => new AdminWinnerCategoryViewModel
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                SelectedWinnerNomineeId = postedByCategory.GetValueOrDefault(category.CategoryId)?.SelectedWinnerNomineeId
                                         ?? existingWinners.GetValueOrDefault(category.CategoryId),
                Nominees = category.Nominees
                    .OrderBy(n => n.Name)
                    .Select(n => new AdminNomineeOptionViewModel
                    {
                        NomineeId = n.NomineeId,
                        Name = n.Name
                    })
                    .ToList()
            }).ToList()
        };
    }

    private async Task<AppSetting> GetOrCreateAppSettingAsync()
    {
        var settings = await db.AppSettings.FirstOrDefaultAsync(x => x.AppSettingId == 1);
        if (settings is not null)
        {
            return settings;
        }

        settings = new AppSetting
        {
            AppSettingId = 1,
            BallotsLockedOverride = null
        };

        db.AppSettings.Add(settings);
        await db.SaveChangesAsync();
        return settings;
    }

    private async Task<AdminCategoriesViewModel> BuildCategoriesModelAsync()
    {
        var categories = await db.Categories
            .AsNoTracking()
            .OrderBy(c => c.CategoryId)
            .Select(c => new AdminCategoryEditItemViewModel
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName,
                Points = c.Points
            })
            .ToListAsync();

        return new AdminCategoriesViewModel
        {
            Categories = categories
        };
    }

    private async Task RecalculateUserScoresAsync()
    {
        var scoreByUser = await (
            from b in db.Ballots
            join w in db.Winners
                on new { b.CategoryId, b.NomineeId } equals new { w.CategoryId, NomineeId = w.WinnerNomineeId }
            join c in db.Categories
                on b.CategoryId equals c.CategoryId
            group new { b, c } by b.UserId
            into g
            select new
            {
                UserId = g.Key,
                Score = g.Sum(x => x.b.Rank == 1 ? x.c.Points : x.c.Points / 2m)
            })
            .ToDictionaryAsync(x => x.UserId, x => x.Score);

        var users = await db.Users.ToListAsync();
        foreach (var user in users)
        {
            user.Score = scoreByUser.GetValueOrDefault(user.UserId, 0m);
        }

        await db.SaveChangesAsync();
    }
}
