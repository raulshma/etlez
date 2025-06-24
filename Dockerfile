# Use the official .NET 9 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Set the working directory
WORKDIR /app

# Copy solution file
COPY *.sln ./

# Copy project files
COPY src/ETLFramework.Core/*.csproj ./src/ETLFramework.Core/
COPY src/ETLFramework.Configuration/*.csproj ./src/ETLFramework.Configuration/
COPY src/ETLFramework.Connectors/*.csproj ./src/ETLFramework.Connectors/
COPY src/ETLFramework.Transformation/*.csproj ./src/ETLFramework.Transformation/
COPY src/ETLFramework.Pipeline/*.csproj ./src/ETLFramework.Pipeline/
COPY src/ETLFramework.API/*.csproj ./src/ETLFramework.API/
COPY src/ETLFramework.Host/*.csproj ./src/ETLFramework.Host/
COPY src/ETLFramework.Playground/*.csproj ./src/ETLFramework.Playground/

# Copy test project files
COPY tests/ETLFramework.Tests.Unit/*.csproj ./tests/ETLFramework.Tests.Unit/
COPY tests/ETLFramework.Tests.Integration/*.csproj ./tests/ETLFramework.Tests.Integration/
COPY tests/ETLFramework.Tests.Performance/*.csproj ./tests/ETLFramework.Tests.Performance/
COPY tests/ETLFramework.Tests/*.csproj ./tests/ETLFramework.Tests/

# Copy sample project files
COPY samples/ETLFramework.Samples/*.csproj ./samples/ETLFramework.Samples/

# Restore dependencies
RUN dotnet restore

# Copy the rest of the source code
COPY . .

# Build the application
RUN dotnet build -c Release --no-restore

# Publish the API project
RUN dotnet publish src/ETLFramework.API/ETLFramework.API.csproj -c Release -o /app/publish --no-restore

# Use the official .NET 9 runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

# Set the working directory
WORKDIR /app

# Create a non-root user
RUN groupadd -r etluser && useradd -r -g etluser etluser

# Copy the published application
COPY --from=build /app/publish .

# Create directories for data and logs
RUN mkdir -p /app/data /app/logs && \
    chown -R etluser:etluser /app

# Switch to non-root user
USER etluser

# Expose the port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Set the entry point
ENTRYPOINT ["dotnet", "ETLFramework.API.dll"]
