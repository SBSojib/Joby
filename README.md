# Joby - Job Discovery + Application Tracker

A full-stack MVP web application for tracking job applications, discovering job opportunities, and managing your job search.

## Features

- 🔐 **User Authentication** - JWT-based auth with refresh tokens
- 👤 **Profile Management** - Build your profile with skills, keywords, and preferences
- 📄 **Resume Parsing** - Upload PDF/DOCX resumes with automatic text extraction
- 💼 **Job Management** - Add jobs by URL (with auto-extraction) or manual entry
- 🎯 **Smart Recommendations** - Skill/keyword matching with explainability
- 📊 **Application Tracking** - Kanban-style pipeline with status tracking
- 📅 **Timeline & Events** - Track every interaction and status change
- 🔔 **Reminders** - Automatic follow-up reminders and custom alerts
- 🌙 **Modern Dark UI** - Beautiful, responsive interface

## Tech Stack

### Backend
- .NET 8 Web API (C#)
- Clean Architecture
- Entity Framework Core
- PostgreSQL
- Hangfire (background jobs)
- JWT Authentication
- FluentValidation
- Swagger/OpenAPI

### Frontend
- React 18 + TypeScript
- Vite
- TanStack Query
- React Router
- React Hook Form + Zod
- Tailwind CSS
- Radix UI components

### Infrastructure
- Docker
- Kubernetes
- Nginx

## Project Structure

```
├── backend/
│   ├── src/
│   │   ├── Api/              # Controllers, middleware, entry point
│   │   ├── Application/      # DTOs, interfaces, validators
│   │   ├── Domain/           # Entities, enums
│   │   └── Infrastructure/   # EF Core, services, auth
│   └── tests/
├── frontend/
│   └── src/
│       ├── components/       # Reusable UI components
│       ├── contexts/         # React contexts
│       ├── lib/              # API client, utilities
│       ├── pages/            # Page components
│       └── types/            # TypeScript types
├── k8s/                      # Kubernetes manifests
├── scripts/                  # Dev helper scripts
└── docker-compose.yml
```

## Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)

### Option 1: Docker Compose (Recommended)

```bash
# Clone the repository
git clone <repo-url>
cd joby

# Start app + Postgres (profile `local` includes the database; Postgres is on host port 5300)
docker compose --profile local up --build -d

# Access the app
# Frontend: http://localhost:8080
# Backend API (host): http://localhost:5000
# Swagger: http://localhost:5000/swagger
```

### Option 2: Local Development

```bash
# Run the setup script
# Windows PowerShell:
.\scripts\dev-setup.ps1

# Linux/Mac:
chmod +x scripts/dev-setup.sh
./scripts/dev-setup.sh

# Start PostgreSQL (Compose profile `local`; published on host port 5300)
docker compose --profile local up -d postgres

# Start backend (terminal 1)
cd backend
dotnet run --project src/Api

# Start frontend (terminal 2)
cd frontend
npm run dev

# Access the app
# Frontend: http://localhost:5173
# Backend API: http://localhost:5000
```

## Environment Variables

### Backend (appsettings.json or environment)

```env
ConnectionStrings__DefaultConnection=Host=localhost;Port=5300;Database=joby;Username=postgres;Password=CHANGE_ME
Jwt__Secret=YOUR_SUPER_SECRET_KEY_MIN_32_CHARS
Jwt__Issuer=Joby
Jwt__Audience=JobyApp
Jwt__AccessTokenExpirationMinutes=60
Jwt__RefreshTokenExpirationDays=7
Cors__AllowedOrigins__0=http://localhost:5173
```

### Frontend (.env)

```env
VITE_API_URL=/api
```

## API Documentation

Once the backend is running, visit:
- Swagger UI: http://localhost:5000/swagger
- Health Check: http://localhost:5000/health/ready

### Key Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/auth/register` | POST | Register new user |
| `/api/auth/login` | POST | Login |
| `/api/auth/refresh` | POST | Refresh token |
| `/api/auth/me` | GET | Get current user |
| `/api/profile` | GET/PUT | Get/update profile |
| `/api/profile/resumes` | POST | Upload resume |
| `/api/jobs` | GET/POST | List/create jobs |
| `/api/jobs/url` | POST | Create job from URL |
| `/api/jobs/recommended` | GET | Get recommended jobs |
| `/api/applications` | GET/POST | List/create applications |
| `/api/applications/pipeline` | GET | Get Kanban pipeline |
| `/api/reminders` | GET/POST | List/create reminders |

## Building Docker Images

```bash
# Build images
chmod +x scripts/build-images.sh
./scripts/build-images.sh v1.0.0

# Or with a registry
REGISTRY=your-registry.com ./scripts/build-images.sh v1.0.0
```

## Kubernetes Deployment

### Prerequisites
- Kubernetes cluster (local: minikube, kind, or Docker Desktop)
- kubectl configured
- Ingress controller (nginx)

### Deploy

```bash
# Create namespace and apply all manifests
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/

# Check deployment status
kubectl get pods -n joby
kubectl get services -n joby

# View logs
kubectl logs -f deployment/backend -n joby
kubectl logs -f deployment/frontend -n joby
```

### Configuration

1. Update `k8s/secret.yaml` with production secrets
2. Update `k8s/configmap.yaml` with your domain
3. Update `k8s/ingress.yaml` with your hostname and TLS settings

For production with managed PostgreSQL:
```bash
# Update the connection string in secrets
kubectl create secret generic joby-secrets \
  --from-literal=ConnectionStrings__DefaultConnection="Host=your-db.example.com;..." \
  --from-literal=Jwt__Secret="your-production-secret" \
  -n joby
```

## Development

### Running Tests

```bash
cd backend
dotnet test
```

### Database Migrations

Migrations are applied automatically on startup. To create a new migration:

```bash
cd backend
dotnet ef migrations add MigrationName --project src/Infrastructure --startup-project src/Api
```

### Adding New Skills to Parser

Edit `backend/src/Infrastructure/Services/ResumeParser.cs` and add skills to the `KnownSkills` HashSet.

## Architecture Decisions

### Authentication
- JWT tokens stored in memory/localStorage (trade-off: XSS risk vs simplicity)
- Refresh tokens stored in HTTP-only cookies
- Automatic token refresh on 401 responses

### Recommendation Engine
- Simple skill/keyword overlap scoring (0-100)
- Computed on profile/job changes and via scheduled job
- Explainable: shows matched and missing skills

### Resume Parsing
- PDF: UglyToad.PdfPig (open source)
- DOCX: OpenXML SDK
- Heuristic parsing for names, emails, skills
- User can edit/override all extracted data

## Security Considerations

1. **Secrets Management**: Use Kubernetes secrets or external secrets management (Vault, AWS Secrets Manager)
2. **CORS**: Configure allowed origins for production
3. **Rate Limiting**: Consider adding rate limiting middleware
4. **Input Validation**: All inputs validated with FluentValidation
5. **SQL Injection**: Prevented by EF Core parameterized queries

## License

MIT

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request




