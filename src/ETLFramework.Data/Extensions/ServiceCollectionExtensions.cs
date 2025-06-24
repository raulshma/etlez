using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ETLFramework.Data.Context;
using ETLFramework.Data.Repositories.Interfaces;
using ETLFramework.Data.Repositories.Implementations;

namespace ETLFramework.Data.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to register ETL Framework data services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds ETL Framework data services with PostgreSQL support.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="connectionString">The PostgreSQL connection string</param>
    /// <param name="configureOptions">Optional action to configure DbContext options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddETLFrameworkData(
        this IServiceCollection services,
        string connectionString,
        Action<DbContextOptionsBuilder>? configureOptions = null)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

        // Register DbContext
        services.AddDbContext<ETLDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
            });

            // Apply additional configuration if provided
            configureOptions?.Invoke(options);
        });

        // Register repositories
        services.AddScoped<IPipelineRepository, PipelineRepository>();
        services.AddScoped<IExecutionRepository, ExecutionRepository>();

        return services;
    }

    /// <summary>
    /// Adds ETL Framework data services with custom DbContext configuration.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureDbContext">Action to configure the DbContext</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddETLFrameworkData(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        if (configureDbContext == null)
            throw new ArgumentNullException(nameof(configureDbContext));

        // Register DbContext with custom configuration
        services.AddDbContext<ETLDbContext>(configureDbContext);

        // Register repositories
        services.AddScoped<IPipelineRepository, PipelineRepository>();
        services.AddScoped<IExecutionRepository, ExecutionRepository>();

        return services;
    }

    /// <summary>
    /// Adds ETL Framework data services for development with in-memory database.
    /// This is useful for testing and development scenarios.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="databaseName">The in-memory database name</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddETLFrameworkDataInMemory(
        this IServiceCollection services,
        string databaseName = "ETLFrameworkTestDb")
    {
        // Register DbContext with in-memory database
        services.AddDbContext<ETLDbContext>(options =>
        {
            options.UseInMemoryDatabase(databaseName);
            options.EnableSensitiveDataLogging();
        });

        // Register repositories
        services.AddScoped<IPipelineRepository, PipelineRepository>();
        services.AddScoped<IExecutionRepository, ExecutionRepository>();

        return services;
    }

    /// <summary>
    /// Ensures the database is created and applies any pending migrations.
    /// This should be called during application startup.
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the async operation</returns>
    public static async Task EnsureETLDatabaseAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ETLDbContext>();
        var logger = scope.ServiceProvider.GetService<ILogger<ETLDbContext>>();

        try
        {
            logger?.LogInformation("Ensuring ETL database is created and up to date...");
            
            await context.Database.MigrateAsync(cancellationToken);
            
            logger?.LogInformation("ETL database is ready");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to ensure ETL database");
            throw;
        }
    }

    /// <summary>
    /// Checks if the database can be connected to.
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connection is successful, false otherwise</returns>
    public static async Task<bool> CanConnectToETLDatabaseAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ETLDbContext>();
            
            return await context.Database.CanConnectAsync(cancellationToken);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets database health information.
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Database health information</returns>
    public static async Task<DatabaseHealthInfo> GetETLDatabaseHealthAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        var healthInfo = new DatabaseHealthInfo();

        try
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ETLDbContext>();

            var startTime = DateTimeOffset.UtcNow;
            healthInfo.CanConnect = await context.Database.CanConnectAsync(cancellationToken);
            healthInfo.ConnectionTime = DateTimeOffset.UtcNow - startTime;

            if (healthInfo.CanConnect)
            {
                healthInfo.PipelineCount = await context.Pipelines.CountAsync(cancellationToken);
                healthInfo.ExecutionCount = await context.Executions.CountAsync(cancellationToken);
                healthInfo.DatabaseProvider = context.Database.ProviderName;
            }
        }
        catch (Exception ex)
        {
            healthInfo.Error = ex.Message;
        }

        return healthInfo;
    }
}

/// <summary>
/// Database health information.
/// </summary>
public class DatabaseHealthInfo
{
    /// <summary>
    /// Gets or sets whether the database can be connected to.
    /// </summary>
    public bool CanConnect { get; set; }

    /// <summary>
    /// Gets or sets the connection time.
    /// </summary>
    public TimeSpan ConnectionTime { get; set; }

    /// <summary>
    /// Gets or sets the number of pipelines in the database.
    /// </summary>
    public int PipelineCount { get; set; }

    /// <summary>
    /// Gets or sets the number of executions in the database.
    /// </summary>
    public int ExecutionCount { get; set; }

    /// <summary>
    /// Gets or sets the database provider name.
    /// </summary>
    public string? DatabaseProvider { get; set; }

    /// <summary>
    /// Gets or sets any error message.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Gets whether the database is healthy.
    /// </summary>
    public bool IsHealthy => CanConnect && string.IsNullOrEmpty(Error);
}
