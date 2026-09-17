using DirectoryService.Application.Abstractions.Database;
using DirectoryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.Infrastructure.Database;

public class DirectoryServiceDbContext : DbContext, IReadDbContext
{
    public DirectoryServiceDbContext(DbContextOptions<DirectoryServiceDbContext> options)
        : base(options)
    {
    }

    public DbSet<Location> Locations => Set<Location>();

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<Position> Positions => Set<Position>();

    IQueryable<Location> IReadDbContext.Locations => Locations.AsNoTracking();

    IQueryable<Department> IReadDbContext.Departments => Departments.AsNoTracking();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("ltree");
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DirectoryServiceDbContext).Assembly);

        modelBuilder.Entity<Location>()
            .HasQueryFilter(location => location.IsActive);

        modelBuilder.Entity<Department>()
            .HasQueryFilter(department => department.IsActive);

        modelBuilder.Entity<Position>()
            .HasQueryFilter(position => position.IsActive);

        modelBuilder.Entity<DepartmentLocation>()
            .HasQueryFilter(link => link.Department.IsActive && link.Location.IsActive);

        modelBuilder.Entity<DepartmentPosition>()
            .HasQueryFilter(link => link.Department.IsActive && link.Position.IsActive);
    }
}
