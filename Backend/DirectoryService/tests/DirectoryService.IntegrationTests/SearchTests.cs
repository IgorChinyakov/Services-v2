using System.Net;
using System.Net.Http.Json;
using DirectoryService.Contracts.Search;
using DirectoryService.Presentation.ApiResponse;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public sealed class SearchTests(DirectoryServiceWebFactory factory) : DirectoryServiceTestsBase(factory)
{
    [Fact]
    public async Task Search_ShouldReturnGroupedResultsWithSubtitlesAndNavigationContext()
    {
        var location = await SeedLocationAsync("Searchable Office");
        var alpha = await SeedDepartmentAsync("Searchable Alpha Team", "search-alpha");
        var beta = await SeedDepartmentAsync("Searchable Beta Team", "search-beta");
        var position = await SeedPositionAsync("Searchable Engineer", [beta, alpha]);
        using var client = CreateClient();

        var results = await Search(client, "  SEARCHABLE  ");

        results.Select(item => item.Type).Should().Equal("location", "department", "department", "position");
        var office = results.Single(item => item.Type == "location");
        office.Id.Should().Be(location.Value);
        office.Title.Should().Be("Searchable Office");
        office.Subtitle.Should().Contain("City").And.Contain("Street").And.Contain("House");
        office.DepartmentId.Should().BeNull();
        results.Should().Contain(item => item.Type == "department" && item.Id == alpha.Value && item.Subtitle == "search-alpha");

        var engineer = results.Single(item => item.Type == "position");
        engineer.Id.Should().Be(position.Value);
        engineer.Subtitle.Should().Be("Searchable Alpha Team, Searchable Beta Team");
        engineer.DepartmentId.Should().Be(alpha.Value);
        results.Should().OnlyContain(item => item.IsActive);
    }

    [Fact]
    public async Task Search_ShouldKeepInactiveDepartmentsButExcludeArchivedEntities()
    {
        var archivedLocation = await SeedLocationAsync("Visibility Office");
        var archivedDepartment = await SeedDepartmentAsync("Visibility Archived", "visibility-archived");
        var inactiveDepartment = await SeedDepartmentAsync("Visibility Inactive", "visibility-inactive");
        var position = await SeedPositionAsync("Visibility Position", [inactiveDepartment]);
        await ExecuteDbContextAsync(async db =>
        {
            (await db.Locations.SingleAsync(item => item.Id == archivedLocation)).SoftDelete();
            (await db.Departments.SingleAsync(item => item.Id == archivedDepartment)).SoftDelete();
            (await db.Positions.SingleAsync(item => item.Id == position)).SoftDelete();
            await db.SaveChangesAsync();
            await db.Departments.Where(item => item.Id == inactiveDepartment)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.IsActive, false));
        });
        using var client = CreateClient();

        var results = await Search(client, "Visibility");

        results.Should().ContainSingle(item => item.Type == "department" && item.Id == inactiveDepartment.Value && !item.IsActive);
    }

    [Fact]
    public async Task Search_PositionContext_ShouldNotExposeArchivedOrInactiveDepartments()
    {
        var active = await SeedDepartmentAsync("Active Context", "active-context");
        var inactive = await SeedDepartmentAsync("Inactive Context", "inactive-context");
        var archived = await SeedDepartmentAsync("Archived Context", "archived-context");
        await SeedPositionAsync("Context Engineer", [active, inactive, archived]);
        await ExecuteDbContextAsync(async db =>
        {
            (await db.Departments.SingleAsync(item => item.Id == archived)).SoftDelete();
            await db.SaveChangesAsync();
            await db.Departments.Where(item => item.Id == inactive)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.IsActive, false));
        });
        using var client = CreateClient();

        var results = await Search(client, "Engineer");

        results.Should().ContainSingle();
        results[0].Subtitle.Should().Be("Active Context");
        results[0].DepartmentId.Should().Be(active.Value);
    }

    [Fact]
    public async Task Search_PositionWithoutAvailableDepartment_ShouldStillReturnOneResult()
    {
        var department = await SeedDepartmentAsync("Unavailable Context", "unavailable-context");
        var position = await SeedPositionAsync("Independent Engineer", [department]);
        await ExecuteDbContextAsync(db => db.Departments.Where(item => item.Id == department)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.IsActive, false)));
        using var client = CreateClient();

        var results = await Search(client, "Independent");

        results.Should().ContainSingle(item => item.Type == "position" && item.Id == position.Value);
        results[0].DepartmentId.Should().BeNull();
        results[0].Subtitle.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_ShouldLimitEachTypeIndependentlyAndKeepStableOrder()
    {
        for (var index = 0; index < 7; index++)
        {
            await SeedLocationAsync($"Limited Office {index:D2}");
            var department = await SeedDepartmentAsync($"Limited Department {index:D2}", $"limited-{index}");
            await SeedPositionAsync($"Limited Position {index:D2}", [department]);
        }

        using var client = CreateClient();
        var results = await Search(client, "Limited");

        results.Should().HaveCount(15);
        foreach (var type in new[] { "location", "department", "position" })
        {
            results.Where(item => item.Type == type).Should().HaveCount(5);
            results.Where(item => item.Type == type).Should().OnlyContain(item =>
                !item.Title.EndsWith("05") && !item.Title.EndsWith("06"));
        }

        (await Search(client, "Limited")).Select(item => (item.Type, item.Id))
            .Should().Equal(results.Select(item => (item.Type, item.Id)));
    }

    [Theory]
    [InlineData("50%", "Office 50% Discount", "Office 500 Discount")]
    [InlineData("ab_", "Office ab_cd", "Office abXcd")]
    [InlineData("ab\\", "Office ab\\cd", "Office abcd")]
    public async Task Search_ShouldTreatPatternCharactersLiterally(string query, string match, string other)
    {
        var expected = await SeedLocationAsync(match);
        await SeedLocationAsync(other);
        using var client = CreateClient();

        (await Search(client, query)).Should().ContainSingle(item => item.Id == expected.Value);
    }

    [Fact]
    public async Task Search_ShouldMatchCyrillicNamesCaseInsensitively()
    {
        var location = await SeedLocationAsync("Ереванский офис");
        using var client = CreateClient();

        (await Search(client, "ЕРЕВАН")).Should().ContainSingle(item => item.Id == location.Value);
    }

    [Fact]
    public async Task Search_WithNoMatchesOrSqlText_ShouldReturnEmptyArray()
    {
        await SeedLocationAsync("Existing Office");
        using var client = CreateClient();

        (await Search(client, "unmatched text")).Should().BeEmpty();
        (await Search(client, "' OR 1=1 --")).Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("x")]
    [InlineData(" x ")]
    public async Task Search_ShouldRejectMissingAndShortQueries(string? query)
    {
        using var client = CreateClient();
        var url = query is null ? "/api/search" : $"/api/search?q={Uri.EscapeDataString(query)}";

        var response = await client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();
        envelope!.IsError.Should().BeTrue();
        envelope.Errors.Should().Contain(item => item.Code == "field.is.invalid" && item.InvalidField == "Q");
    }

    [Fact]
    public async Task Search_ShouldRejectQueriesLongerThanTwoHundredCharacters()
    {
        using var client = CreateClient();
        var response = await client.GetAsync($"/api/search?q={new string('x', 201)}");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static async Task<IReadOnlyList<GlobalSearchResultDto>> Search(HttpClient client, string query)
    {
        var response = await client.GetAsync($"/api/search?q={Uri.EscapeDataString(query)}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<IReadOnlyList<GlobalSearchResultDto>>>();
        envelope!.IsError.Should().BeFalse();
        return envelope.Result!;
    }
}
