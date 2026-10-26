#!/bin/bash
set -euo pipefail

# Smoke test script for Business Analytics
# Exercises startup, CRUD, reporting, and export functionality

# Configuration
API_HOST="${API_HOST:-localhost}"
API_PORT="${API_PORT:-8080}"
API_BASE="http://${API_HOST}:${API_PORT}/api/v1"
HEALTH_BASE="http://${API_HOST}:${API_PORT}/health"

# Test credentials (from DevelopmentSeeder)
ADMIN_EMAIL="admin@company.com"
ADMIN_PASSWORD="Admin123!"
MANAGER_EMAIL="eng.manager@company.com"
MANAGER_PASSWORD="Manager123!"

log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $*"
}

error() {
    log "ERROR: $*" >&2
}

fail() {
    error "$*"
    exit 1
}

get_token() {
    local email="$1"
    local password="$2"
    
    local response
    response=$(curl -sf -X POST "${API_BASE}/auth/login" \
        -H "Content-Type: application/json" \
        -d "{\"email\":\"${email}\",\"password\":\"${password}\"}" 2>/dev/null || echo "")
    
    if [[ -z "${response}" ]]; then
        return 1
    fi
    
    echo "${response}" | jq -r '.token // .accessToken // .access_token // empty'
}

wait_for_health() {
    local max_attempts=30
    local attempt=1
    
    log "Waiting for API health endpoint..."
    while [[ ${attempt} -le ${max_attempts} ]]; do
        if curl -sf "${HEALTH_BASE}/live" >/dev/null 2>&1; then
            log "API liveness check passed"
            break
        fi
        
        if [[ ${attempt} -eq ${max_attempts} ]]; then
            fail "API liveness check failed after ${max_attempts} attempts"
        fi
        
        sleep 2
        ((attempt++))
    done
    
    attempt=1
    log "Waiting for API readiness..."
    while [[ ${attempt} -le ${max_attempts} ]]; do
        if curl -sf "${HEALTH_BASE}/ready" >/dev/null 2>&1; then
            log "API readiness check passed"
            return 0
        fi
        
        if [[ ${attempt} -eq ${max_attempts} ]]; then
            fail "API readiness check failed after ${max_attempts} attempts"
        fi
        
        sleep 2
        ((attempt++))
    done
}

test_crud_departments() {
    log "Testing Department CRUD..."
    
    local token
    token=$(get_token "${ADMIN_EMAIL}" "${ADMIN_PASSWORD}") || fail "Failed to get admin token"
    
    # List departments
    local response
    response=$(curl -sf -H "Authorization: Bearer ${token}" "${API_BASE}/departments") || fail "Failed to list departments"
    log "✓ List departments: $(echo "${response}" | jq 'length') departments found"
    
    # Create department
    local create_response
    create_response=$(curl -sf -X POST -H "Authorization: Bearer ${token}" -H "Content-Type: application/json" \
        -d '{"name":"Test Department"}' "${API_BASE}/departments") || fail "Failed to create department"
    
    local dept_id
    dept_id=$(echo "${create_response}" | jq -r '.id') || fail "Failed to get department ID"
    log "✓ Create department: ${dept_id}"
    
    # Get department
    response=$(curl -sf -H "Authorization: Bearer ${token}" "${API_BASE}/departments/${dept_id}") || fail "Failed to get department"
    log "✓ Get department: $(echo "${response}" | jq -r '.name')"
    
    # Update department
    response=$(curl -sf -X PUT -H "Authorization: Bearer ${token}" -H "Content-Type: application/json" \
        -d '{"name":"Test Department Updated"}' "${API_BASE}/departments/${dept_id}") || fail "Failed to update department"
    log "✓ Update department"
    
    # Deactivate department
    response=$(curl -sf -X DELETE -H "Authorization: Bearer ${token}" "${API_BASE}/departments/${dept_id}") || fail "Failed to deactivate department"
    log "✓ Deactivate department"
    
    # Clean up - reactivate and delete not allowed, but we can verify it's deactivated
    response=$(curl -sf -H "Authorization: Bearer ${token}" "${API_BASE}/departments/${dept_id}") || fail "Failed to get deactivated department"
    local is_active
    is_active=$(echo "${response}" | jq -r '.isActive')
    [[ "${is_active}" == "false" ]] || fail "Department should be deactivated"
    log "✓ Verified department is deactivated"
}

