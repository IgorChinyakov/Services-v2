using DirectoryService.Application.Features.Departments.Update;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.Shared;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public sealed class UpdateDepartmentTests : DirectoryServiceTestsBase
{
    public UpdateDepartmentTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task UpdateDepartment_WithValidName_ShouldOnlyChangeName()
    {
        // Arrange
        var parentId = await SeedDepartmentAsync("Head Office", "head", locationIds: []);
        var departmentId = await SeedDepartmentAsync(
            "Sales Department",
            "sales",
            parentId,
            []);
        var command = new UpdateDepartmentCommand(departmentId.Value, "Enterprise Sales");

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.GetMessage() : string.Empty);
        await ExecuteDbContextAsync(async dbContext =>
        {
            var department = await dbContext.Departments
                .FirstAsync(item => item.Id == departmentId);

            department.Name.Value.Should().Be(command.Name);
            department.Slug.Value.Should().Be("sales");
            department.ParentId.Should().Be(parentId);
            department.Path.Should().Be("head.sales");
            department.Depth.Should().Be(1);
        });
    }

    [Fact]
    public async Task UpdateDepartment_WhenDepartmentDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var command = new UpdateDepartmentCommand(Guid.NewGuid(), "Enterprise Sales");

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Messages.Should().ContainSingle(message => message.Code == "department.not_found");
    }
}
