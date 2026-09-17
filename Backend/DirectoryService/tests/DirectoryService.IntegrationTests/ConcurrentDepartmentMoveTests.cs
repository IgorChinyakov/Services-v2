using System.Net;
using System.Net.Http.Json;
using DirectoryService.Contracts.Departments.Requests;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Presentation.ApiResponse;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public sealed class ConcurrentDepartmentMoveTests : DirectoryServiceTestsBase
{
    public ConcurrentDepartmentMoveTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task MoveSameDepartmentToDifferentParents_Concurrently_ShouldKeepTreeConsistent()
    {
        // Arrange
        var firstParentId = await SeedDepartmentAsync("First Parent", "firstparent");
        var secondParentId = await SeedDepartmentAsync("Second Parent", "secondparent");
        var departmentId = await SeedDepartmentAsync("Moving Department", "movingdepartment");
        using var firstClient = CreateClient();
        using var secondClient = CreateClient();

        // Act
        var responses = await Task.WhenAll(
            MoveDepartmentAsync(firstClient, departmentId, firstParentId),
            MoveDepartmentAsync(secondClient, departmentId, secondParentId));

        // Assert
        responses.Should().Contain(response => response.StatusCode == HttpStatusCode.OK);
        responses.Should().OnlyContain(response =>
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.Conflict);

        foreach (var conflictResponse in responses.Where(response =>
                     response.StatusCode == HttpStatusCode.Conflict))
        {
            var envelope = await conflictResponse.Content.ReadFromJsonAsync<Envelope>();
            envelope!.Errors.Should().ContainSingle(error => error.Code == "department.move.conflict");
        }

        await ExecuteDbContextAsync(async dbContext =>
        {
            var department = await dbContext.Departments
                .AsNoTracking()
                .SingleAsync(item => item.Id == departmentId);

            department.ParentId.Should().BeOneOf(firstParentId, secondParentId);
            department.Path.Should().BeOneOf(
                "firstparent.movingdepartment",
                "secondparent.movingdepartment");
            department.Depth.Should().Be(1);
        });
        await AssertTreeIsConsistentAsync();
    }

    [Fact]
    public async Task MoveDepartmentsUnderEachOther_Concurrently_ShouldRejectCycle()
    {
        // Arrange
        var firstDepartmentId = await SeedDepartmentAsync("First Department", "firstdepartment");
        var secondDepartmentId = await SeedDepartmentAsync("Second Department", "seconddepartment");
        using var firstClient = CreateClient();
        using var secondClient = CreateClient();

        // Act
        var responses = await Task.WhenAll(
            MoveDepartmentAsync(firstClient, firstDepartmentId, secondDepartmentId),
            MoveDepartmentAsync(secondClient, secondDepartmentId, firstDepartmentId));

        // Assert
        responses.Count(response => response.StatusCode == HttpStatusCode.OK).Should().Be(1);
        responses.Count(response => response.StatusCode == HttpStatusCode.Conflict).Should().Be(1);

        var conflictResponse = responses.Single(response => response.StatusCode == HttpStatusCode.Conflict);
        var envelope = await conflictResponse.Content.ReadFromJsonAsync<Envelope>();
        envelope!.Errors.Should().ContainSingle(error => error.Code == "department.move.cycle");

        await AssertTreeIsConsistentAsync();
    }

    private static Task<HttpResponseMessage> MoveDepartmentAsync(
        HttpClient client,
        DepartmentId departmentId,
        DepartmentId parentId)
    {
        return client.PutAsJsonAsync(
            $"/api/departments/{departmentId.Value}/parent",
            new UpdateDepartmentParentRequest { ParentId = parentId.Value });
    }

    private Task AssertTreeIsConsistentAsync()
    {
        return ExecuteDbContextAsync(async dbContext =>
        {
            var departments = await dbContext.Departments
                .AsNoTracking()
                .ToListAsync();
            var departmentsById = departments.ToDictionary(department => department.Id);

            foreach (var department in departments)
            {
                if (department.ParentId is null)
                {
                    department.Path.Should().Be(department.Slug.Value);
                    department.Depth.Should().Be(0);
                }
                else
                {
                    departmentsById.TryGetValue(department.ParentId, out var parent).Should().BeTrue();
                    department.Path.Should().Be($"{parent!.Path}.{department.Slug.Value}");
                    department.Depth.Should().Be((short)(parent.Depth + 1));
                }

                var visitedDepartmentIds = new HashSet<DepartmentId>();
                var current = department;

                while (current.ParentId is not null)
                {
                    visitedDepartmentIds.Add(current.Id).Should().BeTrue();
                    current = departmentsById[current.ParentId];
                }
            }
        });
    }
}
