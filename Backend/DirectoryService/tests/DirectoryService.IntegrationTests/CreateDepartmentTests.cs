using System.Net;
using System.Net.Http.Json;
using DirectoryService.Contracts.Departments.Requests;
using DirectoryService.Domain.Entities;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Presentation.ApiResponse;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public sealed class CreateDepartmentTests : DirectoryServiceTestsBase
{
    public CreateDepartmentTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CreateDepartment_WithValidRootData_ShouldReturnCreatedAndPersistDepartment()
    {
        // Arrange
        var locationId = await SeedLocationAsync();
        using var client = CreateClient();
        var request = new CreateDepartmentRequest
        {
            Name = "Sales Department",
            Slug = "sales",
            LocationIds = [locationId.Value],
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/departments", request);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        envelope.Should().NotBeNull();
        envelope!.IsError.Should().BeFalse();
        var departmentId = DepartmentId.Create(Guid.Parse(envelope.Result!.ToString()!));

        await ExecuteDbContextAsync(async dbContext =>
        {
            var department = await dbContext.Departments
                .Include(item => item.DepartmentLocations)
                .FirstAsync(item => item.Id == departmentId);

            department.Name.Value.Should().Be(request.Name);
            department.Slug.Value.Should().Be(request.Slug);
            department.ParentId.Should().BeNull();
            department.Path.Should().Be("sales");
            department.Depth.Should().Be(0);
            department.DepartmentLocations.Should().ContainSingle(link => link.LocationId == locationId);
        });
    }

    [Fact]
    public async Task CreateDepartment_WithoutLocations_ShouldCreateDepartmentWithoutLinks()
    {
        // Arrange
        using var client = CreateClient();
        var request = new CreateDepartmentRequest
        {
            Name = "Remote Department",
            Slug = "remote",
            LocationIds = [],
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/departments", request);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var departmentId = DepartmentId.Create(Guid.Parse(envelope!.Result!.ToString()!));
        await ExecuteDbContextAsync(async dbContext =>
        {
            var department = await dbContext.Departments
                .Include(item => item.DepartmentLocations)
                .FirstAsync(item => item.Id == departmentId);

            department.Path.Should().Be(request.Slug);
            department.DepartmentLocations.Should().BeEmpty();
        });
    }

    [Fact]
    public async Task CreateDepartment_WithParentId_ShouldCreateChildDepartment()
    {
        // Arrange
        var parentId = await SeedDepartmentAsync("Head Office", "head");
        var childLocationId = await SeedLocationAsync();
        using var client = CreateClient();
        var request = new CreateDepartmentRequest
        {
            Name = "Human Resources",
            Slug = "human",
            ParentId = parentId.Value,
            LocationIds = [childLocationId.Value],
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/departments", request);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var departmentId = DepartmentId.Create(Guid.Parse(envelope!.Result!.ToString()!));
        await ExecuteDbContextAsync(async dbContext =>
        {
            var department = await dbContext.Departments
                .Include(item => item.DepartmentLocations)
                .FirstAsync(item => item.Id == departmentId);

            department.ParentId.Should().Be(parentId);
            department.Path.Should().Be("head.human");
            department.Depth.Should().Be(1);
            department.DepartmentLocations.Should().ContainSingle(link => link.LocationId == childLocationId);
        });
    }

    [Theory]
    [InlineData("Sales", "Slug")]
    [InlineData("b2b--sales", "Slug")]
    public async Task CreateDepartment_WithInvalidSlug_ShouldReturnBadRequest(
        string slug,
        string invalidField)
    {
        // Arrange
        using var client = CreateClient();
        var request = new CreateDepartmentRequest
        {
            Name = "Sales Department",
            Slug = slug,
            LocationIds = [],
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/departments", request);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        envelope!.Errors.Should().Contain(error => error.InvalidField == invalidField);
    }

    [Fact]
    public async Task CreateDepartment_WithMissingLocation_ShouldReturnNotFoundAndNotPersistDepartment()
    {
        // Arrange
        using var client = CreateClient();
        var request = new CreateDepartmentRequest
        {
            Name = "Sales Department",
            Slug = "sales",
            LocationIds = [Guid.NewGuid()],
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/departments", request);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        envelope!.Errors.Should().ContainSingle(error => error.Code == "location.not_found");
        await ExecuteDbContextAsync(async dbContext =>
        {
            (await dbContext.Departments.CountAsync()).Should().Be(0);
            (await dbContext.Set<DepartmentLocation>().CountAsync()).Should().Be(0);
        });
    }

    [Fact]
    public async Task CreateDepartment_WithDuplicateSlug_ShouldReturnConflict()
    {
        // Arrange
        await SeedDepartmentAsync("Sales Department", "sales");
        using var client = CreateClient();
        var request = new CreateDepartmentRequest
        {
            Name = "Another Sales Department",
            Slug = "sales",
            LocationIds = [],
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/departments", request);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        envelope!.Errors.Should().ContainSingle(error => error.Code == "department.slug.duplicate");
    }

    [Fact]
    public async Task CreateDepartment_WithDuplicateLocationIds_ShouldReturnBadRequest()
    {
        // Arrange
        var locationId = await SeedLocationAsync();
        using var client = CreateClient();
        var request = new CreateDepartmentRequest
        {
            Name = "Sales Department",
            Slug = "sales",
            LocationIds = [locationId.Value, locationId.Value],
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/departments", request);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        envelope!.Errors.Should().ContainSingle(error => error.InvalidField == "LocationIds");
    }

    [Fact]
    public async Task CreateDepartment_WithMissingParent_ShouldReturnNotFoundAndNotPersistDepartment()
    {
        // Arrange
        using var client = CreateClient();
        var request = new CreateDepartmentRequest
        {
            Name = "Sales Department",
            Slug = "sales",
            ParentId = Guid.NewGuid(),
            LocationIds = [],
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/departments", request);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        envelope!.Errors.Should().ContainSingle(error => error.Code == "department.not_found");
        await ExecuteDbContextAsync(async dbContext =>
        {
            (await dbContext.Departments.CountAsync()).Should().Be(0);
        });
    }
}
