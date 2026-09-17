using System.Net;
using System.Net.Http.Json;
using DirectoryService.Contracts.Positions.Requests;
using DirectoryService.Domain.Entities;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Presentation.ApiResponse;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public sealed class CreatePositionTests : DirectoryServiceTestsBase
{
    public CreatePositionTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CreatePosition_WithValidData_ShouldReturnCreatedAndPersistPositionWithLinks()
    {
        // Arrange
        var firstDepartmentId = await SeedDepartmentAsync("Development Department", "development");
        var secondDepartmentId = await SeedDepartmentAsync("Platform Department", "platform");
        using var client = CreateClient();
        var request = new CreatePositionRequest
        {
            Name = "Software Developer",
            Description = "Develops company services",
            DepartmentIds = [firstDepartmentId.Value, secondDepartmentId.Value],
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/positions", request);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<Guid>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        envelope!.IsError.Should().BeFalse();
        await ExecuteDbContextAsync(async dbContext =>
        {
            var positionId = PositionId.Create(envelope.Result);
            var position = await dbContext.Positions.SingleAsync(item => item.Id == positionId);
            var departmentIds = await dbContext.Set<DepartmentPosition>()
                .Where(link => link.PositionId == positionId)
                .Select(link => link.DepartmentId)
                .ToListAsync();

            position.Name.Value.Should().Be(request.Name);
            position.Description!.Value.Should().Be(request.Description);
            departmentIds.Should().BeEquivalentTo([firstDepartmentId, secondDepartmentId]);
        });
    }

    [Fact]
    public async Task CreatePosition_WithDuplicateDepartmentIds_ShouldReturnBadRequestAndNotPersistPosition()
    {
        // Arrange
        var departmentId = await SeedDepartmentAsync("Development Department", "development");
        using var client = CreateClient();
        var request = new CreatePositionRequest
        {
            Name = "Software Developer",
            DepartmentIds = [departmentId.Value, departmentId.Value],
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/positions", request);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        envelope!.Errors.Should().Contain(error => error.InvalidField == "DepartmentIds");
        await ExecuteDbContextAsync(async dbContext =>
        {
            (await dbContext.Positions.CountAsync()).Should().Be(0);
        });
    }
}
