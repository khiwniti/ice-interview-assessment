#!/bin/bash
set -euo pipefail

# Resource monitoring script for Business Analytics
# Captures CPU, memory, disk usage for containers and host

# Configuration
REPORT_DIR="${REPORT_DIR:-/var/log/business-analytics/monitoring}"
CONTAINERS="${CONTAINERS:-business-analytics-db business-analytics-api}"
DISK_PATH="${DISK_PATH:-/}"
MEMORY_THRESHOLD_MB="${MEMORY_THRESHOLD_MB:-80}"  # Percentage
CPU_THRESHOLD="${CPU_THRESHOLD:-80}"               # Percentage
DISK_THRESHOLD="${DISK_THRESHOLD:-85}"             # Percentage

mkdir -p "${REPORT_DIR}"

TIMESTAMP=$(date '+%Y-%m-%d %H:%M:%S')
REPORT_FILE="${REPORT_DIR}/resources-$(date '+%Y%m%d').log"

log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $*" | tee -a "${REPORT_FILE}"
}

warn() {
    log "WARNING: $*" >&2
}

# Check if bc is available for floating point math
if ! command -v bc >/dev/null 2>&1; then
    log "Installing bc for calculations..."
    apk add --no-cache bc 2>/dev/null || apt-get update && apt-get install -y bc 2>/dev/null || true
fi

get_host_memory() {
    local mem_info
    mem_info=$(cat /proc/meminfo)
    local total_kb=$(echo "${mem_info}" | grep 'MemTotal:' | awk '{print $2}')
    local available_kb=$(echo "${mem_info}" | grep 'MemAvailable:' | awk '{print $2}')
    local used_kb=$((total_kb - available_kb))
    local used_pct=$(echo "scale=2; ${used_kb} * 100 / ${total_kb}" | bc)
    echo "${used_pct}"
}

get_host_disk() {
    local disk_info
    disk_info=$(df -k "${DISK_PATH}" | tail -1)
    local total_kb=$(echo "${disk_info}" | awk '{print $2}')
    local used_kb=$(echo "${disk_info}" | awk '{print $3}')
    local used_pct=$(echo "scale=2; ${used_kb} * 100 / ${total_kb}" | bc)
    echo "${used_pct}"
}

get_container_stats() {
    local container="$1"
    local stats
    
    stats=$(docker stats --no-stream --format "{{.CPUPerc}},{{.MemUsage}},{{.MemPerc}}" "${container}" 2>/dev/null || echo "0.00%,0B/0B,0.00%")
    
    local cpu_pct=$(echo "${stats}" | cut -d',' -f1 | sed 's/%//')
    local mem_usage=$(echo "${stats}" | cut -d',' -f2)
    local mem_pct=$(echo "${stats}" | cut -d',' -f3 | sed 's/%//')
    
    echo "${cpu_pct},${mem_usage},${mem_pct}"
}

get_backup_size() {
    local backup_dir="/backups"
    if [[ -d "${backup_dir}" ]]; then
        du -sh "${backup_dir}" 2>/dev/null | cut -f1 || echo "0B"
    else
        echo "0B"
    fi
}

main() {
    log "=== Resource Monitoring Report ==="
    log "Timestamp: ${TIMESTAMP}"
    
    # Host memory
    local host_mem_pct
    host_mem_pct=$(get_host_memory)
    log "Host Memory Usage: ${host_mem_pct}%"
    (( $(echo "${host_mem_pct} > ${MEMORY_THRESHOLD_MB}" | bc -l) )) && warn "Host memory usage (${host_mem_pct}%) exceeds threshold (${MEMORY_THRESHOLD_MB}%)"
    
    # Host disk
    local host_disk_pct
    host_disk_pct=$(get_host_disk)
    log "Host Disk Usage (${DISK_PATH}): ${host_disk_pct}%"
    (( $(echo "${host_disk_pct} > ${DISK_THRESHOLD}" | bc -l) )) && warn "Host disk usage (${host_disk_pct}%) exceeds threshold (${DISK_THRESHOLD}%)"
    
    # Container stats
    for container in ${CONTAINERS}; do
        local stats
        stats=$(get_container_stats "${container}")
        local cpu_pct=$(echo "${stats}" | cut -d',' -f1)
        local mem_usage=$(echo "${stats}" | cut -d',' -f2)
        local mem_pct=$(echo "${stats}" | cut -d',' -f3)
        
        log "Container ${container}:"
        log "  CPU: ${cpu_pct}%"
        log "  Memory: ${mem_usage} (${mem_pct}%)"
        
        (( $(echo "${cpu_pct} > ${CPU_THRESHOLD}" | bc -l) )) && warn "Container ${container} CPU usage (${cpu_pct}%) exceeds threshold (${CPU_THRESHOLD}%)"
    done
    
    # Backup directory size
    local backup_size
    backup_size=$(get_backup_size)
    log "Backup Directory Size: ${backup_size}"
    
    # Container health
    log "Container Health Status:"
    for container in ${CONTAINERS}; do
        local health
        health=$(docker inspect --format='{{.State.Health.Status}}' "${container}" 2>/dev/null || echo "no-health-check")
        log "  ${container}: ${health}"
    done
    
    log "=== Report Complete ==="
    echo "" >> "${REPORT_FILE}"
}

main "$@"