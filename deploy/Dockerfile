# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY BusinessAnalytics.sln .
COPY src/BusinessAnalytics.Domain/BusinessAnalytics.Domain.csproj src/BusinessAnalytics.Domain/
COPY src/BusinessAnalytics.Application/BusinessAnalytics.Application.csproj src/BusinessAnalytics.Application/
COPY src/BusinessAnalytics.Infrastructure/BusinessAnalytics.Infrastructure.csproj src/BusinessAnalytics.Infrastructure/
COPY src/BusinessAnalytics.Api/BusinessAnalytics.Api.csproj src/BusinessAnalytics.Api/

# Restore dependencies
RUN dotnet restore BusinessAnalytics.sln

# Copy source code
COPY src/ src/

# Build and publish
RUN dotnet publish src/BusinessAnalytics.Api/BusinessAnalytics.Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user
RUN groupadd -r appgroup && useradd -r -g appgroup appuser

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=build /app/publish .

# Set ownership
RUN chown -R appuser:appgroup /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health/live || exit 1

# Entry point
ENTRYPOINT ["dotnet", "BusinessAnalytics.Api.dll"]