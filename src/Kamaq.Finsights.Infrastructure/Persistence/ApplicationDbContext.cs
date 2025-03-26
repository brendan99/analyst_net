using Microsoft.EntityFrameworkCore;

namespace Kamaq.Finsights.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    
    // Define DbSets for your entities here
    // public DbSet<YourEntity> YourEntities { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure entity mappings here
        // modelBuilder.ApplyConfiguration(new YourEntityConfiguration());
    }
} 