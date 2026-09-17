using System.Net;
using System.Net.Http.Json;
using DirectoryService.Contracts.Departments;
using DirectoryService.Presentation.ApiResponse;
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
    public async Task GetTopByPositions_WithMoreThanFiveDepartments_ShouldReturnTopFive()
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

        for (var number = 0; number < 6; number++)
            await SeedPositionAsync($"Active Position {number + 1}", departments.Take(6 - number).ToArray());

        await SeedPositionAsync("Inactive Position", [departments[0], departments[6]], isActive: false);
        var inactiveDepartmentId = await SeedDepartmentAsync("Inactive Department", "inactive");
        await ExecuteDbContextAsync(dbContext =>
            dbContext.Departments
                .Where(department => department.Id == inactiveDepartmentId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(department => department.IsActive, false)));
        using var client = CreateClient();

        // Act
        var response = await client.GetAsync("/api/departments/top-positions");
        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<IReadOnlyList<TopDepartmentByPositionsDto>>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope!.Result.Should().HaveCount(5);
        envelope.Result!.Select(department => department.Slug)
            .Should().Equal("alpha", "bravo", "charlie", "delta", "echo");
        envelope.Result.Select(department => department.PositionsCount)
            .Should().Equal(6, 5, 4, 3, 2);
        envelope.Result.Should().NotContain(department => department.Id == inactiveDepartmentId.Value);
    }

    [Fact]
    public async Task GetTopByPositions_WithNoActivePositions_ShouldReturnZeroCounts()
    {
        // Arrange
        var emptyDepartmentId = await SeedDepartmentAsync("Empty Department", "empty");
        var inactivePositionDepartmentId = await SeedDepartmentAsync(
            "Inactive Position Department",
            "inactiveposition");
        await SeedPositionAsync("Only Inactive Position", [inactivePositionDepartmentId], isActive: false);
        using var client = CreateClient();

        // Act
        var response = await client.GetAsync("/api/departments/top-positions");
        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<IReadOnlyList<TopDepartmentByPositionsDto>>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope!.Result.Should().BeEquivalentTo(
            [
                new { Id = emptyDepartmentId.Value, Slug = "empty", PositionsCount = 0L },
                new { Id = inactivePositionDepartmentId.Value, Slug = "inactiveposition", PositionsCount = 0L },
            ],
            options => options.ExcludingMissingMembers());
    }
}
