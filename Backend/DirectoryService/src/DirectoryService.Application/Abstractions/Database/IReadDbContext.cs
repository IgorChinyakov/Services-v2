using DirectoryService.Domain.Entities;

namespace DirectoryService.Application.Abstractions.Database;

public interface IReadDbContext
{
    IQueryable<Location> Locations { get; }

    IQueryable<Department> Departments { get; }
}