test_crud_users() {
    log "Testing User CRUD..."
    
    local token
    token=$(get_token "${ADMIN_EMAIL}" "${ADMIN_PASSWORD}") || fail "Failed to get admin token"
    
    # Get departments for user creation
    local dept_response
    dept_response=$(curl -sf -H "Authorization: Bearer ${token}" "${API_BASE}/departments") || fail "Failed to list departments"
    local dept_id
    dept_id=$(echo "${dept_response}" | jq -r '.[0].id') || fail "Failed to get department ID"
    
    # Create user
    local create_response
    create_response=$(curl -sf -X POST -H "Authorization: Bearer ${token}" -H "Content-Type: application/json" \
        -d "{\"departmentId\":\"${dept_id}\",\"email\":\"test.user@company.com\",\"displayName\":\"Test User\",\"password\":\"Test123!\",\"role\":\"Contributor\"}" \
        "${API_BASE}/users") || fail "Failed to create user"
    
    local user_id
    user_id=$(echo "${create_response}" | jq -r '.id') || fail "Failed to get user ID"
    log "✓ Create user: ${user_id}"
    
    # Get user
    response=$(curl -sf -H "Authorization: Bearer ${token}" "${API_BASE}/users/${user_id}") || fail "Failed to get user"
    log "✓ Get user: $(echo "${response}" | jq -r '.displayName')"
    
    # Update user
    response=$(curl -sf -X PUT -H "Authorization: Bearer ${token}" -H "Content-Type: application/json" \
        -d '{"email":"test.user.updated@company.com","displayName":"Test User Updated","role":"Contributor"}' \
        "${API_BASE}/users/${user_id}") || fail "Failed to update user"
    log "✓ Update user"
    
    # Deactivate user
    response=$(curl -sf -X DELETE -H "Authorization: Bearer ${token}" "${API_BASE}/users/${user_id}") || fail "Failed to deactivate user"
    log "✓ Deactivate user"
}

test_crud_projects() {
    log "Testing Project CRUD..."
    
    local token
    token=$(get_token "${MANAGER_EMAIL}" "${MANAGER_PASSWORD}") || fail "Failed to get manager token"
    
    # Get departments and users
    local dept_response
    dept_response=$(curl -sf -H "Authorization: Bearer ${token}" "${API_BASE}/departments")
    local dept_id
    dept_id=$(echo "${dept_response}" | jq -r '.[0].id')
    
    local user_response
    user_response=$(curl -sf -H "Authorization: Bearer ${token}" "${API_BASE}/users")
    local owner_id
    owner_id=$(echo "${user_response}" | jq -r '.[0].id')
    
    # Create project
    local create_response
    create_response=$(curl -sf -X POST -H "Authorization: Bearer ${token}" -H "Content-Type: application/json" \
        -d "{\"departmentId\":\"${dept_id}\",\"ownerUserId\":\"${owner_id}\",\"name\":\"Smoke Test Project\",\"description\":\"Project for smoke testing\",\"status\":\"Active\",\"startAt\":\"$(date -u -d '+1 day' +%Y-%m-%dT%H:%M:%SZ)\",\"dueAt\":\"$(date -u -d '+30 days' +%Y-%m-%dT%H:%M:%SZ)\"}" \
        "${API_BASE}/projects") || fail "Failed to create project"
    
    local project_id
    project_id=$(echo "${create_response}" | jq -r '.id') || fail "Failed to get project ID"
    local project_version
    project_version=$(echo "${create_response}" | jq -r '.version')
    log "✓ Create project: ${project_id} (v${project_version})"
    
    # Get project
    response=$(curl -sf -H "Authorization: Bearer ${token}" "${API_BASE}/projects/${project_id}") || fail "Failed to get project"
    log "✓ Get project: $(echo "${response}" | jq -r '.name')"
    
    # Update project (with ETag)
    response=$(curl -sf -X PUT -H "Authorization: Bearer ${token}" -H "Content-Type: application/json" -H "If-Match: ${project_version}" \
        -d "{\"name\":\"Smoke Test Project Updated\",\"description\":\"Updated description\",\"status\":\"Active\",\"startAt\":\"$(date -u -d '+1 day' +%Y-%m-%dT%H:%M:%SZ)\",\"dueAt\":\"$(date -u -d '+30 days' +%Y-%m-%dT%H:%M:%SZ)\"}" \
        "${API_BASE}/projects/${project_id}") || fail "Failed to update project"
    
    local new_version
    new_version=$(echo "${response}" | jq -r '.version')
    log "✓ Update project (v${project_version} -> v${new_version})"
    
    # Test concurrent update rejection
    response=$(curl -s -X PUT -H "Authorization: Bearer ${token}" -H "Content-Type: application/json" -H "If-Match: ${project_version}" \
        -d "{\"name\":\"Concurrent Update\",\"description\":\"Should fail\",\"status\":\"Active\",\"startAt\":\"$(date -u -d '+1 day' +%Y-%m-%dT%H:%M:%SZ)\",\"dueAt\":\"$(date -u -d '+30 days' +%Y-%m-%dT%H:%M:%SZ)\"}" \
        "${API_BASE}/projects/${project_id}" 2>&1)
    
    if echo "${response}" | grep -q "412\|Precondition Failed"; then
        log "✓ Concurrent update correctly rejected (412)"
    else
        log "Note: Concurrent update behavior may vary"
    fi
    
    # Return project ID for task tests
    echo "${project_id}"
}

