using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OscarsBallot.Data;
using OscarsBallot.Infrastructure;
using OscarsBallot.Models;
using OscarsBallot.ViewModels.Ballot;

namespace OscarsBallot.Controllers;

public class BallotController(AppDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = HttpContext.Session.GetInt32(SessionKeys.UserId);
        if (userId is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var model = await BuildSubmissionModelAsync(userId.Value);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(BallotSubmissionViewModel model)
    {
        var userId = HttpContext.Session.GetInt32(SessionKeys.UserId);
        if (userId is null)
        {
            return RedirectToAction("Login", "Account");
        }

        model = await BuildSubmissionModelAsync(userId.Value, model);
        ValidateSubmission(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existing = await db.Ballots
            .Where(x => x.UserId == userId.Value)
            .ToListAsync();

        db.Ballots.RemoveRange(existing);

        var ballots = model.Categories.SelectMany(category => new[]
        {
            new Ballot
            {
                UserId = userId.Value,
                CategoryId = category.CategoryId,
                Rank = 1,
                NomineeId = category.FirstChoiceNomineeId!.Value
            },
            new Ballot
            {
                UserId = userId.Value,
                CategoryId = category.CategoryId,
                Rank = 2,
                NomineeId = category.SecondChoiceNomineeId!.Value
            }
        });

        db.Ballots.AddRange(ballots);
        await db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Your ballot has been saved.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<BallotSubmissionViewModel> BuildSubmissionModelAsync(
        int userId,
        BallotSubmissionViewModel? postedModel = null)
    {
        var categories = await db.Categories
            .Include(x => x.Nominees)
            .OrderBy(x => x.CategoryName)
            .ToListAsync();

        var existingSelections = await db.Ballots
            .Where(x => x.UserId == userId)
            .ToListAsync();

        var postedByCategory = postedModel?.Categories.ToDictionary(x => x.CategoryId) ?? [];

        return new BallotSubmissionViewModel
        {
            Categories = categories.Select(category =>
            {
                var posted = postedByCategory.GetValueOrDefault(category.CategoryId);
                var firstExisting = existingSelections
                    .FirstOrDefault(x => x.CategoryId == category.CategoryId && x.Rank == 1)?.NomineeId;
                var secondExisting = existingSelections
                    .FirstOrDefault(x => x.CategoryId == category.CategoryId && x.Rank == 2)?.NomineeId;

                return new BallotCategorySelectionViewModel
                {
                    CategoryId = category.CategoryId,
                    CategoryName = category.CategoryName,
                    FirstChoiceNomineeId = posted?.FirstChoiceNomineeId ?? firstExisting,
                    SecondChoiceNomineeId = posted?.SecondChoiceNomineeId ?? secondExisting,
                    Nominees = category.Nominees
                        .OrderBy(x => x.Name)
                        .Select(x => new BallotNomineeOptionViewModel
                        {
                            NomineeId = x.NomineeId,
                            Name = x.Name
                        })
                        .ToList()
                };
            }).ToList()
        };
    }

    private void ValidateSubmission(BallotSubmissionViewModel model)
    {
        for (var i = 0; i < model.Categories.Count; i++)
        {
            var category = model.Categories[i];
            var nomineeIds = category.Nominees.Select(x => x.NomineeId).ToHashSet();

            if (!category.FirstChoiceNomineeId.HasValue)
            {
                ModelState.AddModelError($"Categories[{i}].FirstChoiceNomineeId", "First choice is required.");
            }

            if (!category.SecondChoiceNomineeId.HasValue)
            {
                ModelState.AddModelError($"Categories[{i}].SecondChoiceNomineeId", "Second choice is required.");
            }

            if (category.FirstChoiceNomineeId.HasValue && !nomineeIds.Contains(category.FirstChoiceNomineeId.Value))
            {
                ModelState.AddModelError($"Categories[{i}].FirstChoiceNomineeId", "Invalid nominee selection.");
            }

            if (category.SecondChoiceNomineeId.HasValue && !nomineeIds.Contains(category.SecondChoiceNomineeId.Value))
            {
                ModelState.AddModelError($"Categories[{i}].SecondChoiceNomineeId", "Invalid nominee selection.");
            }

            if (category.FirstChoiceNomineeId.HasValue
                && category.SecondChoiceNomineeId.HasValue
                && category.FirstChoiceNomineeId.Value == category.SecondChoiceNomineeId.Value)
            {
                ModelState.AddModelError($"Categories[{i}].SecondChoiceNomineeId",
                    "Second choice must be different from first choice.");
            }
        }
    }
}
