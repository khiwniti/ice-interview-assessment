# Acceptance Tests Documentation

## Test Categories

### 1. Domain Rules Tests
Validate business invariants in isolation (no database, no HTTP).

### 2. Application Logic Tests
Validate use cases, validation, authorization with mocked repositories.

### 3. Integration Tests
Full stack tests with real PostgreSQL (Testcontainers).

### 4. API Contract Tests
HTTP-level tests against running API.

### 5. Reporting Tests
Validate SQL query results match business rules.

### 6. Export Tests
Validate JSON/CSV parity and formatting.

### 7. Concurrency Tests
Validate optimistic locking behavior.

### 8. Security Tests
Validate role/department scoping.

### 9. Container Tests
Validate Docker Compose startup, health checks, persistence.

### 10. Operations Tests
Validate backup, restore, monitoring, log rotation scripts.

## Test Fixtures

### Department Progress Scenarios

| Scenario | Setup | Expected Progress |
|----------|-------|-------------------|
| Empty department | Department, no projects | null (N/A) |
| Project no tasks | Department → Project (Active) | null (N/A) |
| 2 tasks, 1 done | Project with 2 tasks (1 Done, 1 Todo) | 50% |
| 1 done, 1 cancelled | Project with 2 tasks (1 Done, 1 Cancelled) | 100% |
| Unequal projects | Proj A: 1/1 done, Proj B: 0/9 done | 10% (task-weighted) |
| Deleted task | Project with 1 Done, 1 Deleted | 100% (deleted excluded) |
| Cancelled project | Department with 1 Cancelled project | Excluded from report |
| Overdue task | Task with past due_at, not Done | Overdue count = 1 |
| No due date | Task without due_at | Not overdue |

### API Test Data
Seeded via `DevelopmentSeeder`:
- 4 Departments: Engineering, Marketing, Sales, Operations
- 10 Users across roles
- 5 Projects with varied statuses
- 18 Tasks with varied statuses/priorities

## Running Tests

