# Architecture Documentation

## Overview

Business Analytics follows **Clean Architecture** principles with a **modular monolith** deployment model. The system is organized into four layers with strict dependency rules:

```
┌─────────────────────────────────────┐
│           API Layer                 │  Controllers, Middleware, Auth, DI
├─────────────────────────────────────┤
│        Application Layer            │  Use Cases, DTOs, Validation, Interfaces
├─────────────────────────────────────┤
│         Domain Layer                │  Entities, Enums, Business Rules
├─────────────────────────────────────┤
│      Infrastructure Layer           │  EF Core, Repositories, SQL, Identity
└─────────────────────────────────────┘
```

## Dependency Rules

- **Domain** has no dependencies on external frameworks
- **Application** depends only on Domain
- **Infrastructure** implements Application interfaces
- **API** references Application and Infrastructure for composition

## Layer Responsibilities

### Domain Layer (`BusinessAnalytics.Domain`)
- **Entities**: `Department`, `User`, `Project`, `TaskItem`, `SystemLog`
- **Enums**: `ProjectStatus`, `TaskStatus`, `TaskPriority`, `UserRole`, `AuditAction`, `AuditOutcome`
- **Business Rules**: Status transitions, completion logic, validation invariants
- **Abstractions**: Repository interfaces, UnitOfWork

### Application Layer (`BusinessAnalytics.Application`)
- **Services**: `DepartmentService`, `UserService`, `ProjectService`, `TaskService`, `ReportService`
- **DTOs**: Request/Response records for each entity
- **Validators**: FluentValidation rules for all inputs
- **Interfaces**: Repository contracts, `IAuditService`, `IReportService`

### Infrastructure Layer (`BusinessAnalytics.Infrastructure`)
- **Persistence**: EF Core `DbContext`, entity configurations, migrations
- **Repositories**: Concrete implementations using EF Core
- **Reporting**: Parameterized SQL queries via `Npgsql`
- **Identity**: ASP.NET Core Identity integration
- **Auditing**: `AuditService` writing to `system_logs`

### API Layer (`BusinessAnalytics.Api`)
- **Controllers**: REST endpoints with proper HTTP semantics
- **Middleware**: Exception handling, request logging
- **Authentication**: JWT Bearer tokens with role-based policies
- **Documentation**: OpenAPI/Swagger with XML comments

## Data Flow

### Write Path (Command)
```
HTTP Request → Controller → Application Service → Domain Entity
                                                    ↓
                                            Infrastructure Repository
                                                    ↓
                                            EF Core → PostgreSQL
                                                    ↓
                                            AuditService → system_logs
```

### Read Path (Query)
```
HTTP Request → Controller → Application Service → ReportService
                                                    ↓
                                            Raw SQL → PostgreSQL
                                                    ↓
                                            DTO Mapping → HTTP Response
```

## Concurrency Control

Optimistic locking via `Version` property on all entities:
- Clients send `If-Match: <version>` header on updates
- Server rejects stale updates with `412 Precondition Failed`
- Version increments on every successful modification

## Security

- **Authentication**: JWT Bearer tokens (HS256)
- **Authorization**: Role + resource scope policies
- **Passwords**: BCrypt via ASP.NET Core Identity
- **Secrets**: Environment variables only, never in source

## Container Architecture

```
┌─────────────────┐     ┌─────────────────┐
│   API Container │────▶│  PostgreSQL     │
│   (non-root)    │     │  (persistent)   │
└─────────────────┘     └─────────────────┘
        ▲                       ▲
        │                       │
        ▼                       ▼
┌─────────────────┐     ┌─────────────────┐
│  Migrate (once) │     │   Backup (cron) │
└─────────────────┘     └─────────────────┘
```

- **Multi-stage Dockerfile**: SDK build → Runtime ASP.NET Core
- **Health checks**: Liveness (process) + Readiness (DB + app)
- **Volumes**: `postgres_data`, `backup_data`
- **Network**: Internal bridge network

## Reporting Architecture

Department progress uses a **two-stage aggregation** to avoid join multiplication:

```sql
WITH project_metrics AS (
    -- Pre-aggregate per project
    SELECT p.id, p.department_id,
           COUNT(t.id) FILTER (WHERE t.status <> 'Cancelled') AS task_count,
           COUNT(t.id) FILTER (WHERE t.status = 'Done') AS completed_task_count
    FROM projects p
    LEFT JOIN tasks t ON t.project_id = p.id AND t.deleted_at IS NULL
    WHERE p.deleted_at IS NULL AND p.status <> 'Cancelled'
    GROUP BY p.id, p.department_id
)
SELECT d.id, d.name, COUNT(pm.id), SUM(pm.task_count), ...
FROM departments d
LEFT JOIN project_metrics pm ON pm.department_id = d.id
WHERE d.is_active = TRUE
GROUP BY d.id, d.name;
```

Both JSON and CSV exports use the **same query service** ensuring data parity.

## Extensibility Points

1. **New Entity**: Add to Domain → Application DTOs/Interfaces → Infrastructure Config/Repo → API Controller
2. **New Report**: Add SQL to `ReportService` → DTO → Controller endpoint
3. **New Auth Provider**: Replace JWT middleware with OIDC handler
4. **Background Jobs**: Add hosted services in Infrastructure