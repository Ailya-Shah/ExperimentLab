using ExperimentLab.Models;
using Microsoft.EntityFrameworkCore;

namespace ExperimentLab.Data;

/// <summary>
/// The EF Core database context. Each DbSet becomes a table.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Experiment> Experiments => Set<Experiment>();
    public DbSet<Variant> Variants => Set<Variant>();
    public DbSet<Event> Events => Set<Event>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // When an experiment is deleted, its variants go with it.
        modelBuilder.Entity<Experiment>()
            .HasMany(e => e.Variants)
            .WithOne(v => v.Experiment!)
            .HasForeignKey(v => v.ExperimentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
