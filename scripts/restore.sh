#!/bin/bash
set -euo pipefail

# Database restore script for Business Analytics
# Uses pg_restore for custom format backups

# Configuration
BACKUP_DIR="${BACKUP_DIR:-/backups}"
DB_HOST="${DB_HOST:-db}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${POSTGRES_DB:-business_analytics}"
DB_USER="${POSTGRES_USER:-app_user}"
DB_PASSWORD="${POSTGRES_PASSWORD}"
TARGET_DB="${TARGET_DB:-${DB_NAME}_restore_$(date '+%Y%m%d-%H%M%S')}"
LOCK_FILE="/tmp/restore.lock"

# Logging
LOG_FILE="${BACKUP_DIR}/restore.log"
exec > >(tee -a "${LOG_FILE}") 2>&1

log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $*"
}

error() {
    log "ERROR: $*" >&2
}

cleanup() {
    rm -f "${LOCK_FILE}"
    log "Cleanup completed"
}

usage() {
    cat <<EOF
Usage: $0 [OPTIONS] BACKUP_FILE

Restore a PostgreSQL database from a custom-format backup.

Options:
    -h, --help              Show this help message
    --target-db NAME        Target database name (default: ${DB_NAME}_restore_TIMESTAMP)
    --force                 Skip confirmation prompt
    --list                  List available backups and exit
    --verify                Verify backup integrity without restoring

Environment variables:
    BACKUP_DIR              Backup directory (default: /backups)
    DB_HOST                 Database host (default: db)
    DB_PORT                 Database port (default: 5432)
    DB_NAME                 Source database name (default: business_analytics)
    DB_USER                 Database user (default: app_user)
    DB_PASSWORD             Database password (required)
    POSTGRES_PASSWORD       Alias for DB_PASSWORD

Examples:
    $0 backup-20240115-120000.dump
    $0 --target-db my_restore_db backup-20240115-120000.dump
    $0 --list
    $0 --verify backup-20240115-120000.dump
EOF
}

list_backups() {
    log "Available backups in ${BACKUP_DIR}:"
    find "${BACKUP_DIR}" -name "backup-*.dump" -type f -printf "%T@ %p\n" 2>/dev/null | sort -rn | while read -r time file; do
        date_str=$(date -d "@${time%.*}" '+%Y-%m-%d %H:%M:%S' 2>/dev/null || date -r "${time%.*}" '+%Y-%m-%d %H:%M:%S')
        size=$(stat -c%s "${file}")
        echo "  ${file} (${date_str}, ${size} bytes)"
    done
    exit 0
}

verify_backup() {
    local backup_file="$1"
    log "Verifying backup: ${backup_file}"
    
    if [[ ! -f "${backup_file}" ]]; then
        error "Backup file not found: ${backup_file}"
        exit 1
    fi
    
    # Check checksum if manifest exists
    local manifest="${backup_file%.dump}.manifest"
    local checksum_file="${backup_file%.dump}.sha256"
    
    if [[ -f "${checksum_file}" ]]; then
        log "Verifying checksum..."
        if sha256sum -c "${checksum_file}" 2>&1 | tee -a "${LOG_FILE}"; then
            log "Checksum verification passed"
        else
            error "Checksum verification failed"
            exit 1
        fi
    fi
    
    # Test pg_restore --list
    export PGPASSWORD="${DB_PASSWORD}"
    if pg_restore --list "${backup_file}" >/dev/null 2>&1; then
        log "Backup format is valid"
    else
        error "Backup format is invalid or corrupted"
        exit 1
    fi
    
    log "Verification completed successfully"
    exit 0
}

# Parse arguments
FORCE=false
BACKUP_FILE=""
LIST=false
VERIFY=false

while [[ $# -gt 0 ]]; do
    case $1 in
        -h|--help)
            usage
            ;;
        --target-db)
            TARGET_DB="$2"
            shift 2
            ;;
        --force)
            FORCE=true
            shift
            ;;
        --list)
            LIST=true
            shift
            ;;
        --verify)
            VERIFY=true
            shift
            ;;
        -*)
            error "Unknown option: $1"
            usage
            ;;
        *)
            BACKUP_FILE="$1"
            shift
            ;;
    esac
done

if [[ "${LIST}" == "true" ]]; then
    list_backups
fi

if [[ -z "${BACKUP_FILE}" ]]; then
    error "Backup file not specified"
    usage
fi

# Resolve backup file path
if [[ ! "${BACKUP_FILE}" = /* ]]; then
    BACKUP_FILE="${BACKUP_DIR}/${BACKUP_FILE}"
fi

if [[ "${VERIFY}" == "true" ]]; then
    verify_backup "${BACKUP_FILE}"
fi

# Check for required environment
if [[ -z "${DB_PASSWORD}" ]]; then
    error "POSTGRES_PASSWORD environment variable is required"
    exit 1
fi

# Prevent overlapping runs
if [[ -f "${LOCK_FILE}" ]]; then
    PID=$(cat "${LOCK_FILE}" 2>/dev/null || echo "")
    if [[ -n "${PID}" ]] && kill -0 "${PID}" 2>/dev/null; then
        error "Restore already running (PID: ${PID})"
        exit 1
    else
        log "Stale lock file found, removing"
        rm -f "${LOCK_FILE}"
    fi
fi

echo $$ > "${LOCK_FILE}"
trap cleanup EXIT

# Verify backup file exists
if [[ ! -f "${BACKUP_FILE}" ]]; then
    error "Backup file not found: ${BACKUP_FILE}"
    exit 1
fi

# Verify backup format
export PGPASSWORD="${DB_PASSWORD}"
if ! pg_restore --list "${BACKUP_FILE}" >/dev/null 2>&1; then
    error "Invalid backup file format"
    exit 1
fi

# Confirmation
if [[ "${FORCE}" != "true" ]]; then
    echo "This will restore the backup to database: ${TARGET_DB}"
    echo "Source backup: ${BACKUP_FILE}"
    echo "Target host: ${DB_HOST}:${DB_PORT}"
    read -rp "Are you sure you want to continue? [y/N] " -n 1
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        log "Restore cancelled by user"
        exit 0
    fi
fi

log "Starting restore of ${BACKUP_FILE} to ${TARGET_DB} on ${DB_HOST}:${DB_PORT}"

# Create target database if it doesn't exist
log "Creating target database: ${TARGET_DB}"
psql -h "${DB_HOST}" -p "${DB_PORT}" -U "${DB_USER}" -d postgres -c "CREATE DATABASE \"${TARGET_DB}\";" 2>&1 | tee -a "${LOG_FILE}"

# Restore backup
log "Restoring backup..."
if pg_restore \
    --host="${DB_HOST}" \
    --port="${DB_PORT}" \
    --username="${DB_USER}" \
    --dbname="${TARGET_DB}" \
    --verbose \
    --exit-on-error \
    --jobs=4 \
    "${BACKUP_FILE}" 2>&1 | tee -a "${LOG_FILE}"; then
    
    log "Restore completed successfully"
    
    # Verify schema
    log "Verifying schema..."
    TABLE_COUNT=$(psql -h "${DB_HOST}" -p "${DB_PORT}" -U "${DB_USER}" -d "${TARGET_DB}" -t -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public';" 2>/dev/null | xargs)
    log "Restored ${TABLE_COUNT} tables"
    
    log "Restore successful. Target database: ${TARGET_DB}"
    exit 0
else
    error "pg_restore failed"
    exit 1
fi