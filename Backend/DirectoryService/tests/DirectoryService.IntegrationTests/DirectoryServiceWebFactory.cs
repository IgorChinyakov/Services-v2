using System.Data.Common;
using System.Globalization;
using DirectoryService.Api;
using DirectoryService.Application;
using DirectoryService.Infrastructure.Database;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace DirectoryService.IntegrationTests;

public class DirectoryServiceWebFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const int RedisPort = 6379;

    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres")
        .WithDatabase("directory_testing_db")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly IContainer _redisContainer = new ContainerBuilder("redis:7.4-alpine")
        .WithPortBinding(RedisPort, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted(["redis-cli", "ping"]))
        .Build();

    private Respawner? _respawner;
    private DbConnection? _dbConnection;
    private NpgsqlDataSource? _dataSource;
    private HybridCache? _hybridCache;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _postgresContainer.StartAsync(),
            _redisContainer.StartAsync());

        using var scope = Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
        _dataSource = scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
        _hybridCache = scope.ServiceProvider.GetRequiredService<HybridCache>();

        await context.Database.MigrateAsync();

        _dbConnection = new NpgsqlConnection(_postgresContainer.GetConnectionString());
        await _dbConnection.OpenAsync();

        await EnsureLtreeExtensionAsync();

        await InitializeRespawner();
    }

    public async Task ResetDatabaseAsync()
    {
        if (_respawner is null || _dbConnection is null || _dataSource is null || _hybridCache is null)
            throw new InvalidOperationException("Test database was not initialized.");

        await _respawner.ResetAsync(_dbConnection);
        await EnsureLtreeExtensionAsync();

        var flushResult = await _redisContainer.ExecAsync(["redis-cli", "FLUSHALL"]);
        if (flushResult.ExitCode != 0)
            throw new InvalidOperationException($"Failed to flush test Redis: {flushResult.Stderr}");

        await _hybridCache.RemoveByTagAsync(
            [CacheConstants.LOCATIONS_CACHE_TAG, CacheConstants.DEPARTMENTS_CACHE_TAG]);
    }

    public async Task<long> GetRedisDatabaseSizeAsync()
    {
        var result = await _redisContainer.ExecAsync(["redis-cli", "DBSIZE"]);
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"Failed to get test Redis database size: {result.Stderr}");

        return long.Parse(result.Stdout.Trim(), CultureInfo.InvariantCulture);
    }

    public new async Task DisposeAsync()
    {
        if (_dbConnection is not null)
            await _dbConnection.DisposeAsync();

        await base.DisposeAsync();
        await _redisContainer.StopAsync();
        await _redisContainer.DisposeAsync();
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
            services.RemoveAll<IDistributedCache>();

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

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration =
                    $"{_redisContainer.Hostname}:{_redisContainer.GetMappedPublicPort(RedisPort)}";
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
