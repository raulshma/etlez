using ETLFramework.API.Services;
using ETLFramework.Configuration.Providers;
using ETLFramework.Core.Interfaces;
using ETLFramework.Pipeline;
using ETLFramework.Connectors;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ETL Framework API", Version = "v1" });
});

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ETL Framework API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();

// Add health check endpoint
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTimeOffset.UtcNow });

app.Run();
