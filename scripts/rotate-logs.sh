#!/bin/bash
set -euo pipefail

# Log rotation script for Business Analytics
# Demonstrates Linux log rotation for operational logs

# Configuration
LOG_DIR="${LOG_DIR:-/var/log/business-analytics}"
MAX_SIZE="${MAX_SIZE:-10M}"
MAX_FILES="${MAX_FILES:-5}"
COMPRESS="${COMPRESS:-true}"
COMPRESS_CMD="${COMPRESS_CMD:-gzip}"
COMPRESS_EXT="${COMPRESS_EXT:-.gz}"

log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $*"
}

rotate_log() {
    local log_file="$1"
    local base_name
    base_name=$(basename "${log_file}")
    local dir_name
    dir_name=$(dirname "${log_file}")
    
    log "Rotating ${log_file}"
    
    # Rotate existing files
    for i in $(seq $((MAX_FILES - 1)) -1 1); do
        local old_file="${dir_name}/${base_name}.${i}${COMPRESS_EXT}"
        local new_file="${dir_name}/${base_name}.$((i + 1))${COMPRESS_EXT}"
        
        if [[ -f "${old_file}" ]]; then
            mv "${old_file}" "${new_file}"
            log "  Moved ${old_file} -> ${new_file}"
        fi
    done
    
    # Rotate current log
    if [[ -f "${log_file}" ]]; then
        local rotated_file="${dir_name}/${base_name}.1"
        mv "${log_file}" "${rotated_file}"
        log "  Moved ${log_file} -> ${rotated_file}"
        
        # Compress if enabled
        if [[ "${COMPRESS}" == "true" ]]; then
            ${COMPRESS_CMD} "${rotated_file}"
            log "  Compressed ${rotated_file} -> ${rotated_file}${COMPRESS_EXT}"
        fi
    fi
    
    # Create new empty log file
    touch "${log_file}"
    chmod 644 "${log_file}"
    log "  Created new ${log_file}"
}

main() {
    log "Starting log rotation for ${LOG_DIR}"
    
    if [[ ! -d "${LOG_DIR}" ]]; then
        log "Log directory does not exist: ${LOG_DIR}"
        exit 0
    fi
    
    # Find log files to rotate
    find "${LOG_DIR}" -name "*.log" -type f | while read -r log_file; do
        # Check file size
        local size
        size=$(stat -c%s "${log_file}" 2>/dev/null || stat -f%z "${log_file}" 2>/dev/null || echo 0)
        
        # Convert MAX_SIZE to bytes
        local max_bytes
        case "${MAX_SIZE}" in
            *K|*k) max_bytes=$((${MAX_SIZE%[Kk]} * 1024)) ;;
            *M|*m) max_bytes=$((${MAX_SIZE%[Mm]} * 1024 * 1024)) ;;
            *G|*g) max_bytes=$((${MAX_SIZE%[Gg]} * 1024 * 1024 * 1024)) ;;
            *) max_bytes=${MAX_SIZE} ;;
        esac
        
        if [[ ${size} -ge ${max_bytes} ]]; then
            log "Log file ${log_file} (${size} bytes) exceeds max size (${max_bytes} bytes)"
            rotate_log "${log_file}"
        else
            log "Log file ${log_file} (${size} bytes) within limits"
        fi
    done
    
    log "Log rotation completed"
}

main "$@"