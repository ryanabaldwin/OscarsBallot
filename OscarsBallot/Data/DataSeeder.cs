using Microsoft.EntityFrameworkCore;
using OscarsBallot.Models;

namespace OscarsBallot.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();

        var categoriesToSeed = new[]
        {
            "Best Picture",
            "Best Director",
            "Best Actor",
            "Best Actress",
            "Best Supporting Actor",
            "Best Supporting Actress",
            "Best Original Screenplay",
            "Best Adapted Screenplay",
            "Best International Feature Film",
            "Best Animated Feature Film",
            "Best Documentary Feature Film",
            "Best Documentary Short Film",
            "Best Live Action Short Film",
            "Best Animated Short Film",
            "Best Original Score",
            "Best Original Song",
            "Best Sound",
            "Best Production Design",
            "Best Cinematography",
            "Best Makeup and Hairstyling",
            "Best Costume Design",
            "Best Film Editing",
            "Best Visual Effects"
        };

        var existingCategoryNames = await db.Categories
            .Select(c => c.CategoryName)
            .ToListAsync();
        var existingLookup = existingCategoryNames.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var newCategories = categoriesToSeed
            .Where(categoryName => !existingLookup.Contains(categoryName))
            .Select(categoryName => new Category
            {
                CategoryName = categoryName,
                Points = 10m
            })
            .ToList();

        if (newCategories.Count > 0)
        {
            db.Categories.AddRange(newCategories);
            await db.SaveChangesAsync();
        }

        if (!await db.AppSettings.AnyAsync())
        {
            db.AppSettings.Add(new AppSetting
            {
                AppSettingId = 1,
                BallotsLockedOverride = null
            });
            await db.SaveChangesAsync();
        }
    }
}
