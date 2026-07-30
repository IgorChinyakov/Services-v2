using DirectoryService.Application.Features.Departments.GetTopByPositions;
using DirectoryService.Contracts.Departments;
using DirectoryService.Domain.Entities.Ids;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public sealed class GetTopDepartmentsByPositionsTests : DirectoryServiceTestsBase
{
    public GetTopDepartmentsByPositionsTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetTopByPositions_WithMoreThanFiveDepartments_ShouldReturnTopFiveByActivePositionCount()
    {
        // Arrange
        var departments = new[]
        {
            await SeedDepartmentAsync("Alpha Department", "alpha"),
            await SeedDepartmentAsync("Bravo Department", "bravo"),
            await SeedDepartmentAsync("Charlie Department", "charlie"),
            await SeedDepartmentAsync("Delta Department", "delta"),
            await SeedDepartmentAsync("Echo Department", "echo"),
            await SeedDepartmentAsync("Foxtrot Department", "foxtrot"),
            await SeedDepartmentAsync("Golf Department", "golf"),
        };

        for (var positionNumber = 0; positionNumber < 6; positionNumber++)
        {
            await SeedPositionAsync(
                $"Active Position {positionNumber + 1}",
                departments.Take(6 - positionNumber).ToArray());
        }

        await SeedPositionAsync(
            "Inactive Position",
            [departments[0], departments[6]],
            isActive: false);

        var inactiveDepartmentId = await SeedDepartmentAsync(
            "Inactive Department",
            "inactive");

        await ExecuteDbContextAsync(dbContext =>
            dbContext.Departments
                .Where(department => department.Id == inactiveDepartmentId)
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(department => department.IsActive, false)));

        for (var positionNumber = 0; positionNumber < 7; positionNumber++)
        {
            await SeedPositionAsync(
                $"Inactive Department Position {positionNumber + 1}",
                [inactiveDepartmentId]);
        }

        var query = new GetTopDepartmentsByPositionsQuery();

        // Act
        var result = await ExecuteQueryAsync<
            GetTopDepartmentsByPositionsQuery,
            IReadOnlyList<TopDepartmentByPositionsDto>>(query);

        // Assert
        result.IsSuccess.Should().BeTrue(
            result.IsFailure ? result.Error.GetMessage() : string.Empty);

        result.Value.Should().HaveCount(5);
        result.Value.Select(department => department.Identifier)
            .Should()
            .Equal("alpha", "bravo", "charlie", "delta", "echo");
        result.Value.Select(department => department.PositionsCount)
            .Should()
            .Equal(6, 5, 4, 3, 2);
        result.Value.Should().NotContain(department =>
            department.Id == inactiveDepartmentId.Value);
    }

    [Fact]
    public async Task GetTopByPositions_WithNoActivePositions_ShouldReturnDepartmentsWithZeroCount()
    {
        // Arrange
        var emptyDepartmentId = await SeedDepartmentAsync(
            "Empty Department",
            "empty");
        var inactivePositionDepartmentId = await SeedDepartmentAsync(
            "Inactive Position Department",
            "inactiveposition");

        await SeedPositionAsync(
            "Only Inactive Position",
            [inactivePositionDepartmentId],
            isActive: false);

        var query = new GetTopDepartmentsByPositionsQuery();

        // Act
        var result = await ExecuteQueryAsync<
            GetTopDepartmentsByPositionsQuery,
            IReadOnlyList<TopDepartmentByPositionsDto>>(query);

        // Assert
        result.IsSuccess.Should().BeTrue(
            result.IsFailure ? result.Error.GetMessage() : string.Empty);

        result.Value.Should().BeEquivalentTo(
            [
                new
                {
                    Id = emptyDepartmentId.Value,
                    Identifier = "empty",
                    PositionsCount = 0L,
                },
                new
                {
                    Id = inactivePositionDepartmentId.Value,
                    Identifier = "inactiveposition",
                    PositionsCount = 0L,
                },
            ],
            options => options.ExcludingMissingMembers());
    }
}
