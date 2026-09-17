using DirectoryService.Domain.Entities;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DepartmentName = DirectoryService.Domain.ValueObjects.Department.Name;
using DepartmentSlug = DirectoryService.Domain.ValueObjects.Department.Slug;
using LocationAddress = DirectoryService.Domain.ValueObjects.Location.Address;
using LocationName = DirectoryService.Domain.ValueObjects.Location.Name;
using PositionName = DirectoryService.Domain.ValueObjects.Position.Name;

namespace DirectoryService.IntegrationTests;

public class DirectoryServiceTestsBase : IAsyncLifetime
{
    protected readonly IServiceProvider Services;
    private readonly DirectoryServiceWebFactory _factory;
    private readonly Func<Task> _resetDatabase;
    private int _locationNumber;

    protected DirectoryServiceTestsBase(DirectoryServiceWebFactory factory)
    {
        _factory = factory;
        Services = factory.Services;
        _resetDatabase = factory.ResetDatabaseAsync;
    }

    public async Task InitializeAsync()
    {
        await _resetDatabase();
    }

    public async Task DisposeAsync()
    {
        await _resetDatabase();
    }

    protected HttpClient CreateClient() => _factory.CreateClient();

    protected async Task<TResult> ExecuteScopedAsync<TResult>(
        Func<IServiceProvider, Task<TResult>> action)
    {
        await using var scope = Services.CreateAsyncScope();

        return await action(scope.ServiceProvider);
    }

    protected Task ExecuteScopedAsync(Func<IServiceProvider, Task> action)
    {
        return ExecuteScopedAsync(async services =>
        {
            await action(services);
            return true;
        });
    }

    protected Task<TResult> ExecuteDbContextAsync<TResult>(
        Func<DirectoryServiceDbContext, Task<TResult>> action)
    {
        return ExecuteScopedAsync(services =>
        {
            var dbContext = services.GetRequiredService<DirectoryServiceDbContext>();

            return action(dbContext);
        });
    }

    protected Task ExecuteDbContextAsync(Func<DirectoryServiceDbContext, Task> action)
    {
        return ExecuteDbContextAsync(async dbContext =>
        {
            await action(dbContext);
            return true;
        });
    }

    protected Task<long> GetRedisDatabaseSizeAsync()
    {
        return _factory.GetRedisDatabaseSizeAsync();
    }

    protected async Task<LocationId> SeedLocationAsync(string? name = null)
    {
        return await ExecuteDbContextAsync(async dbContext =>
        {
            var number = ++_locationNumber;

            var location = new Location(
                LocationName.Create(name ?? $"Location {number}").Value,
                LocationAddress.Create(
                    $"City {number}",
                    $"Street {number}",
                    $"House {number}",
                    $"Apartment {number}").Value);

            await dbContext.Locations.AddAsync(location);
            await dbContext.SaveChangesAsync();

            return location.Id;
        });
    }

    protected async Task<DepartmentId> SeedDepartmentAsync(
        string name,
        string slug,
        DepartmentId? parentId = null,
        IReadOnlyCollection<LocationId>? locationIds = null)
    {
        var actualLocationIds = locationIds ?? [await SeedLocationAsync()];

        return await ExecuteDbContextAsync(async dbContext =>
        {
            var parent = parentId is null
                ? null
                : await dbContext.Departments.FindAsync(
                    keyValues: new object?[] { parentId },
                    cancellationToken: default);

            var department = new Department(
                DepartmentName.Create(name).Value,
                DepartmentSlug.Create(slug).Value,
                parent,
                actualLocationIds);

            await dbContext.Departments.AddAsync(department);
            await dbContext.SaveChangesAsync();

            return department.Id;
        });
    }

    protected async Task<PositionId> SeedPositionAsync(
        string name,
        IReadOnlyCollection<DepartmentId> departmentIds,
        bool isActive = true)
    {
        return await ExecuteDbContextAsync(async dbContext =>
        {
            var position = new Position(
                PositionName.Create(name).Value,
                null,
                departmentIds);

            await dbContext.Positions.AddAsync(position);
            await dbContext.SaveChangesAsync();

            if (!isActive)
            {
                await dbContext.Positions
                    .Where(item => item.Id == position.Id)
                    .ExecuteUpdateAsync(setters =>
                        setters.SetProperty(item => item.IsActive, false));
            }

            return position.Id;
        });
    }
}
