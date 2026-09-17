using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Features.Departments.CleanupInactive;
using DirectoryService.Application.Features.Locations.CleanupInactive;
using DirectoryService.Application.Features.Positions.CleanupInactive;
using DirectoryService.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public sealed class CleanupInactiveLocationAndPositionTests : DirectoryServiceTestsBase
{
    public CleanupInactiveLocationAndPositionTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CleanupLocations_WhenOldAndRecentLocationsExist_ShouldDeleteOnlyOldLocationAndItsLinks()
    {
        // Arrange
        var oldLocationId = await SeedLocationAsync("Old location");
        var recentLocationId = await SeedLocationAsync("Recent location");
        await SeedDepartmentAsync(
            "Location cleanup department",
            "location-cleanup",
            locationIds: [oldLocationId, recentLocationId]);

        var deletedBeforeUtc = DateTime.UtcNow.AddDays(-30);
        await ExecuteDbContextAsync(async dbContext =>
        {
            await dbContext.Locations
                .Where(location => location.Id == oldLocationId || location.Id == recentLocationId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(location => location.IsActive, false)
                    .SetProperty(location => location.DeletedAt, DateTime.UtcNow));

            await dbContext.Locations
                .IgnoreQueryFilters()
                .Where(location => location.Id == oldLocationId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(location => location.DeletedAt, deletedBeforeUtc.AddDays(-1)));
        });

        // Act
        var result = await ExecuteScopedAsync(async services =>
        {
            var handler = services.GetRequiredService<
                ICommandHandler<CleanupInactiveLocationsCommand, int>>();

            return await handler.Handle(
                new CleanupInactiveLocationsCommand(deletedBeforeUtc, 100),
                CancellationToken.None);
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);

        await ExecuteDbContextAsync(async dbContext =>
        {
            (await dbContext.Locations
                    .IgnoreQueryFilters()
                    .AnyAsync(location => location.Id == oldLocationId))
                .Should().BeFalse();

            (await dbContext.Locations
                    .IgnoreQueryFilters()
                    .AnyAsync(location => location.Id == recentLocationId))
                .Should().BeTrue();

            (await dbContext.Set<DepartmentLocation>()
                    .IgnoreQueryFilters()
                    .AnyAsync(link => link.LocationId == oldLocationId))
                .Should().BeFalse();

            (await dbContext.Set<DepartmentLocation>()
                    .IgnoreQueryFilters()
                    .AnyAsync(link => link.LocationId == recentLocationId))
                .Should().BeTrue();
        });
    }

    [Fact]
    public async Task CleanupPositions_WhenOldAndRecentPositionsExist_ShouldDeleteOnlyOldPositionAndItsLinks()
    {
        // Arrange
        var departmentId = await SeedDepartmentAsync("Position cleanup department", "position-cleanup");
        var oldPositionId = await SeedPositionAsync("Old position", [departmentId]);
        var recentPositionId = await SeedPositionAsync("Recent position", [departmentId]);

        var deletedBeforeUtc = DateTime.UtcNow.AddDays(-30);
        await ExecuteDbContextAsync(async dbContext =>
        {
            await dbContext.Positions
                .Where(position => position.Id == oldPositionId || position.Id == recentPositionId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(position => position.IsActive, false)
                    .SetProperty(position => position.DeletedAt, DateTime.UtcNow));

            await dbContext.Positions
                .IgnoreQueryFilters()
                .Where(position => position.Id == oldPositionId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(position => position.DeletedAt, deletedBeforeUtc.AddDays(-1)));
        });

        // Act
        var result = await ExecuteScopedAsync(async services =>
        {
            var handler = services.GetRequiredService<
                ICommandHandler<CleanupInactivePositionsCommand, int>>();

            return await handler.Handle(
                new CleanupInactivePositionsCommand(deletedBeforeUtc, 100),
                CancellationToken.None);
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);

        await ExecuteDbContextAsync(async dbContext =>
        {
            (await dbContext.Positions
                    .IgnoreQueryFilters()
                    .AnyAsync(position => position.Id == oldPositionId))
                .Should().BeFalse();

            (await dbContext.Positions
                    .IgnoreQueryFilters()
                    .AnyAsync(position => position.Id == recentPositionId))
                .Should().BeTrue();

            (await dbContext.Set<DepartmentPosition>()
                    .IgnoreQueryFilters()
                    .AnyAsync(link => link.PositionId == oldPositionId))
                .Should().BeFalse();

            (await dbContext.Set<DepartmentPosition>()
                    .IgnoreQueryFilters()
                    .AnyAsync(link => link.PositionId == recentPositionId))
                .Should().BeTrue();
        });
    }

    [Fact]
    public async Task CleanupLocations_WhenRowsExceedBatchSize_ShouldDeleteOneBatchPerInvocation()
    {
        // Arrange
        var firstLocationId = await SeedLocationAsync("First old location");
        var secondLocationId = await SeedLocationAsync("Second old location");
        var thirdLocationId = await SeedLocationAsync("Third old location");
        var deletedBeforeUtc = DateTime.UtcNow.AddDays(-30);

        await ExecuteDbContextAsync(async dbContext =>
        {
            await dbContext.Locations
                .Where(location =>
                    location.Id == firstLocationId ||
                    location.Id == secondLocationId ||
                    location.Id == thirdLocationId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(location => location.IsActive, false)
                    .SetProperty(location => location.DeletedAt, deletedBeforeUtc.AddDays(-1)));
        });

        // Act
        var firstResult = await ExecuteScopedAsync(async services =>
        {
            var handler = services.GetRequiredService<
                ICommandHandler<CleanupInactiveLocationsCommand, int>>();

            return await handler.Handle(
                new CleanupInactiveLocationsCommand(deletedBeforeUtc, 2),
                CancellationToken.None);
        });

        var remainingAfterFirstBatch = await ExecuteDbContextAsync(dbContext =>
            dbContext.Locations
                .IgnoreQueryFilters()
                .CountAsync());

        var secondResult = await ExecuteScopedAsync(async services =>
        {
            var handler = services.GetRequiredService<
                ICommandHandler<CleanupInactiveLocationsCommand, int>>();

            return await handler.Handle(
                new CleanupInactiveLocationsCommand(deletedBeforeUtc, 2),
                CancellationToken.None);
        });

        // Assert
        firstResult.IsSuccess.Should().BeTrue();
        firstResult.Value.Should().Be(2);
        remainingAfterFirstBatch.Should().Be(1);
        secondResult.IsSuccess.Should().BeTrue();
        secondResult.Value.Should().Be(1);

        (await ExecuteDbContextAsync(dbContext => dbContext.Locations
                .IgnoreQueryFilters()
                .CountAsync()))
            .Should().Be(0);
    }

    [Fact]
    public async Task CleanupDepartments_WhenOldAndRecentDepartmentsExist_ShouldDeleteOnlyOldDepartment()
    {
        // Arrange
        var oldDepartmentId = await SeedDepartmentAsync(
            "Old deleted department",
            "old-deleted-department",
            locationIds: []);
        var recentDepartmentId = await SeedDepartmentAsync(
            "Recent deleted department",
            "recent-deleted-department",
            locationIds: []);

        using var client = CreateClient();
        (await client.DeleteAsync($"/api/departments/{oldDepartmentId.Value}"))
            .IsSuccessStatusCode.Should().BeTrue();
        (await client.DeleteAsync($"/api/departments/{recentDepartmentId.Value}"))
            .IsSuccessStatusCode.Should().BeTrue();

        var deletedBeforeUtc = DateTime.UtcNow.AddDays(-30);
        await ExecuteDbContextAsync(async dbContext =>
        {
            await dbContext.Departments
                .IgnoreQueryFilters()
                .Where(department => department.Id == oldDepartmentId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(department => department.DeletedAt, deletedBeforeUtc.AddDays(-1)));
        });

        // Act
        var result = await ExecuteScopedAsync(async services =>
        {
            var handler = services.GetRequiredService<
                ICommandHandler<CleanupInactiveDepartmentsCommand, int>>();

            return await handler.Handle(
                new CleanupInactiveDepartmentsCommand(deletedBeforeUtc, 100),
                CancellationToken.None);
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);

        await ExecuteDbContextAsync(async dbContext =>
        {
            (await dbContext.Departments
                    .IgnoreQueryFilters()
                    .AnyAsync(department => department.Id == oldDepartmentId))
                .Should().BeFalse();

            (await dbContext.Departments
                    .IgnoreQueryFilters()
                    .AnyAsync(department => department.Id == recentDepartmentId))
                .Should().BeTrue();
        });
    }
}
