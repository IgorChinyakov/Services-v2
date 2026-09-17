using System.Net;
using System.Net.Http.Json;
using DirectoryService.Domain.Entities;
using DirectoryService.Presentation.ApiResponse;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public sealed class DepartmentPositionLinkTests : DirectoryServiceTestsBase
{
    public DepartmentPositionLinkTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task AddDepartmentPosition_WhenEntitiesExist_ShouldCreateLink()
    {
        // Arrange
        var departmentId = await SeedDepartmentAsync("Sales Department", "sales");
        var positionId = await SeedPositionAsync("Sales Manager", []);
        using var client = CreateClient();

        // Act
        var response = await client.PostAsync(
            $"/api/departments/{departmentId.Value}/positions/{positionId.Value}",
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await ExecuteDbContextAsync(async dbContext =>
        {
            (await dbContext.Set<DepartmentPosition>().AnyAsync(link =>
                link.DepartmentId == departmentId && link.PositionId == positionId)).Should().BeTrue();
        });
    }

    [Fact]
    public async Task AddDepartmentPosition_WhenLinkExists_ShouldReturnConflict()
    {
        // Arrange
        var departmentId = await SeedDepartmentAsync("Sales Department", "sales");
        var positionId = await SeedPositionAsync("Sales Manager", [departmentId]);
        using var client = CreateClient();

        // Act
        var response = await client.PostAsync(
            $"/api/departments/{departmentId.Value}/positions/{positionId.Value}",
            null);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        envelope!.Errors.Should().ContainSingle(error => error.Code == "department.position.duplicate");
    }

    [Fact]
    public async Task AddDepartmentPosition_WhenPositionDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var departmentId = await SeedDepartmentAsync("Sales Department", "sales");
        using var client = CreateClient();

        // Act
        var response = await client.PostAsync(
            $"/api/departments/{departmentId.Value}/positions/{Guid.NewGuid()}",
            null);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        envelope!.Errors.Should().ContainSingle(error => error.Code == "position.not_found");
    }

    [Fact]
    public async Task RemoveDepartmentPosition_WhenLinkExists_ShouldDeleteLinkOnly()
    {
        // Arrange
        var departmentId = await SeedDepartmentAsync("Sales Department", "sales");
        var positionId = await SeedPositionAsync("Sales Manager", [departmentId]);
        using var client = CreateClient();

        // Act
        var response = await client.DeleteAsync(
            $"/api/departments/{departmentId.Value}/positions/{positionId.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await ExecuteDbContextAsync(async dbContext =>
        {
            (await dbContext.Set<DepartmentPosition>().AnyAsync(link =>
                link.DepartmentId == departmentId && link.PositionId == positionId)).Should().BeFalse();
            (await dbContext.Departments.AnyAsync(item => item.Id == departmentId)).Should().BeTrue();
            (await dbContext.Positions.AnyAsync(item => item.Id == positionId)).Should().BeTrue();
        });
    }

    [Fact]
    public async Task RemoveDepartmentPosition_WhenLinkDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var departmentId = await SeedDepartmentAsync("Sales Department", "sales");
        var positionId = await SeedPositionAsync("Sales Manager", []);
        using var client = CreateClient();

        // Act
        var response = await client.DeleteAsync(
            $"/api/departments/{departmentId.Value}/positions/{positionId.Value}");
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        envelope!.Errors.Should().ContainSingle(error => error.Code == "department.position.not_found");
    }
}
