# Development setup script for Windows PowerShell

Write-Host "=== Joby Development Setup ===" -ForegroundColor Cyan

# Check prerequisites
Write-Host "`nChecking prerequisites..." -ForegroundColor Yellow

# Check Docker
if (!(Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Host "Docker is not installed. Please install Docker Desktop." -ForegroundColor Red
    exit 1
}
Write-Host "Docker: OK" -ForegroundColor Green

# Check .NET SDK
if (!(Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host ".NET SDK is not installed. Please install .NET 8 SDK." -ForegroundColor Red
    exit 1
}
Write-Host ".NET SDK: OK" -ForegroundColor Green

# Check Node.js
if (!(Get-Command node -ErrorAction SilentlyContinue)) {
    Write-Host "Node.js is not installed. Please install Node.js 20+." -ForegroundColor Red
    exit 1
}
Write-Host "Node.js: OK" -ForegroundColor Green

# Start PostgreSQL
Write-Host "`nStarting PostgreSQL..." -ForegroundColor Yellow
docker-compose up -d postgres

# Wait for PostgreSQL to be ready
Write-Host "Waiting for PostgreSQL to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Install backend dependencies and run migrations
Write-Host "`nSetting up backend..." -ForegroundColor Yellow
Set-Location backend
dotnet restore
dotnet build
Set-Location ..

# Install frontend dependencies
Write-Host "`nSetting up frontend..." -ForegroundColor Yellow
Set-Location frontend
npm install
Set-Location ..

Write-Host "`n=== Setup Complete ===" -ForegroundColor Green
Write-Host "`nTo start development:"
Write-Host "  Backend:  cd backend && dotnet run --project src/Api"
Write-Host "  Frontend: cd frontend && npm run dev"
Write-Host "`nOr use Docker Compose:"
Write-Host "  docker-compose up --build"
Write-Host ""




