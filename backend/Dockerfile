FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY nuget.config .

COPY Joby.sln .
COPY src/Api/Joby.Api.csproj src/Api/
COPY src/Application/Joby.Application.csproj src/Application/
COPY src/Domain/Joby.Domain.csproj src/Domain/
COPY src/Infrastructure/Joby.Infrastructure.csproj src/Infrastructure/
COPY tests/Joby.Tests/Joby.Tests.csproj tests/Joby.Tests/

RUN dotnet restore

COPY src/ src/
COPY tests/ tests/

WORKDIR /src/src/Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN adduser --disabled-password --gecos '' appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 5000

USER appuser

HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/health/live || exit 1

ENTRYPOINT ["dotnet", "Joby.Api.dll"]
