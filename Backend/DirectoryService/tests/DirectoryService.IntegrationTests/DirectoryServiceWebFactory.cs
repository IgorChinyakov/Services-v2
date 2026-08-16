using System.Data.Common;
using DirectoryService.Api;
using DirectoryService.Infrastructure.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace DirectoryService.IntegrationTests;

public class DirectoryServiceWebFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres")
        .WithDatabase("directory_testing_db")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private Respawner? _respawner;
    private DbConnection? _dbConnection;
    private NpgsqlDataSource? _dataSource;

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        using var scope = Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
        _dataSource = scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>();

        await context.Database.MigrateAsync();

        _dbConnection = new NpgsqlConnection(_postgresContainer.GetConnectionString());
        await _dbConnection.OpenAsync();

        await EnsureLtreeExtensionAsync();

        await InitializeRespawner();
    }

    public async Task ResetDatabaseAsync()
    {
        if (_respawner is null || _dbConnection is null || _dataSource is null)
            throw new InvalidOperationException("Test database was not initialized.");

        await _respawner.ResetAsync(_dbConnection);
        await EnsureLtreeExtensionAsync();
    }

    public new async Task DisposeAsync()
    {
        if (_dbConnection is not null)
            await _dbConnection.DisposeAsync();

        await base.DisposeAsync();
        await _postgresContainer.StopAsync();
        await _postgresContainer.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DirectoryServiceDbContext>();
            services.RemoveAll<DbContextOptions<DirectoryServiceDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<NpgsqlDataSource>();

            services.AddSingleton(_ =>
            {
                var dataSourceBuilder = new NpgsqlSlimDataSourceBuilder(_postgresContainer.GetConnectionString());
                dataSourceBuilder.EnableArrays();
                dataSourceBuilder.EnableLTree();

                return dataSourceBuilder.Build();
            });

            services.AddDbContext<DirectoryServiceDbContext>((serviceProvider, options) =>
            {
                var dataSource = serviceProvider.GetRequiredService<NpgsqlDataSource>();

                options.UseNpgsql(dataSource);
            });
        });
    }

    private async Task InitializeRespawner()
    {
        if (_dbConnection is null)
            throw new InvalidOperationException("Test database connection was not initialized.");

        _respawner = await Respawner.CreateAsync(
            _dbConnection,
            new RespawnerOptions { DbAdapter = DbAdapter.Postgres, SchemasToInclude = ["public"] });
    }

    private async Task EnsureLtreeExtensionAsync()
    {
        if (_dbConnection is null || _dataSource is null)
            throw new InvalidOperationException("Test database was not initialized.");

        await using var command = _dbConnection.CreateCommand();
        command.CommandText = "CREATE EXTENSION IF NOT EXISTS ltree;";
        await command.ExecuteNonQueryAsync();

        await _dataSource.ReloadTypesAsync();
    }
}
