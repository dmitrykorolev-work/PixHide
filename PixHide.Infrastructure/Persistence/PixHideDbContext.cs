using Microsoft.EntityFrameworkCore;

using PixHide.Domain.Entities;

namespace PixHide.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for working with SQLite. Contains DbSets for all entities, key configuration, indexes, etc.
/// </summary>
public class PixHideDbContext : DbContext
{
    public PixHideDbContext(DbContextOptions<PixHideDbContext> options) : base(options)
    {
        Database.EnsureCreated();
    }

    public DbSet<ImageItem> ImageItems { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ImageItems
        modelBuilder.Entity<ImageItem>(b =>
        {
            b.ToTable( "ImageItems" );
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }

    // Default configuration for SQLite if no options are provided
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite( "Data Source=pixhide.db" );
        }
    }
}