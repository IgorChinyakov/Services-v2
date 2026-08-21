using DirectoryService.Application.Features.Departments.Create;
using DirectoryService.Application.Features.Departments.GetChildren;
using DirectoryService.Application.Features.Departments.GetRoots;
using DirectoryService.Application.Features.Departments.GetTopByPositions;
using DirectoryService.Application.Features.Departments.SoftDelete;
using DirectoryService.Application.Features.Departments.UpdateParent;
using DirectoryService.Application.Features.Positions.Create;
using DirectoryService.Contracts.Common;
using DirectoryService.Contracts.Departments;
using FluentAssertions;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public sealed class DepartmentCachingTests : DirectoryServiceTestsBase
{
    public DepartmentCachingTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetRoots_WhenCacheIsEmpty_ShouldStoreResultInRedis()
    {
        // Arrange
        await SeedDepartmentAsync("Head Office", "head");
        var query = new GetRootDepartmentsQuery(Page: 1, Size: 20, Prefetch: 3);
        var keysBeforeQuery = await GetRedisDatabaseSizeAsync();

        // Act
        var result = await ExecuteQueryAsync<
            GetRootDepartmentsQuery,
            PagedList<RootDepartmentDto>>(query);
        var keysAfterQuery = await GetRedisDatabaseSizeAsync();

        // Assert
        result.IsSuccess.Should().BeTrue(
            result.IsFailure ? result.Error.GetMessage() : string.Empty);
        keysAfterQuery.Should().BeGreaterThan(keysBeforeQuery);
    }

    [Fact]
    public async Task GetRoots_WhenSameQueryIsRepeated_ShouldReturnCachedResult()
    {
        // Arrange
        await SeedDepartmentAsync("Head Office", "head");
        var query = new GetRootDepartmentsQuery(Page: 1, Size: 20, Prefetch: 3);

        var firstResult = await ExecuteQueryAsync<
            GetRootDepartmentsQuery,
            PagedList<RootDepartmentDto>>(query);

        await SeedDepartmentAsync("Sales Department", "sales");

        // Act
        var secondResult = await ExecuteQueryAsync<
            GetRootDepartmentsQuery,
            PagedList<RootDepartmentDto>>(query);

        // Assert
        firstResult.IsSuccess.Should().BeTrue(
            firstResult.IsFailure ? firstResult.Error.GetMessage() : string.Empty);
        secondResult.IsSuccess.Should().BeTrue(
            secondResult.IsFailure ? secondResult.Error.GetMessage() : string.Empty);
        secondResult.Value.Items.Select(department => department.Identifier)
            .Should()
            .Equal("head");
    }

    [Fact]
    public async Task GetRoots_WithDifferentPagination_ShouldUseDifferentCacheKeys()
    {
        // Arrange
        await SeedDepartmentAsync("Head Office", "head");
        await SeedDepartmentAsync("Sales Department", "sales");

        var firstPageQuery = new GetRootDepartmentsQuery(Page: 1, Size: 1, Prefetch: 3);
        var secondPageQuery = new GetRootDepartmentsQuery(Page: 2, Size: 1, Prefetch: 3);

        // Act
        var firstPageResult = await ExecuteQueryAsync<
            GetRootDepartmentsQuery,
            PagedList<RootDepartmentDto>>(firstPageQuery);
        var secondPageResult = await ExecuteQueryAsync<
            GetRootDepartmentsQuery,
            PagedList<RootDepartmentDto>>(secondPageQuery);

        // Assert
        firstPageResult.IsSuccess.Should().BeTrue(
            firstPageResult.IsFailure ? firstPageResult.Error.GetMessage() : string.Empty);
        secondPageResult.IsSuccess.Should().BeTrue(
            secondPageResult.IsFailure ? secondPageResult.Error.GetMessage() : string.Empty);
        firstPageResult.Value.Items.Should().ContainSingle();
        secondPageResult.Value.Items.Should().ContainSingle();
        secondPageResult.Value.Items[0].Id.Should().NotBe(firstPageResult.Value.Items[0].Id);
    }

    [Fact]
    public async Task CreateDepartment_WhenRootsAreCached_ShouldInvalidateCache()
    {
        // Arrange
        await SeedDepartmentAsync("Head Office", "head");
        var locationId = await SeedLocationAsync();
        var query = new GetRootDepartmentsQuery(Page: 1, Size: 20, Prefetch: 3);

        var cachedResult = await ExecuteQueryAsync<
            GetRootDepartmentsQuery,
            PagedList<RootDepartmentDto>>(query);
        cachedResult.IsSuccess.Should().BeTrue(
            cachedResult.IsFailure ? cachedResult.Error.GetMessage() : string.Empty);

        var command = new CreateDepartmentCommand(
            "Sales Department",
            "sales",
            null,
            [locationId.Value]);

        // Act
        var commandResult = await ExecuteCommandAsync<CreateDepartmentCommand, Guid>(command);
        var queryResult = await ExecuteQueryAsync<
            GetRootDepartmentsQuery,
            PagedList<RootDepartmentDto>>(query);

        // Assert
        commandResult.IsSuccess.Should().BeTrue(
            commandResult.IsFailure ? commandResult.Error.GetMessage() : string.Empty);
        queryResult.IsSuccess.Should().BeTrue(
            queryResult.IsFailure ? queryResult.Error.GetMessage() : string.Empty);
        queryResult.Value.Items.Select(department => department.Identifier)
            .Should()
            .BeEquivalentTo(["head", "sales"]);
    }

    [Fact]
    public async Task UpdateParent_WhenChildrenAreCached_ShouldInvalidateBothParentQueries()
    {
        // Arrange
        var firstParentId = await SeedDepartmentAsync("Head Office", "head");
        var secondParentId = await SeedDepartmentAsync("Sales Department", "sales");
        var childId = await SeedDepartmentAsync("Development Department", "development", firstParentId);

        var firstParentQuery = new GetDepartmentChildrenQuery(firstParentId.Value, Page: 1, Size: 20);
        var secondParentQuery = new GetDepartmentChildrenQuery(secondParentId.Value, Page: 1, Size: 20);

        var firstParentCachedResult = await ExecuteQueryAsync<
            GetDepartmentChildrenQuery,
            PagedList<DepartmentNodeDto>>(firstParentQuery);
        var secondParentCachedResult = await ExecuteQueryAsync<
            GetDepartmentChildrenQuery,
            PagedList<DepartmentNodeDto>>(secondParentQuery);
        firstParentCachedResult.IsSuccess.Should().BeTrue(
            firstParentCachedResult.IsFailure ? firstParentCachedResult.Error.GetMessage() : string.Empty);
        secondParentCachedResult.IsSuccess.Should().BeTrue(
            secondParentCachedResult.IsFailure ? secondParentCachedResult.Error.GetMessage() : string.Empty);

        var command = new UpdateDepartmentParentCommand(childId.Value, secondParentId.Value);

        // Act
        var commandResult = await ExecuteCommandAsync(command);
        var firstParentResult = await ExecuteQueryAsync<
            GetDepartmentChildrenQuery,
            PagedList<DepartmentNodeDto>>(firstParentQuery);
        var secondParentResult = await ExecuteQueryAsync<
            GetDepartmentChildrenQuery,
            PagedList<DepartmentNodeDto>>(secondParentQuery);

        // Assert
        commandResult.IsSuccess.Should().BeTrue(
            commandResult.IsFailure ? commandResult.Error.GetMessage() : string.Empty);
        firstParentResult.IsSuccess.Should().BeTrue(
            firstParentResult.IsFailure ? firstParentResult.Error.GetMessage() : string.Empty);
        secondParentResult.IsSuccess.Should().BeTrue(
            secondParentResult.IsFailure ? secondParentResult.Error.GetMessage() : string.Empty);
        firstParentResult.Value.Items.Should().BeEmpty();
        secondParentResult.Value.Items.Should().ContainSingle(department => department.Id == childId.Value);
    }

    [Fact]
    public async Task SoftDeleteDepartment_WhenRootsAreCached_ShouldInvalidateCache()
    {
        // Arrange
        await SeedDepartmentAsync("Head Office", "head");
        var departmentToDeleteId = await SeedDepartmentAsync("Sales Department", "sales");
        var query = new GetRootDepartmentsQuery(Page: 1, Size: 20, Prefetch: 3);

        var cachedResult = await ExecuteQueryAsync<
            GetRootDepartmentsQuery,
            PagedList<RootDepartmentDto>>(query);
        cachedResult.IsSuccess.Should().BeTrue(
            cachedResult.IsFailure ? cachedResult.Error.GetMessage() : string.Empty);

        var command = new SoftDeleteDepartmentCommand(departmentToDeleteId.Value);

        // Act
        var commandResult = await ExecuteCommandAsync(command);
        var queryResult = await ExecuteQueryAsync<
            GetRootDepartmentsQuery,
            PagedList<RootDepartmentDto>>(query);

        // Assert
        commandResult.IsSuccess.Should().BeTrue(
            commandResult.IsFailure ? commandResult.Error.GetMessage() : string.Empty);
        queryResult.IsSuccess.Should().BeTrue(
            queryResult.IsFailure ? queryResult.Error.GetMessage() : string.Empty);
        queryResult.Value.Items.Select(department => department.Identifier)
            .Should()
            .Equal("head");
    }

    [Fact]
    public async Task CreatePosition_WhenTopDepartmentsAreCached_ShouldInvalidateCache()
    {
        // Arrange
        var departmentId = await SeedDepartmentAsync("Development Department", "development");
        var query = new GetTopDepartmentsByPositionsQuery();

        var cachedResult = await ExecuteQueryAsync<
            GetTopDepartmentsByPositionsQuery,
            IReadOnlyList<TopDepartmentByPositionsDto>>(query);
        cachedResult.IsSuccess.Should().BeTrue(
            cachedResult.IsFailure ? cachedResult.Error.GetMessage() : string.Empty);
        cachedResult.Value.Should().ContainSingle(department =>
            department.Id == departmentId.Value && department.PositionsCount == 0);

        var command = new CreatePositionCommand(
            "Software Developer",
            null,
            [departmentId.Value]);

        // Act
        var commandResult = await ExecuteCommandAsync<CreatePositionCommand, Guid>(command);
        var queryResult = await ExecuteQueryAsync<
            GetTopDepartmentsByPositionsQuery,
            IReadOnlyList<TopDepartmentByPositionsDto>>(query);

        // Assert
        commandResult.IsSuccess.Should().BeTrue(
            commandResult.IsFailure ? commandResult.Error.GetMessage() : string.Empty);
        queryResult.IsSuccess.Should().BeTrue(
            queryResult.IsFailure ? queryResult.Error.GetMessage() : string.Empty);
        queryResult.Value.Should().ContainSingle(department =>
            department.Id == departmentId.Value && department.PositionsCount == 1);
    }
}