### Unit & Integration Tests
```bash
# All tests
dotnet test

# Specific project
dotnet test tests/Domain.Tests
dotnet test tests/Integration.Tests

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Testcontainers Requirements
- Docker daemon running
- Integration tests spin up real PostgreSQL containers

### Architecture Tests
```bash
dotnet test tests/Architecture.Tests
```
Validates:
- Domain has no Infrastructure references
- Application has no API references
- Controllers don't contain business logic

## Acceptance Criteria Mapping

### Database Schema (25%)

| Criterion | Verification |
|-----------|--------------|
| Required tables exist | `SELECT * FROM information_schema.tables` |
| Foreign keys enforced | Insert invalid FK → error |
| Unique constraints | Duplicate email → error |
| Indexes created | `\d+ table_name` shows indexes |
| Soft delete columns | `deleted_at` / `is_active` present |
| Version column | `version` on all business entities |
| Enum constraints | Invalid status → error |
| Migration applied | `__EFMigrationsHistory` has entry |

### Container Quality (25%)

| Criterion | Verification |
|-----------|--------------|
| Clean startup | `docker compose up --build -d` succeeds |
| Health checks pass | `./scripts/health-check.sh` exits 0 |
| Data persistence | Restart containers → data intact |
| Volumes survive | `docker compose down` → `up` → data intact |
| Non-root API | `docker exec api whoami` → `appuser` |
| Log limits | `docker inspect api` shows log opts |
| Migration runs once | Migration container exits 0 |
| API starts after migration | Depends on `service_completed_successfully` |

### Backend Logic (25%)

| Criterion | Verification |
|-----------|--------------|
| CRUD works | All endpoints return 200/201/204 |
| Validation rejects invalid | 400 for missing required fields |
| Business rules enforced | 409 for invalid transitions |
| Concurrency works | 412 for stale If-Match |
| Department report correct | Matches all fixture scenarios |
| JSON/CSV parity | Same values, different formats |
| Auth required | 401 without token |
| Role scoping works | 403 cross-department access |
| Audit logged | `system_logs` entries created |

### Linux/Unix Skills (25%)

| Criterion | Verification |
|-----------|--------------|
| Backup creates artifact | `backup-*.dump` + manifest + checksum |
| Restore succeeds | `./scripts/restore.sh backup-*.dump` |
| Health check detects failure | Stop DB → `health-check.sh` exits 1 |
| Resource monitor runs | `./scripts/monitor-resources.sh` produces output |
| Log rotation works | Create 15M log → `rotate-logs.sh` rotates |
| Smoke test passes | `./scripts/smoke-test.sh` exits 0 |

## CI Pipeline Validation

Every PR must pass:

```yaml
# .github/workflows/ci.yml (conceptual)
jobs:
  build:
    - dotnet restore
    - dotnet build --no-restore
    - dotnet format --verify-no-changes
    - dotnet test tests/Domain.Tests
    - dotnet test tests/Application.Tests
    - dotnet test tests/Architecture.Tests
  
  integration:
    - Start PostgreSQL (Testcontainers)
    - Apply migrations
    - dotnet test tests/Integration.Tests
  
  docker:
    - docker build -t business-analytics-api .
    - docker compose -f docker-compose.ci.yml up -d
    - ./scripts/health-check.sh
    - ./scripts/smoke-test.sh
    - docker compose down -v
  
  scripts:
    - shellcheck scripts/*.sh
    - bash -n scripts/*.sh
  
  security:
    - dotnet list package --vulnerable
    - Trivy scan Docker image
    - Check for committed secrets (gitleaks)
```

## Manual Verification Checklist

### Pre-Demo Setup
- [ ] Fresh clone of repository
- [ ] `cp .env.example .env`
- [ ] `docker compose up --build -d`
- [ ] Wait for all services healthy
- [ ] Access Swagger at `http://localhost:8080/swagger`

### Database Demo
- [ ] Show tables: `docker exec db psql -U app_user -d business_analytics -c "\dt"`
- [ ] Show constraints: `\d+ projects`
- [ ] Show indexes: `\di`
- [ ] Run report query manually, show EXPLAIN ANALYZE

### Container Demo
- [ ] `docker compose ps` shows all running
- [ ] `docker compose logs api` shows startup
- [ ] `docker compose restart api` → data persists
- [ ] `docker compose down` → `up` → data persists
- [ ] `docker compose down -v` → data gone (warn!)

### API Demo
- [ ] Login as admin → get token
- [ ] Create department → verify in DB
- [ ] Create user → verify in DB
- [ ] Create project → verify owner/dept constraints
- [ ] Create tasks → verify assignment rules
- [ ] Update task to Done → verify completed_at
- [ ] Complete project → verify all tasks done required
- [ ] Try invalid transition → 409
- [ ] Try stale update → 412
- [ ] Cross-department access → 403

### Reporting Demo
- [ ] GET `/reports/departments/progress` → JSON
- [ ] GET `/reports/departments/progress/export?format=json` → download
- [ ] GET `/reports/departments/progress/export?format=csv` → download
- [ ] Open CSV in editor → verify headers, escaping
- [ ] Compare JSON/CSV values → identical

### Operations Demo
- [ ] `./scripts/backup.sh` → backup created
- [ ] `./scripts/restore.sh --list` → shows backup
- [ ] `./scripts/restore.sh --verify backup-*.dump` → passes
- [ ] `./scripts/restore.sh backup-*.dump` → restores to new DB
- [ ] `./scripts/health-check.sh` → all green
- [ ] `./scripts/monitor-resources.sh` → output shown
- [ ] `./scripts/rotate-logs.sh` → rotates large log
- [ ] `./scripts/smoke-test.sh` → all green

## Known Limitations (MVP)

1. **No historical trend reporting** - Only current state
2. **No email notifications** - Audit only
3. **No file attachments** - Text only
4. **No real-time updates** - Polling required
5. **Single-region deployment** - No HA setup
6. **Basic rate limiting** - Not implemented
7. **No API versioning beyond v1** - Path-based only

## Sign-Off Criteria

All of the following must be demonstrated:

- [ ] Fresh clone → `docker compose up --build -d` → working API
- [ ] Database schema matches design docs
- [ ] All CRUD operations work with validation
- [ ] Department report matches all test fixtures
- [ ] JSON and CSV exports contain equivalent data
- [ ] Backup creates restorable artifact
- [ ] Restore verified against test data
- [ ] Health checks detect unhealthy state
- [ ] Resource monitoring produces timestamped output
- [ ] Log rotation demonstrated with size trigger
- [ ] Smoke test passes end-to-end
- [ ] Documentation sufficient for handover