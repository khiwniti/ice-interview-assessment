#!/bin/bash
set -euo pipefail

# =============================================================================
# Business Analytics - Complete End-to-End Test Script
# =============================================================================
# This script performs a full E2E test of the Business Analytics system:
# 1. Prerequisites check (Docker, Docker Compose, .NET SDK)
# 2. Clean environment setup
# 3. Build and start all services
# 4. Run automated smoke tests
# 5. Run operational script verification
# 6. Run unit/integration tests (if .NET SDK available)
# 7. Cleanup
# =============================================================================

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
COMPOSE_FILE="${PROJECT_DIR}/docker-compose.yml"
ENV_FILE="${PROJECT_DIR}/.env"
ENV_EXAMPLE="${PROJECT_DIR}/.env.example"
SCRIPTS_DIR="${PROJECT_DIR}/scripts"
TEST_TIMEOUT=120
STARTUP_TIMEOUT=180

# Test results tracking
TESTS_PASSED=0
TESTS_FAILED=0
FAILED_TESTS=()

log() {
    echo -e "${BLUE}[$(date '+%Y-%m-%d %H:%M:%S')]${NC} $*"
}

log_success() {
    echo -e "${GREEN}[$(date '+%Y-%m-%d %H:%M:%S')] ✓${NC} $*"
    ((TESTS_PASSED++))
}

log_warn() {
    echo -e "${YELLOW}[$(date '+%Y-%m-%d %H:%M:%S')] ⚠${NC} $*"
}

log_error() {
    echo -e "${RED}[$(date '+%Y-%m-%d %H:%M:%S')] ✗${NC} $*" >&2
    ((TESTS_FAILED++))
    FAILED_TESTS+=("$1")
}

print_header() {
    echo ""
    echo -e "${BLUE}===============================================================================${NC}"
    echo -e "${BLUE}  $1${NC}"
    echo -e "${BLUE}===============================================================================${NC}"
}

check_command() {
    if command -v "$1" &>/dev/null; then
        log_success "$1 found: $(command -v "$1")"
        return 0
    else
        log_error "Missing required command: $1"
        return 1
    fi
}

wait_for_health() {
    local url="$1"
    local name="$2"
    local max_attempts="${3:-60}"
    local attempt=1

    log "Waiting for $name to be healthy..."
    while [[ $attempt -le $max_attempts ]]; do
        if curl -sf "$url" >/dev/null 2>&1; then
            log_success "$name is healthy"
            return 0
        fi
        sleep 2
        ((attempt++))
    done
    log_error "$name failed health check after $max_attempts attempts"
    return 1
}

# =============================================================================
# MAIN EXECUTION
# =============================================================================

print_header "Business Analytics - Complete E2E Test Suite"
log "Project directory: $PROJECT_DIR"

# -----------------------------------------------------------------------------
# Phase 1: Prerequisites Check
# -----------------------------------------------------------------------------
print_header "Phase 1: Prerequisites Check"

check_command "docker" || exit 1
check_command "docker-compose" || check_command "docker compose" || exit 1

if check_command "dotnet"; then
    DOTNET_AVAILABLE=true
    log "Dotnet version: $(dotnet --version)"
else
    DOTNET_AVAILABLE=false
    log_warn "dotnet not found - skipping unit/integration tests"
fi

# Check Docker daemon
log "Checking Docker daemon..."
if docker info >/dev/null 2>&1; then
    log_success "Docker daemon is running"
else
    log_error "Docker daemon is not accessible"
    exit 1
fi

# -----------------------------------------------------------------------------
# Phase 2: Environment Setup
# -----------------------------------------------------------------------------
print_header "Phase 2: Environment Setup"

cd "$PROJECT_DIR"

# Create .env from example if not exists
if [[ ! -f "$ENV_FILE" ]]; then
    log "Creating .env from .env.example"
    cp "$ENV_EXAMPLE" "$ENV_FILE"
    log_success ".env created"
else
    log_success ".env already exists"
fi

