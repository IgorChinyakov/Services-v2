using System.Net;
using System.Net.Http.Json;
using DirectoryService.Contracts.Positions;
using DirectoryService.Presentation.ApiResponse;
using FluentAssertions;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public sealed class GetPositionsTests : DirectoryServiceTestsBase
{
    public GetPositionsTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetPositions_WithDepartmentFilter_ShouldReturnCursorPages()
    {
        var selectedDepartment = await SeedDepartmentAsync("Selected Department", "selected");
        var otherDepartment = await SeedDepartmentAsync("Other Department", "other");
        var first = await SeedPositionAsync("First Position", [selectedDepartment]);
        var second = await SeedPositionAsync("Second Position", [selectedDepartment]);
        var third = await SeedPositionAsync("Third Position", [selectedDepartment]);
        await SeedPositionAsync("Other Position", [otherDepartment]);
        await SeedPositionAsync("Inactive Position", [selectedDepartment], isActive: false);
        using var client = CreateClient();

        var firstResponse = await client.GetAsync(
            $"/api/positions?size=2&departmentId={selectedDepartment.Value}");
        var firstEnvelope = await firstResponse.Content.ReadFromJsonAsync<Envelope<PositionCursorPageDto>>();

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstEnvelope!.IsError.Should().BeFalse();
        firstEnvelope.Result!.Items.Select(position => position.Id)
            .Should().Equal(first.Value, second.Value);
        firstEnvelope.Result.NextCursor.Should().NotBeNullOrEmpty();

        var nextResponse = await client.GetAsync(
            $"/api/positions?size=2&departmentId={selectedDepartment.Value}" +
            $"&cursor={Uri.EscapeDataString(firstEnvelope.Result.NextCursor!)}");
        var nextEnvelope = await nextResponse.Content.ReadFromJsonAsync<Envelope<PositionCursorPageDto>>();

        nextResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        nextEnvelope!.Result!.Items.Select(position => position.Id)
            .Should().Equal(third.Value);
        nextEnvelope.Result.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task GetPositions_WithCursorFromAnotherDepartment_ShouldReturnBadRequest()
    {
        var firstDepartment = await SeedDepartmentAsync("First Department", "first");
        var secondDepartment = await SeedDepartmentAsync("Second Department", "second");
        await SeedPositionAsync("Position One", [firstDepartment]);
        await SeedPositionAsync("Position Two", [firstDepartment]);
        using var client = CreateClient();

        var response = await client.GetFromJsonAsync<Envelope<PositionCursorPageDto>>(
            $"/api/positions?size=1&departmentId={firstDepartment.Value}");
        var cursor = response!.Result!.NextCursor;

        var invalidResponse = await client.GetAsync(
            $"/api/positions?size=1&departmentId={secondDepartment.Value}" +
            $"&cursor={Uri.EscapeDataString(cursor!)}");
        var invalidEnvelope = await invalidResponse.Content.ReadFromJsonAsync<Envelope<PositionCursorPageDto>>();

        invalidResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        invalidEnvelope!.Errors.Should().ContainSingle(error => error.Code == "positions.cursor.invalid");
    }
}
