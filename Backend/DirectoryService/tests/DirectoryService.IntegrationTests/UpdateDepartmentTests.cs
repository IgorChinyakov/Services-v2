using System.Net;
using System.Net.Http.Json;
using DirectoryService.Contracts.Departments.Requests;
using DirectoryService.Presentation.ApiResponse;
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
    public async Task UpdateDepartment_WithValidName_ShouldReturnOkAndOnlyChangeName()
    {
        // Arrange
        var parentId = await SeedDepartmentAsync("Head Office", "head", locationIds: []);
        var departmentId = await SeedDepartmentAsync("Sales Department", "sales", parentId, []);
        using var client = CreateClient();
        var request = new UpdateDepartmentRequest { Name = "Enterprise Sales" };

        // Act
        var response = await client.PatchAsJsonAsync($"/api/departments/{departmentId.Value}", request);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope!.IsError.Should().BeFalse();
        await ExecuteDbContextAsync(async dbContext =>
        {
            var department = await dbContext.Departments.FirstAsync(item => item.Id == departmentId);
            department.Name.Value.Should().Be(request.Name);
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
        using var client = CreateClient();

        // Act
        var response = await client.PatchAsJsonAsync(
            $"/api/departments/{Guid.NewGuid()}",
            new UpdateDepartmentRequest { Name = "Enterprise Sales" });
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        envelope!.Errors.Should().ContainSingle(error => error.Code == "department.not_found");
    }
}