# Make scripts executable
log "Making scripts executable..."
chmod +x "$SCRIPTS_DIR"/*.sh
log_success "Scripts are executable"

# -----------------------------------------------------------------------------
# Phase 3: Clean Start (remove any existing containers/volumes)
# -----------------------------------------------------------------------------
print_header "Phase 3: Clean Environment"

log "Stopping and removing existing containers..."
docker compose -f "$COMPOSE_FILE" down -v --remove-orphans 2>/dev/null || true
log_success "Clean shutdown complete"

# -----------------------------------------------------------------------------
# Phase 4: Build and Start Services
# -----------------------------------------------------------------------------
print_header "Phase 4: Build and Start Services"

log "Building Docker images (this may take a few minutes)..."
if docker compose -f "$COMPOSE_FILE" build --no-cache --pull; then
    log_success "Docker images built successfully"
else
    log_error "Docker build failed"
    exit 1
fi

log "Starting services..."
if docker compose -f "$COMPOSE_FILE" up -d; then
    log_success "Services started"
else
    log_error "Failed to start services"
    exit 1
fi

# -----------------------------------------------------------------------------
# Phase 5: Wait for Health Checks
# -----------------------------------------------------------------------------
print_header "Phase 5: Health Checks"

wait_for_health "http://localhost:8080/health/live" "API Liveness" 60
wait_for_health "http://localhost:8080/health/ready" "API Readiness" 90

# Verify all containers are healthy
log "Verifying container status..."
docker compose -f "$COMPOSE_FILE" ps --format "table {{.Name}}\t{{.Status}}\t{{.Health}}"

# Check migration completed
MIGRATE_STATUS=$(docker compose -f "$COMPOSE_FILE" ps -q migrate 2>/dev/null | xargs docker inspect --format '{{.State.ExitCode}}' 2>/dev/null || echo "1")
if [[ "$MIGRATE_STATUS" == "0" ]]; then
    log_success "Migration container exited successfully (code 0)"
else
    log_error "Migration container failed (exit code: $MIGRATE_STATUS)"
    docker compose -f "$COMPOSE_FILE" logs migrate
    exit 1
fi

# -----------------------------------------------------------------------------
# Phase 6: Automated Smoke Tests
# -----------------------------------------------------------------------------
print_header "Phase 6: Automated Smoke Tests"

log "Running smoke-test.sh..."
if "$SCRIPTS_DIR/smoke-test.sh"; then
    log_success "Smoke tests passed"
else
    log_error "Smoke tests failed"
    # Show logs for debugging
    docker compose -f "$COMPOSE_FILE" logs api --tail 50
    exit 1
fi

# -----------------------------------------------------------------------------
# Phase 7: Operational Scripts Verification
# -----------------------------------------------------------------------------
print_header "Phase 7: Operational Scripts Verification"

# Health check script
log "Running health-check.sh..."
if "$SCRIPTS_DIR/health-check.sh"; then
    log_success "Health check passed"
else
    log_error "Health check failed"
fi

# Resource monitoring
log "Running monitor-resources.sh..."
if "$SCRIPTS_DIR/monitor-resources.sh"; then
    log_success "Resource monitoring works"
else
    log_error "Resource monitoring failed"
fi

# Backup
log "Running backup.sh..."
if "$SCRIPTS_DIR/backup.sh"; then
    log_success "Backup created"
else
    log_error "Backup failed"
fi

# List backups
log "Running restore.sh --list..."
if "$SCRIPTS_DIR/restore.sh" --list; then
    log_success "Backup listing works"
else
    log_error "Backup listing failed"
fi

# Verify backup
LATEST_BACKUP=$(find "$PROJECT_DIR/backups" -name "backup-*.dump" -type f 2>/dev/null | sort -r | head -1 || true)
if [[ -n "$LATEST_BACKUP" ]]; then
    log "Running restore.sh --verify on $LATEST_BACKUP..."
    if "$SCRIPTS_DIR/restore.sh" --verify "$(basename "$LATEST_BACKUP")"; then
        log_success "Backup verification passed"
    else
        log_error "Backup verification failed"
    fi

    # Restore to new database
    log "Running restore.sh to new database..."
    if "$SCRIPTS_DIR/restore.sh" --force "$(basename "$LATEST_BACKUP")"; then
        log_success "Restore to new database succeeded"
    else
        log_error "Restore failed"
    fi
else
    log_warn "No backup found to verify/restore"
fi

# Log rotation
log "Running rotate-logs.sh..."
if "$SCRIPTS_DIR/rotate-logs.sh"; then
    log_success "Log rotation works"
else
    log_error "Log rotation failed"
fi

# -----------------------------------------------------------------------------
# Phase 8: Data Persistence Test
# -----------------------------------------------------------------------------
print_header "Phase 8: Data Persistence Test"

log "Restarting containers (simulating host reboot)..."
docker compose -f "$COMPOSE_FILE" restart

wait_for_health "http://localhost:8080/health/ready" "API after restart" 60

# Verify data still exists
TOKEN=$(curl -sf -X POST "http://localhost:8080/api/v1/auth/login" \
    -H "Content-Type: application/json" \
    -d '{"email":"admin@company.com","password":"Admin123!"}' | jq -r '.token // empty')

if [[ -n "$TOKEN" && "$TOKEN" != "null" ]]; then
    DEPT_COUNT=$(curl -sf -H "Authorization: Bearer $TOKEN" "http://localhost:8080/api/v1/departments" | jq 'length')
    if [[ "$DEPT_COUNT" -ge 4 ]]; then
        log_success "Data persisted after restart (departments: $DEPT_COUNT)"
    else
        log_error "Data loss after restart (departments: $DEPT_COUNT)"
    fi
else
    log_error "Failed to authenticate after restart"
fi

# -----------------------------------------------------------------------------
# Phase 9: Unit & Integration Tests (if .NET SDK available)
# -----------------------------------------------------------------------------
if [[ "$DOTNET_AVAILABLE" == true ]]; then
    print_header "Phase 9: Unit & Integration Tests"

    log "Running dotnet restore..."
    if dotnet restore "$PROJECT_DIR/BusinessAnalytics.sln"; then
        log_success "Dependencies restored"
    else
        log_error "Dotnet restore failed"
    fi

    log "Running dotnet build..."
    if dotnet build "$PROJECT_DIR/BusinessAnalytics.sln" -c Release --no-restore; then
        log_success "Build successful"
    else
        log_error "Build failed"
    fi

    log "Running Domain unit tests..."
    if dotnet test "$PROJECT_DIR/tests/Domain.Tests/Domain.Tests.csproj" -c Release --no-build --logger "console;verbosity=quiet"; then
        log_success "Domain tests passed"
    else
        log_error "Domain tests failed"
    fi

    log "Running Architecture tests..."
    if dotnet test "$PROJECT_DIR/tests/Architecture.Tests/Architecture.Tests.csproj" -c Release --no-build --logger "console;verbosity=quiet"; then
        log_success "Architecture tests passed"
    else
        log_error "Architecture tests failed"
    fi

    # Integration tests require Testcontainers (Docker) - run if Docker is available
    log "Running Integration tests (Testcontainers)..."
    if dotnet test "$PROJECT_DIR/tests/Integration.Tests/Integration.Tests.csproj" -c Release --no-build --logger "console;verbosity=quiet"; then
        log_success "Integration tests passed"
    else
        log_error "Integration tests failed"
    fi
else
    print_header "Phase 9: Unit & Integration Tests (SKIPPED)"
    log_warn ".NET SDK not available - skipping dotnet tests"
fi

# -----------------------------------------------------------------------------
# Phase 10: API Contract Verification
# -----------------------------------------------------------------------------
print_header "Phase 10: API Contract Verification"

# Swagger/OpenAPI
log "Checking Swagger documentation..."
if curl -sf "http://localhost:8080/swagger/v1/swagger.json" | jq -e '.paths' >/dev/null; then
    log_success "OpenAPI spec accessible"
else
    log_error "OpenAPI spec not accessible"
fi

# Test key endpoints
ENDPOINTS=(
    "GET|/health/live|200"
    "GET|/health/ready|200"
    "GET|/api/v1/departments|200"
    "GET|/api/v1/users|200"
    "GET|/api/v1/projects|200"
    "GET|/api/v1/reports/departments/progress|200"
)

for endpoint in "${ENDPOINTS[@]}"; do
    IFS='|' read -r method path expected <<< "$endpoint"
    if [[ "$method" == "GET" ]]; then
        if curl -sf -H "Authorization: Bearer $TOKEN" "http://localhost:8080$path" >/dev/null; then
            log_success "$method $path -> $expected"
        else
            log_error "$method $path -> expected $expected"
        fi
    fi
done

# Test exports
log "Testing JSON export..."
if curl -sf -H "Authorization: Bearer $TOKEN" "http://localhost:8080/api/v1/reports/departments/progress/export?format=json" -o /tmp/export.json; then
    if [[ -s /tmp/export.json ]] && jq -e '.[] | .departmentId' /tmp/export.json >/dev/null; then
        log_success "JSON export valid"
    else
        log_error "JSON export invalid"
    fi
else
    log_error "JSON export failed"
fi

log "Testing CSV export..."
if curl -sf -H "Authorization: Bearer $TOKEN" "http://localhost:8080/api/v1/reports/departments/progress/export?format=csv" -o /tmp/export.csv; then
    if [[ -s /tmp/export.csv ]] && head -1 /tmp/export.csv | grep -q "DepartmentId"; then
        log_success "CSV export valid"
    else
        log_error "CSV export invalid"
    fi
else
    log_error "CSV export failed"
fi

# -----------------------------------------------------------------------------
# Phase 11: Cleanup
# -----------------------------------------------------------------------------
print_header "Phase 11: Cleanup"

log "Stopping all services..."
docker compose -f "$COMPOSE_FILE" down
log_success "Services stopped"

# -----------------------------------------------------------------------------
# SUMMARY
# -----------------------------------------------------------------------------
print_header "E2E Test Summary"

echo -e "${GREEN}Tests Passed: $TESTS_PASSED${NC}"
echo -e "${RED}Tests Failed: $TESTS_FAILED${NC}"

if [[ ${#FAILED_TESTS[@]} -gt 0 ]]; then
    echo -e "${RED}Failed Tests:${NC}"
    for test in "${FAILED_TESTS[@]}"; do
        echo -e "  ${RED}✗${NC} $test"
    done
    echo ""
    echo -e "${RED}===============================================================================${NC}"
    echo -e "${RED}  E2E TESTS FAILED${NC}"
    echo -e "${RED}===============================================================================${NC}"
    exit 1
else
    echo ""
    echo -e "${GREEN}===============================================================================${NC}"
    echo -e "${GREEN}  ALL E2E TESTS PASSED ✓${NC}"
    echo -e "${GREEN}===============================================================================${NC}"
    echo ""
    echo "System is ready for deployment!"
    exit 0
fi