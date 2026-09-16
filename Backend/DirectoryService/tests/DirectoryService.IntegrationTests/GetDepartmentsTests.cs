using System.Net;
using System.Net.Http.Json;
using DirectoryService.Contracts.Common;
using DirectoryService.Contracts.Departments;
using DirectoryService.Presentation.ApiResponse;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public sealed class GetDepartmentsTests : DirectoryServiceTestsBase
{
    public static TheoryData<string> InvalidQueryParameters => new()
    {
        "page=0",
        "pageSize=0",
        "pageSize=101",
        "sortBy=unknown",
        "sortDir=unknown",
        $"search={new string('a', 151)}",
    };

    private readonly DirectoryServiceWebFactory _factory;

    public GetDepartmentsTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetDepartments_WithoutParameters_ShouldReturnFirstPageWithDefaults()
    {
        // Arrange
        for (var index = 0; index < 25; index++)
        {
            await SeedDepartmentAsync(
                $"Department {index:D2}",
                $"department{GetAlphabeticSuffix(index)}");
        }

        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/departments");
        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<PagedList<DepartmentListItemDto>>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope.Should().NotBeNull();
        envelope!.IsError.Should().BeFalse();
        envelope.Result.Should().NotBeNull();
        envelope.Result!.Page.Should().Be(1);
        envelope.Result.PageSize.Should().Be(20);
        envelope.Result.TotalCount.Should().Be(25);
        envelope.Result.Items.Should().HaveCount(20);
        envelope.Result.Items.Select(item => item.Name).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetDepartments_SecondPage_ShouldReturnRemainingItemsAndPreserveTotalCount()
    {
        // Arrange
        for (var index = 0; index < 25; index++)
        {
            await SeedDepartmentAsync(
                $"Department {index:D2}",
                $"department{GetAlphabeticSuffix(index)}");
        }

        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/departments?page=2&pageSize=20");
        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<PagedList<DepartmentListItemDto>>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope.Should().NotBeNull();
        envelope!.Result.Should().NotBeNull();
        envelope.Result!.Page.Should().Be(2);
        envelope.Result.PageSize.Should().Be(20);
        envelope.Result.TotalCount.Should().Be(25);
        envelope.Result.Items.Should().HaveCount(5);
    }

    [Theory]
    [InlineData("Sales Department", "sAlEs")]
    [InlineData("Отдел Продаж", "пРоДаЖ")]
    public async Task GetDepartments_Search_ShouldBeCaseInsensitive(
        string departmentName,
        string search)
    {
        // Arrange
        await SeedDepartmentAsync(departmentName, "searchdepartment");
        await SeedDepartmentAsync("Unrelated Department", "unrelateddepartment");
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/departments?search={search}");
        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<PagedList<DepartmentListItemDto>>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope.Should().NotBeNull();
        envelope!.Result.Should().NotBeNull();
        envelope.Result!.TotalCount.Should().Be(1);
        envelope.Result.Items.Should().ContainSingle(item => item.Name == departmentName);
    }

    [Fact]
    public async Task GetDepartments_SortByNameDescending_ShouldReturnOrderedPage()
    {
        // Arrange
        await SeedDepartmentAsync("Alpha Department", "alphadepartment");
        await SeedDepartmentAsync("Charlie Department", "charliedepartment");
        await SeedDepartmentAsync("Bravo Department", "bravodepartment");
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(
            "/api/departments?sortBy=name&sortDir=desc");
        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<PagedList<DepartmentListItemDto>>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope.Should().NotBeNull();
        envelope!.Result.Should().NotBeNull();
        envelope.Result!.Items.Select(item => item.Name).Should().ContainInOrder(
            "Charlie Department",
            "Bravo Department",
            "Alpha Department");
    }

    [Fact]
    public async Task GetDepartments_SortByCreatedAtDescending_ShouldReturnNewestFirst()
    {
        // Arrange
        var olderDepartmentId = await SeedDepartmentAsync("Older Department", "olderdepartment");
        var newerDepartmentId = await SeedDepartmentAsync("Newer Department", "newerdepartment");

        await ExecuteDbContextAsync(async dbContext =>
        {
            await dbContext.Departments
                .Where(department => department.Id == olderDepartmentId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(
                        department => department.CreatedAt,
                        DateTime.UtcNow.AddDays(-2)));

            await dbContext.Departments
                .Where(department => department.Id == newerDepartmentId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(
                        department => department.CreatedAt,
                        DateTime.UtcNow.AddDays(-1)));
        });

        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(
            "/api/departments?sortBy=createdAt&sortDir=desc");
        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<PagedList<DepartmentListItemDto>>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope.Should().NotBeNull();
        envelope!.Result.Should().NotBeNull();
        envelope.Result!.Items.Select(item => item.Id).Should().ContainInOrder(
            newerDepartmentId.Value,
            olderDepartmentId.Value);
    }

    [Theory]
    [MemberData(nameof(InvalidQueryParameters))]
    public async Task GetDepartments_WithInvalidParameters_ShouldReturnValidationError(
        string queryString)
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/departments?{queryString}");
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        envelope.Should().NotBeNull();
        envelope!.IsError.Should().BeTrue();
        envelope.Error.Should().NotBeNull();
        envelope.Error!.Messages.Should().NotBeEmpty();
    }

    private static string GetAlphabeticSuffix(int index)
    {
        return ((char)('a' + index)).ToString();
    }
}
