using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DirectoryService.Infrastructure.Database;

public sealed class DirectoryServiceDesignTimeDbContextFactory
    : IDesignTimeDbContextFactory<DirectoryServiceDbContext>
{
    public DirectoryServiceDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("Postgres__ConnectionString")
            ?? "Host=localhost;Database=directory_service_design;Username=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<DirectoryServiceDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new DirectoryServiceDbContext(optionsBuilder.Options);
    }
}
