using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using OscarsBallot.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var configuredConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
string? sqliteDbPath = null;

if (builder.Environment.IsDevelopment())
{
    string sqliteConnectionString;
    if (string.IsNullOrWhiteSpace(configuredConnectionString))
    {
        string home = Environment.GetEnvironmentVariable("HOME") ?? AppContext.BaseDirectory;
        string dataFolder = Path.Combine(home, "site", "wwwroot", "App_Data");
        Directory.CreateDirectory(dataFolder);

        string dbFileName = "OscarsBallot.db";
        sqliteDbPath = Path.Combine(dataFolder, dbFileName);
        sqliteConnectionString = $"Data Source={sqliteDbPath}";
    }
    else
    {
        sqliteConnectionString = configuredConnectionString;
    }

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(sqliteConnectionString));
}
else
{
    if (string.IsNullOrWhiteSpace(configuredConnectionString))
    {
        throw new InvalidOperationException(
            "Production database connection string is missing. Set ConnectionStrings__DefaultConnection.");
    }

    builder.Services.AddDbContext<AppDbContext, PostgresAppDbContext>(options =>
        options.UseNpgsql(configuredConnectionString));
}

builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromHours(8);
});

var app = builder.Build();

// ---------- Apply migrations (and create DB) before seeding ----------
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // Apply any pending migrations (creates DB file if missing)
        await db.Database.MigrateAsync();
        if (builder.Environment.IsDevelopment())
        {
            logger.LogInformation("Applied migrations and ensured SQLite DB exists at: {DbPath}", sqliteDbPath);
        }
        else
        {
            logger.LogInformation("Applied migrations for PostgreSQL.");
        }
    }
    catch (Exception ex)
    {
        var loggerEx = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        loggerEx.LogError(ex, "Error applying migrations on startup.");
        throw; // rethrow so deployment fails clearly; remove if you prefer swallowing
    }
}

// Seed data (your existing seeder)
await DataSeeder.SeedAsync(app.Services);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();