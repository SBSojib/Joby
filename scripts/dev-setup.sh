#!/bin/bash

echo "=== Joby Development Setup ==="

# Check prerequisites
echo -e "\nChecking prerequisites..."

# Check Docker
if ! command -v docker &> /dev/null; then
    echo "Docker is not installed. Please install Docker."
    exit 1
fi
echo "Docker: OK"

# Check .NET SDK
if ! command -v dotnet &> /dev/null; then
    echo ".NET SDK is not installed. Please install .NET 8 SDK."
    exit 1
fi
echo ".NET SDK: OK"

# Check Node.js
if ! command -v node &> /dev/null; then
    echo "Node.js is not installed. Please install Node.js 20+."
    exit 1
fi
echo "Node.js: OK"

# Start PostgreSQL
echo -e "\nStarting PostgreSQL..."
docker-compose up -d postgres

# Wait for PostgreSQL to be ready
echo "Waiting for PostgreSQL to be ready..."
sleep 10

# Install backend dependencies and run migrations
echo -e "\nSetting up backend..."
cd backend
dotnet restore
dotnet build
cd ..

# Install frontend dependencies
echo -e "\nSetting up frontend..."
cd frontend
npm install
cd ..

echo -e "\n=== Setup Complete ==="
echo -e "\nTo start development:"
echo "  Backend:  cd backend && dotnet run --project src/Api"
echo "  Frontend: cd frontend && npm run dev"
echo -e "\nOr use Docker Compose:"
echo "  docker-compose up --build"
echo ""




