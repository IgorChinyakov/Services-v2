using System.Net;
using System.Net.Http.Json;
using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Features.Locations.Create;
using DirectoryService.Contracts.Common;
using DirectoryService.Contracts.Locations.Requests;
using DirectoryService.Domain.Shared;
using DirectoryService.Presentation.ApiResponse;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public sealed class ApiResponseContractTests : DirectoryServiceTestsBase
{
    private readonly DirectoryServiceWebFactory _factory;

    public ApiResponseContractTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateLocation_WithValidRequest_ShouldReturnCreatedEnvelope()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var request = CreateValidLocationRequest();

        // Act
        var response = await client.PostAsJsonAsync("/api/locations", request);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<Guid>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        envelope.Should().NotBeNull();
        envelope!.IsError.Should().BeFalse();
        envelope.Errors.Should().BeEmpty();
        envelope.Result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateLocation_WithInvalidRequest_ShouldReturnAllValidationErrorsInEnvelope()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var request = new CreateLocationRequest
        {
            Name = string.Empty,
            Address = new AddressDto
            {
                City = string.Empty,
                Street = string.Empty,
                House = string.Empty,
                Apartment = string.Empty,
            },
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/locations", request);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        envelope.Should().NotBeNull();
        envelope!.IsError.Should().BeTrue();
        envelope.Result.Should().BeNull();
        envelope.Errors.Should().HaveCountGreaterThanOrEqualTo(5);
        envelope.Errors.Select(error => error.InvalidField).Should().Contain([
            "Name",
            "Address.City",
            "Address.Street",
            "Address.House",
            "Address.Apartment",
        ]);
    }

    [Fact]
    public async Task AddDepartmentLocation_WhenLinkExists_ShouldReturnConflictEnvelope()
    {
        // Arrange
        var locationId = await SeedLocationAsync();
        var departmentId = await SeedDepartmentAsync(
            "Sales Department",
            "sales",
            locationIds: [locationId]);
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsync(
            $"/api/departments/{departmentId.Value}/locations/{locationId.Value}",
            null);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        envelope.Should().NotBeNull();
        envelope!.IsError.Should().BeTrue();
        envelope.Errors.Should().ContainSingle(error => error.Code == "department.location.duplicate");
    }

    [Fact]
    public async Task CreateLocation_WhenUnexpectedExceptionOccurs_ShouldReturnSafeFailureEnvelope()
    {
        // Arrange
        using var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ICommandHandler<CreateLocationCommand, Guid>>();
                services.AddScoped<ICommandHandler<CreateLocationCommand, Guid>, ThrowingCreateLocationHandler>();
            });
        });
        using var client = customFactory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/locations", CreateValidLocationRequest());
        var responseBody = await response.Content.ReadAsStringAsync();
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        envelope.Should().NotBeNull();
        envelope!.Errors.Should().ContainSingle(error => error.Code == "failure");
        responseBody.Should().NotContain(ThrowingCreateLocationHandler.SensitiveExceptionMessage);
    }

    [Fact]
    public async Task OpenApi_ShouldDescribeEnvelopeAndCreatedResponse()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/openapi/v1.json");
        var document = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        document.Should().Contain("Envelope");
        document.Should().Contain("\"201\"");
        document.Should().Contain("/api/locations");
    }

    private static CreateLocationRequest CreateValidLocationRequest() =>
        new()
        {
            Name = "Main Office",
            Address = new AddressDto
            {
                City = "Moscow",
                Street = "Tverskaya",
                House = "12",
                Apartment = "10",
            },
        };

    private sealed class ThrowingCreateLocationHandler : ICommandHandler<CreateLocationCommand, Guid>
    {
        public const string SensitiveExceptionMessage = "Database credentials leaked";

        public Task<Result<Guid, Error>> Handle(
            CreateLocationCommand command,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException(SensitiveExceptionMessage);
        }
    }
}
