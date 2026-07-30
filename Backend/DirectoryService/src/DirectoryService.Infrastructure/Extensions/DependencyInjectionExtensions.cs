using DirectoryService.Application.Abstractions.Database;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Infrastructure.Database;
using DirectoryService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace DirectoryService.Infrastructure.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(_ =>
        {
            string connectionString = configuration.GetSection(Constants.DATABASE).Value ??
                                      throw new ArgumentNullException(Constants.DATABASE);

            var dataSourceBuilder = new NpgsqlSlimDataSourceBuilder(connectionString);
            dataSourceBuilder.EnableLTree();

            return dataSourceBuilder.Build();
        });

        services.AddDbContext<DirectoryServiceDbContext>((sp, options) =>
        {
            var environment = sp.GetRequiredService<IHostEnvironment>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var dataSource = sp.GetRequiredService<NpgsqlDataSource>();

            options.UseNpgsql(dataSource);

            if (environment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }

            options.UseLoggerFactory(loggerFactory);
        });

        services.AddScoped<ITransactionManager, TransactionManager>();
        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IPositionRepository, PositionRepository>();

        return services;
    }
}
