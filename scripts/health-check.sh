#!/bin/bash
set -euo pipefail

# Health check script for Business Analytics services

# Configuration
API_HOST="${API_HOST:-localhost}"
API_PORT="${API_PORT:-8080}"
DB_HOST="${DB_HOST:-db}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${POSTGRES_DB:-business_analytics}"
DB_USER="${POSTGRES_USER:-app_user}"
DB_PASSWORD="${POSTGRES_PASSWORD}"

# Timeout settings
CURL_TIMEOUT="${CURL_TIMEOUT:-10}"
DB_TIMEOUT="${DB_TIMEOUT:-5}"

log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $*"
}

error() {
    log "ERROR: $*" >&2
}

check_api_live() {
    log "Checking API liveness..."
    if curl -sf --max-time "${CURL_TIMEOUT}" "http://${API_HOST}:${API_PORT}/health/live" >/dev/null; then
        log "✓ API liveness check passed"
        return 0
    else
        error "✗ API liveness check failed"
        return 1
    fi
}

check_api_ready() {
    log "Checking API readiness..."
    if curl -sf --max-time "${CURL_TIMEOUT}" "http://${API_HOST}:${API_PORT}/health/ready" >/dev/null; then
        log "✓ API readiness check passed"
        return 0
    else
        error "✗ API readiness check failed"
        return 1
    fi
}

check_database() {
    log "Checking database connectivity..."
    export PGPASSWORD="${DB_PASSWORD}"
    if pg_isready -h "${DB_HOST}" -p "${DB_PORT}" -U "${DB_USER}" -d "${DB_NAME}" -t "${DB_TIMEOUT}" >/dev/null 2>&1; then
        log "✓ Database connectivity check passed"
        return 0
    else
        error "✗ Database connectivity check failed"
        return 1
    fi
}

check_database_query() {
    log "Checking database query execution..."
    export PGPASSWORD="${DB_PASSWORD}"
    if psql -h "${DB_HOST}" -p "${DB_PORT}" -U "${DB_USER}" -d "${DB_NAME}" -c "SELECT 1;" >/dev/null 2>&1; then
        log "✓ Database query check passed"
        return 0
    else
        error "✗ Database query check failed"
        return 1
    fi
}

check_containers() {
    log "Checking container status..."
    local failed=0
    
    for container in business-analytics-db business-analytics-api; do
        if docker ps --filter "name=${container}" --filter "status=running" --format "{{.Names}}" | grep -q "${container}"; then
            log "✓ Container ${container} is running"
        else
            error "✗ Container ${container} is not running"
            failed=1
        fi
    done
    
    return ${failed}
}

main() {
    local exit_code=0
    
    log "Starting health checks..."
    
    check_database || exit_code=1
    check_database_query || exit_code=1
    check_containers || exit_code=1
    check_api_live || exit_code=1
    check_api_ready || exit_code=1
    
    if [[ ${exit_code} -eq 0 ]]; then
        log "All health checks passed"
    else
        log "Some health checks failed"
    fi
    
    exit ${exit_code}
}

main "$@"