# Business Analytics API

A project-monitoring REST API built with **.NET 8**, **Clean Architecture**, **PostgreSQL**, **Docker Compose**, and **Linux automation**.

## Features

- **Project & Task Management**: Full CRUD with status lifecycles, priorities, and assignments
- **Department Analytics**: Task-weighted progress reporting with overdue tracking
- **User Management**: Role-based access control (Admin, Manager, Contributor, Viewer)
- **Audit Logging**: Immutable business event trail with JSON metadata
- **Data Export**: JSON and CSV exports with formula-injection protection
- **Concurrency Control**: Optimistic locking via ETag/If-Match headers
- **Health Checks**: Liveness and readiness endpoints for container orchestration
- **Backup/Restore**: Automated pg_dump/pg_restore with retention and verification
- **Resource Monitoring**: CPU, memory, and disk usage reporting
- **Log Rotation**: Linux logrotate integration with compression
## Quick Start

### Option 1: Automated Installation (Recommended)

```bash
# Install prerequisites (Docker, Docker Compose, .NET 8, Git, jq, shellcheck)
curl -fsSL https://raw.githubusercontent.com/khiwniti/ice-interview-assessment/main/scripts/install.sh | sudo bash

# Clone and run complete E2E test
git clone https://github.com/khiwniti/ice-interview-assessment.git
cd ice-interview-assessment
./scripts/e2e-test.sh
```

### Option 2: Manual Setup

#### Prerequisites
- Docker and Docker Compose v2
- Git

#### One-Command Setup

```bash
git clone https://github.com/khiwniti/ice-interview-assessment.git
cd ice-interview-assessment
cp .env.example .env
docker compose up --build -d
```

### Run Complete E2E Test Suite

```bash
# Full automated test (health, CRUD, reports, exports, operations, persistence)
./scripts/e2e-test.sh

# Or run individual test phases
./scripts/smoke-test.sh          # API + CRUD + reports + exports
./scripts/health-check.sh        # Service health verification
./scripts/monitor-resources.sh   # CPU, memory, disk monitoring
./scripts/backup.sh              # Database backup
./scripts/restore.sh --list      # List available backups
./scripts/rotate-logs.sh         # Log rotation
```

The API will be available at `http://localhost:8080` with Swagger UI at `http://localhost:8080/swagger`.

### Default Credentials (Development Only)

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@company.com | Admin123! |
| Manager | eng.manager@company.com | Manager123! |
| Contributor | eng.dev1@company.com | Dev123! |
| Viewer | viewer@company.com | Viewer123! |

⚠️ **Never use these credentials in production.** Override all secrets via environment variables.

## Architecture

```
API Client → ASP.NET Core API → Application Layer → Domain Model
                                          ↓
                              Infrastructure Layer → PostgreSQL
```

**Clean Architecture Layers:**
- **Domain**: Entities, enums, business rules (no external dependencies)
- **Application**: Use cases, DTOs, validation, interfaces
- **Infrastructure**: EF Core, repositories, SQL reports, Identity
- **API**: Controllers, middleware, authentication, composition root

## API Endpoints

| Area | Endpoints |
|------|-----------|
| Departments | `GET/POST /api/v1/departments`, `GET/PUT/DELETE /api/v1/departments/{id}` |
| Users | `GET/POST /api/v1/users`, `GET/PUT/DELETE /api/v1/users/{id}` |
| Projects | `GET/POST /api/v1/projects`, `GET/PUT/DELETE /api/v1/projects/{id}` |
| Tasks | `GET/POST /api/v1/projects/{id}/tasks`, `GET/PUT/PATCH/DELETE /api/v1/tasks/{id}` |
| Reports | `GET /api/v1/reports/departments/progress`, `GET /api/v1/reports/projects/{id}/summary` |
| Export | `GET /api/v1/reports/departments/progress/export?format=json|csv` |
| Health | `GET /health/live`, `GET /health/ready` |

## Configuration

Copy `.env.example` to `.env` and configure:

```env
# Database
POSTGRES_DB=business_analytics
POSTGRES_USER=app_user
POSTGRES_PASSWORD=your_secure_password

# JWT (minimum 32 characters for production)
JWT_KEY=your-production-secret-key-min-32-chars
JWT_ISSUER=BusinessAnalytics
JWT_AUDIENCE=BusinessAnalytics

# CORS
CORS_ORIGIN_0=https://your-frontend.com
```

## Operations

### Backup
```bash
# Manual backup
docker exec business-analytics-backup /scripts/backup.sh

# Or run directly
./scripts/backup.sh
```

### Restore
```bash
# List available backups
./scripts/restore.sh --list

# Restore to new database
./scripts/restore.sh backup-20240115-120000.dump

# Verify backup integrity
./scripts/restore.sh --verify backup-20240115-120000.dump
```

### Health Check
```bash
./scripts/health-check.sh
```

### Resource Monitoring
```bash
./scripts/monitor-resources.sh
```

### Log Rotation
```bash
# Manual rotation
./scripts/rotate-logs.sh

# Automatic via logrotate (install in /etc/logrotate.d/)
sudo cp deploy/logrotate.conf /etc/logrotate.d/business-analytics
```

### Smoke Test
```bash
./scripts/smoke-test.sh
```

## Development

### Running Tests
```bash
dotnet test
```

### Adding Migrations
```bash
dotnet ef migrations add MigrationName --project src/BusinessAnalytics.Infrastructure --startup-project src/BusinessAnalytics.Api
```

### Project Structure
```
business-analytics/
├── src/
│   ├── BusinessAnalytics.Domain/       # Domain entities, rules
│   ├── BusinessAnalytics.Application/  # Use cases, DTOs, interfaces
│   ├── BusinessAnalytics.Infrastructure/ # EF Core, repos, SQL reports
│   └── BusinessAnalytics.Api/          # Controllers, middleware
├── tests/
│   ├── Domain.Tests/
│   ├── Application.Tests/
│   ├── Integration.Tests/
│   └── Architecture.Tests/
├── tools/DevelopmentSeeder/            # Dev data seeding
├── scripts/                            # Operational scripts
├── deploy/                             # Dockerfile, logrotate.conf
├── docs/                               # Documentation
├── docker-compose.yml
├── .env.example
└── README.md
```

## Business Rules

### Project Status Lifecycle
`Planned → Active → OnHold → Completed` or `Cancelled`
- Completed projects require all non-cancelled tasks to be Done
- Completed projects are read-only until reopened

### Task Status Lifecycle
`Todo → InProgress → Blocked → Done` or `Cancelled`
- Tasks can only be marked Done if parent project is editable
- Setting Done populates `completed_at`; reopening clears it

### Completion Formula
```
Progress % = 100 × (Done Tasks) / (Non-Cancelled Tasks)
```
- Cancelled tasks excluded from numerator and denominator
- No eligible tasks → `null` (displayed as "N/A")
- Department progress is task-weighted, not project-averaged

### Soft Deletion
- Projects/Tasks: Soft-deleted (`deleted_at` timestamp), excluded from lists/reports
- Users/Departments: Deactivated (`is_active=false`), preserved for history

## Production Deployment

1. Generate strong secrets for all environment variables
2. Use separate database users for migration (owner) and runtime (read/write)
3. Enable HTTPS via reverse proxy (nginx, Traefik)
4. Configure external backup storage (S3, Azure Blob, etc.)
5. Set up monitoring/alerting on health endpoints
6. Run restore drill before go-live

## License

MIT