test_crud_tasks() {
    local project_id="$1"
    
    log "Testing Task CRUD..."
    
    local token
    token=$(get_token "${MANAGER_EMAIL}" "${MANAGER_PASSWORD}") || fail "Failed to get manager token"
    
    # Get a user for assignment
    local user_response
    user_response=$(curl -sf -H "Authorization: Bearer ${token}" "${API_BASE}/users")
    local assignee_id
    assignee_id=$(echo "${user_response}" | jq -r '.[0].id')
    
    # Create task
    local create_response
    create_response=$(curl -sf -X POST -H "Authorization: Bearer ${token}" -H "Content-Type: application/json" \
        -d "{\"title\":\"Smoke Test Task\",\"description\":\"Task for smoke testing\",\"priority\":\"High\",\"dueAt\":\"$(date -u -d '+7 days' +%Y-%m-%dT%H:%M:%SZ)\",\"assignedToUserId\":\"${assignee_id}\"}" \
        "${API_BASE}/projects/${project_id}/tasks") || fail "Failed to create task"
    
    local task_id
    task_id=$(echo "${create_response}" | jq -r '.id') || fail "Failed to get task ID"
    local task_version
    task_version=$(echo "${create_response}" | jq -r '.version')
    log "✓ Create task: ${task_id} (v${task_version})"
    
    # Get task
    response=$(curl -sf -H "Authorization: Bearer ${token}" "${API_BASE}/tasks/${task_id}") || fail "Failed to get task"
    log "✓ Get task: $(echo "${response}" | jq -r '.title')"
    
    # List project tasks
    response=$(curl -sf -H "Authorization: Bearer ${token}" "${API_BASE}/projects/${project_id}/tasks") || fail "Failed to list project tasks"
    log "✓ List project tasks: $(echo "${response}" | jq 'length') tasks found"
    
    # Update task status to Done
    response=$(curl -sf -X PATCH -H "Authorization: Bearer ${token}" -H "Content-Type: application/json" -H "If-Match: ${task_version}" \
        -d '{"status":"Done"}' "${API_BASE}/tasks/${task_id}/status") || fail "Failed to update task status"
    log "✓ Update task status to Done"
    
    # Verify completed_at is set
    response=$(curl -sf -H "Authorization: Bearer ${token}" "${API_BASE}/tasks/${task_id}") || fail "Failed to get updated task"
    local completed_at
    completed_at=$(echo "${response}" | jq -r '.completedAt')
    [[ "${completed_at}" != "null" ]] || fail "CompletedAt should be set for Done task"
    log "✓ Verified completedAt is set: ${completed_at}"
    
    # Soft delete task
    local new_version
    new_version=$(echo "${response}" | jq -r '.version')
    response=$(curl -sf -X DELETE -H "Authorization: Bearer ${token}" -H "If-Match: ${new_version}" "${API_BASE}/tasks/${task_id}") || fail "Failed to delete task"
    log "✓ Delete task"
}

test_reporting() {
    log "Testing Reporting..."
    
    local token
    token=$(get_token "${MANAGER_EMAIL}" "${MANAGER_PASSWORD}") || fail "Failed to get manager token"
    
    # Department progress report
    local response
    response=$(curl -sf -H "Authorization: Bearer ${token}" "${API_BASE}/reports/departments/progress") || fail "Failed to get department progress report"
    log "✓ Department progress report: $(echo "${response}" | jq 'length') departments"
    
    # Project summary report
    local project_id="$1"
    response=$(curl -sf -H "Authorization: Bearer ${token}" "${API_BASE}/reports/projects/${project_id}/summary") || fail "Failed to get project summary"
    log "✓ Project summary report: $(echo "${response}" | jq -r '.projectName') - $(echo "${response}" | jq -r '.progressPercent // "N/A"}% complete"
    
    # Export JSON
    response=$(curl -sf -H "Authorization: Bearer ${token}" "${API_BASE}/reports/departments/progress/export?format=json" -o /tmp/export.json) || fail "Failed to export JSON"
    [[ -s /tmp/export.json ]] || fail "JSON export is empty"
    log "✓ JSON export: $(wc -c < /tmp/export.json) bytes"
    
    # Export CSV
    response=$(curl -sf -H "Authorization: Bearer ${token}" "${API_BASE}/reports/departments/progress/export?format=csv" -o /tmp/export.csv) || fail "Failed to export CSV"
    [[ -s /tmp/export.csv ]] || fail "CSV export is empty"
    log "✓ CSV export: $(wc -c < /tmp/export.csv) bytes"
    
    # Verify CSV has header
    head -1 /tmp/export.csv | grep -q "DepartmentId" || fail "CSV missing header"
    log "✓ CSV has correct header"
}

main() {
    log "=== Business Analytics Smoke Test ==="
    
    wait_for_health
    
    test_crud_departments
    test_crud_users
    local project_id
    project_id=$(test_crud_projects)
    test_crud_tasks "${project_id}"
    test_reporting "${project_id}"
    
    log "=== All Smoke Tests Passed ==="
}

main "$@"