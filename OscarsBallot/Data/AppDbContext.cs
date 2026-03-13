using Microsoft.EntityFrameworkCore;
using OscarsBallot.Models;

namespace OscarsBallot.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Nominee> Nominees => Set<Nominee>();
    public DbSet<Ballot> Ballots => Set<Ballot>();
    public DbSet<Winner> Winners => Set<Winner>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(x => new { x.FirstName, x.LastName }).IsUnique();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(x => x.CategoryName).IsUnique();
        });

        modelBuilder.Entity<Nominee>(entity =>
        {
            entity.HasIndex(x => new { x.CategoryId, x.Name }).IsUnique();
        });

        modelBuilder.Entity<Ballot>(entity =>
        {
            entity.HasIndex(x => new { x.UserId, x.CategoryId, x.Rank }).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.CategoryId, x.NomineeId }).IsUnique();
            entity.ToTable(x => x.HasCheckConstraint("CK_Ballots_Rank", "Rank IN (1, 2)"));
        });

        modelBuilder.Entity<Winner>(entity =>
        {
            entity.HasKey(x => x.CategoryId);

            entity.HasOne(x => x.Category)
                .WithOne(x => x.Winner)
                .HasForeignKey<Winner>(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.WinnerNominee)
                .WithMany(x => x.WinningCategories)
                .HasForeignKey(x => x.WinnerNomineeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
