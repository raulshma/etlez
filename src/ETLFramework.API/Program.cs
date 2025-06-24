using ETLFramework.API.Services;
using ETLFramework.Configuration.Providers;
using ETLFramework.Core.Interfaces;
using ETLFramework.Pipeline;
using ETLFramework.Connectors;
using ETLFramework.Data.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add ETL Framework data services
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddETLFrameworkData(connectionString, options =>
{
    var dbConfig = builder.Configuration.GetSection("ETLFramework:Database");

    if (builder.Environment.IsDevelopment())
    {
        if (dbConfig.GetValue<bool>("EnableSensitiveDataLogging"))
            options.EnableSensitiveDataLogging();

        if (dbConfig.GetValue<bool>("EnableDetailedErrors"))
            options.EnableDetailedErrors();
    }
});

// Add ETL Framework services
builder.Services.AddSingleton<ETLFramework.Core.Interfaces.IConfigurationProvider, JsonConfigurationProvider>();
builder.Services.AddSingleton<IConnectorFactory, ConnectorFactory>();
builder.Services.AddSingleton<IPipelineOrchestrator, PipelineOrchestrator>();
builder.Services.AddScoped<IPipelineService, PipelineService>();

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Ensure database is created and up to date
try
{
    await app.Services.EnsureETLDatabaseAsync();
    app.Logger.LogInformation("Database initialization completed successfully");
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Failed to initialize database");
    throw;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "ETL Framework API";
        options.Theme = ScalarTheme.Solarized;
    });
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();

// Add health check endpoints
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTimeOffset.UtcNow });

app.MapGet("/health/database", async (IServiceProvider serviceProvider) =>
{
    try
    {
        var healthInfo = await serviceProvider.GetETLDatabaseHealthAsync();
        return Results.Ok(new
        {
            status = healthInfo.IsHealthy ? "healthy" : "unhealthy",
            database = new
            {
                canConnect = healthInfo.CanConnect,
                connectionTime = healthInfo.ConnectionTime.TotalMilliseconds,
                pipelineCount = healthInfo.PipelineCount,
                executionCount = healthInfo.ExecutionCount,
                provider = healthInfo.DatabaseProvider,
                error = healthInfo.Error
            },
            timestamp = DateTimeOffset.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.Ok(new
        {
            status = "unhealthy",
            database = new { error = ex.Message },
            timestamp = DateTimeOffset.UtcNow
        });
    }
});

app.Run();
