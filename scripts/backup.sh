#!/bin/bash
set -euo pipefail

# Database backup script for Business Analytics
# Uses pg_dump custom format for compressed, portable backups

# Configuration
BACKUP_DIR="${BACKUP_DIR:-/backups}"
DB_HOST="${DB_HOST:-db}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${POSTGRES_DB:-business_analytics}"
DB_USER="${POSTGRES_USER:-app_user}"
DB_PASSWORD="${POSTGRES_PASSWORD}"
RETENTION_DAYS="${RETENTION_DAYS:-7}"
LOCK_FILE="/tmp/backup.lock"

# Logging
LOG_FILE="${BACKUP_DIR}/backup.log"
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

# Check for required environment
if [[ -z "${DB_PASSWORD}" ]]; then
    error "POSTGRES_PASSWORD environment variable is required"
    exit 1
fi

# Prevent overlapping runs
if [[ -f "${LOCK_FILE}" ]]; then
    PID=$(cat "${LOCK_FILE}" 2>/dev/null || echo "")
    if [[ -n "${PID}" ]] && kill -0 "${PID}" 2>/dev/null; then
        error "Backup already running (PID: ${PID})"
        exit 1
    else
        log "Stale lock file found, removing"
        rm -f "${LOCK_FILE}"
    fi
fi

echo $$ > "${LOCK_FILE}"
trap cleanup EXIT

# Create backup directory if it doesn't exist
mkdir -p "${BACKUP_DIR}"

# Generate timestamp
TIMESTAMP=$(date '+%Y%m%d-%H%M%S')
BACKUP_FILE="${BACKUP_DIR}/backup-${TIMESTAMP}.dump"
TEMP_FILE="${BACKUP_FILE}.tmp"
MANIFEST_FILE="${BACKUP_DIR}/backup-${TIMESTAMP}.manifest"
CHECKSUM_FILE="${BACKUP_DIR}/backup-${TIMESTAMP}.sha256"

log "Starting backup of ${DB_NAME} on ${DB_HOST}:${DB_PORT}"

# Run pg_dump
export PGPASSWORD="${DB_PASSWORD}"
if pg_dump \
    --format=custom \
    --file="${TEMP_FILE}" \
    --host="${DB_HOST}" \
    --port="${DB_PORT}" \
    --username="${DB_USER}" \
    --dbname="${DB_NAME}" \
    --verbose 2>&1 | tee -a "${LOG_FILE}"; then
    
    # Move temp file to final location
    mv "${TEMP_FILE}" "${BACKUP_FILE}"
    
    # Generate checksum
    sha256sum "${BACKUP_FILE}" > "${CHECKSUM_FILE}"
    
    # Create manifest
    cat > "${MANIFEST_FILE}" <<EOF
backup_file: $(basename "${BACKUP_FILE}")
database: ${DB_NAME}
host: ${DB_HOST}
port: ${DB_PORT}
user: ${DB_USER}
timestamp: ${TIMESTAMP}
size_bytes: $(stat -c%s "${BACKUP_FILE}")
checksum: $(cat "${CHECKSUM_FILE}" | awk '{print $1}')
pg_dump_version: $(pg_dump --version)
EOF
    
    # Set restrictive permissions
    chmod 600 "${BACKUP_FILE}" "${CHECKSUM_FILE}" "${MANIFEST_FILE}"
    
    log "Backup completed successfully: ${BACKUP_FILE}"
    log "Size: $(stat -c%s "${BACKUP_FILE}") bytes"
    log "Checksum: $(cat "${CHECKSUM_FILE}" | awk '{print $1}')"
    
    # Apply retention
    log "Applying retention policy (${RETENTION_DAYS} days)"
    find "${BACKUP_DIR}" -name "backup-*.dump" -type f -mtime +${RETENTION_DAYS} -delete
    find "${BACKUP_DIR}" -name "backup-*.manifest" -type f -mtime +${RETENTION_DAYS} -delete
    find "${BACKUP_DIR}" -name "backup-*.sha256" -type f -mtime +${RETENTION_DAYS} -delete
    
    REMAINING=$(find "${BACKUP_DIR}" -name "backup-*.dump" -type f | wc -l)
    log "Retention applied. ${REMAINING} backup(s) remaining"
    
    exit 0
else
    error "pg_dump failed"
    rm -f "${TEMP_FILE}"
    exit 1
fi