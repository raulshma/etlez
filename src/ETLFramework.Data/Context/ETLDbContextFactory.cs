using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ETLFramework.Data.Context;

/// <summary>
/// Design-time factory for ETLDbContext to support EF Core migrations.
/// </summary>
public class ETLDbContextFactory : IDesignTimeDbContextFactory<ETLDbContext>
{
    /// <summary>
    /// Creates a new instance of ETLDbContext for design-time operations.
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>A configured ETLDbContext instance</returns>
    public ETLDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ETLDbContext>();
        
        // Use a default connection string for migrations
        // This will be overridden at runtime with the actual connection string
        optionsBuilder.UseNpgsql("Host=localhost;Database=etlframework;Username=postgres;Password=password");
        
        return new ETLDbContext(optionsBuilder.Options);
    }
}
