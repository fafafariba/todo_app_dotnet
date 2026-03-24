using Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Todo> Todos => Set<Todo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global query filters for soft deletes
        modelBuilder.Entity<User>().HasQueryFilter(u => u.DeletedAt == null);
        modelBuilder.Entity<Todo>().HasQueryFilter(t => t.DeletedAt == null);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<User>())
        {
            if (entry.State == EntityState.Added) { entry.Entity.CreatedAt = now; entry.Entity.UpdatedAt = now; }
            else if (entry.State == EntityState.Modified) { entry.Entity.UpdatedAt = now; }
        }

        foreach (var entry in ChangeTracker.Entries<Todo>())
        {
            if (entry.State == EntityState.Added) { entry.Entity.CreatedAt = now; entry.Entity.UpdatedAt = now; }
            else if (entry.State == EntityState.Modified) { entry.Entity.UpdatedAt = now; }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
