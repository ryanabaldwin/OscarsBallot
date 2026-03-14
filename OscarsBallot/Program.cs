// Program.cs
using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OscarsBallot.Data; // <-- adjust if your namespace differs

var builder = WebApplication.CreateBuilder(args);

// Basic services
builder.Services.AddControllersWithViews();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromHours(8);
});

// Read configured connection (may be null/empty)
var configuredConn = builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty;

// Helper to detect Postgres connection strings (Npgsql format or postgres URI)
static bool LooksLikePostgres(string s)
{
    if (string.IsNullOrWhiteSpace(s)) return false;
    return s.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)
        || s.IndexOf("Host=", StringComparison.OrdinalIgnoreCase) >= 0
        || s.IndexOf("Username=", StringComparison.OrdinalIgnoreCase) >= 0
        || s.IndexOf("User Id=", StringComparison.OrdinalIgnoreCase) >= 0;
}

// Configure DbContext: Postgres when appropriate, otherwise SQLite fallback
string? sqliteDbPath = null;
if (LooksLikePostgres(configuredConn))
{
    // Use Postgres (Npgsql). Expect Npgsql-style connection string (Host=..., Username=..., Password=...)
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(configuredConn));
}
else
{
    // Use SQLite. Create an App_Data folder under the app content root so the DB is persisted in /home/site/wwwroot/App_Data on Azure
    var home = Environment.GetEnvironmentVariable("HOME") ?? AppContext.BaseDirectory;
    var dataFolder = Path.Combine(home, "site", "wwwroot", "App_Data");
    Directory.CreateDirectory(dataFolder);

    const string dbFileName = "OscarsBallot.db";
    sqliteDbPath = Path.Combine(dataFolder, dbFileName);
    var sqliteConn = $"Data Source={sqliteDbPath}";

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(sqliteConn));
}

// Build the app
var app = builder.Build();

// Apply migrations at startup (best-effort). Note: for production it's safer to run migrations from CI.
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Best-effort migration: try to migrate but don't crash the whole app if it fails.
        // This prevents the container from aborting on transient DB problems. CI-based migrations are recommended.
        logger.LogInformation("Attempting to apply migrations...");
        await db.Database.MigrateAsync();
        if (LooksLikePostgres(configuredConn))
        {
            logger.LogInformation("Applied migrations for Postgres.");
        }
        else
        {
            logger.LogInformation("Applied migrations and ensured SQLite DB exists at: {DbPath}", sqliteDbPath);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error applying migrations at startup. Application will continue to run, but database schema may be out of date.");
        // DO NOT rethrow here — we want the app to come up so you can inspect logs & fix connection strings.
    }
}

// Run your seed (if you have one) — keep its own try/catch if desired
try
{
    await DataSeeder.SeedAsync(app.Services);
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Seeder failed (non-fatal).");
}

// Configure middlewares & routes
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // if your app uses static files
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
