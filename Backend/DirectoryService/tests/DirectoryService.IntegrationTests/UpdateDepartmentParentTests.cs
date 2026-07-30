using DirectoryService.Application.Features.Departments.UpdateParent;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.Shared;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public class UpdateDepartmentParentTests : DirectoryServiceTestsBase
{
    public UpdateDepartmentParentTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task UpdateDepartmentParent_WithValidParent_ShouldMoveDepartmentSubtree()
    {
        // Arrange
        var hqId = await SeedDepartmentAsync("Head Office", "head");
        var salesId = await SeedDepartmentAsync("Sales Department", "sales", hqId);
        var regionalId = await SeedDepartmentAsync("Regional Department", "regional", salesId);
        var southId = await SeedDepartmentAsync("South Department", "south", regionalId);
        var command = new UpdateDepartmentParentCommand(
            regionalId.Value,
            hqId.Value);

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.GetMessage() : string.Empty);

        await ExecuteDbContextAsync(async dbContext =>
        {
            var hq = await dbContext.Departments.FirstAsync(x => x.Id == hqId);
            var sales = await dbContext.Departments.FirstAsync(x => x.Id == salesId);
            var regional = await dbContext.Departments.FirstAsync(x => x.Id == regionalId);
            var south = await dbContext.Departments.FirstAsync(x => x.Id == southId);

            hq.Path.Should().Be("head");
            hq.Depth.Should().Be(0);
            sales.Path.Should().Be("head.sales");
            sales.Depth.Should().Be(1);
            regional.ParentId.Should().Be(hqId);
            regional.Path.Should().Be("head.regional");
            regional.Depth.Should().Be(1);
            south.ParentId.Should().Be(regionalId);
            south.Path.Should().Be("head.regional.south");
            south.Depth.Should().Be(2);
        });
    }

    [Fact]
    public async Task UpdateDepartmentParent_WithNullParent_ShouldMoveDepartmentSubtreeToRoot()
    {
        // Arrange
        var hqId = await SeedDepartmentAsync("Head Office", "head");
        var regionalId = await SeedDepartmentAsync("Regional Department", "regional", hqId);
        var southId = await SeedDepartmentAsync("South Department", "south", regionalId);
        var command = new UpdateDepartmentParentCommand(
            regionalId.Value,
            null);

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.GetMessage() : string.Empty);

        await ExecuteDbContextAsync(async dbContext =>
        {
            var regional = await dbContext.Departments.FirstAsync(x => x.Id == regionalId);
            var south = await dbContext.Departments.FirstAsync(x => x.Id == southId);

            regional.ParentId.Should().BeNull();
            regional.Path.Should().Be("regional");
            regional.Depth.Should().Be(0);
            south.ParentId.Should().Be(regionalId);
            south.Path.Should().Be("regional.south");
            south.Depth.Should().Be(1);
        });
    }

    [Fact]
    public async Task UpdateDepartmentParent_WithDescendantParent_ShouldReturnConflictAndKeepTreeUnchanged()
    {
        // Arrange
        var hqId = await SeedDepartmentAsync("Head Office", "head");
        var salesId = await SeedDepartmentAsync("Sales Department", "sales", hqId);
        var regionalId = await SeedDepartmentAsync("Regional Department", "regional", salesId);
        var southId = await SeedDepartmentAsync("South Department", "south", regionalId);
        var command = new UpdateDepartmentParentCommand(
            salesId.Value,
            southId.Value);

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict, result.Error.GetMessage());

        await ExecuteDbContextAsync(async dbContext =>
        {
            var sales = await dbContext.Departments.FirstAsync(x => x.Id == salesId);
            var regional = await dbContext.Departments.FirstAsync(x => x.Id == regionalId);
            var south = await dbContext.Departments.FirstAsync(x => x.Id == southId);

            sales.ParentId.Should().Be(hqId);
            sales.Path.Should().Be("head.sales");
            sales.Depth.Should().Be(1);
            regional.ParentId.Should().Be(salesId);
            regional.Path.Should().Be("head.sales.regional");
            regional.Depth.Should().Be(2);
            south.ParentId.Should().Be(regionalId);
            south.Path.Should().Be("head.sales.regional.south");
            south.Depth.Should().Be(3);
        });
    }

    [Fact]
    public async Task UpdateDepartmentParent_WithMissingParent_ShouldReturnNotFoundError()
    {
        // Arrange
        var departmentId = await SeedDepartmentAsync("Sales Department", "sales");
        var command = new UpdateDepartmentParentCommand(
            departmentId.Value,
            Guid.NewGuid());

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound, result.Error.GetMessage());
    }

    [Fact]
    public async Task UpdateDepartmentParent_WithSameParentAndDepartmentIds_ShouldReturnValidationError()
    {
        // Arrange
        var departmentId = DepartmentId.New();
        var command = new UpdateDepartmentParentCommand(
            departmentId.Value,
            departmentId.Value);

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation, result.Error.GetMessage());
    }
}
