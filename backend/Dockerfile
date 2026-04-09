# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy nuget.config first to use only official sources
COPY nuget.config .

# Copy solution and project files
COPY Joby.sln .
COPY src/Api/Joby.Api.csproj src/Api/
COPY src/Application/Joby.Application.csproj src/Application/
COPY src/Domain/Joby.Domain.csproj src/Domain/
COPY src/Infrastructure/Joby.Infrastructure.csproj src/Infrastructure/
COPY tests/Joby.Tests/Joby.Tests.csproj tests/Joby.Tests/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY src/ src/
COPY tests/ tests/

# Build and publish
WORKDIR /src/src/Api
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user
RUN adduser --disabled-password --gecos '' appuser

# Copy published app
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port
EXPOSE 5000

# Switch to non-root user
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/health/live || exit 1

# Entry point
ENTRYPOINT ["dotnet", "Joby.Api.dll"]
