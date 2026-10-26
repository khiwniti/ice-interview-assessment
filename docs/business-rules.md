# Business Rules Documentation

## Project Status Lifecycle

### Valid Transitions
```
Planned → Active
Planned → Cancelled
Active → OnHold
Active → Completed (if all non-cancelled tasks are Done)
Active → Cancelled
OnHold → Active
OnHold → Cancelled
Completed → Active (reopen)
Cancelled → (no transitions allowed)
```

### Rules
1. **Completion Requirement**: A project can only become `Completed` when:
   - It has at least one non-cancelled task
   - All non-cancelled tasks are `Done`

2. **Read-Only Completion**: `Completed` projects cannot be modified until reopened to `Active`

3. **Cancellation**: `Cancelled` projects are terminal (no further transitions)

4. **Soft Delete**: Deleting a project soft-deletes all its non-deleted tasks in the same transaction

## Task Status Lifecycle

### Valid Transitions
```
Todo → InProgress
Todo → Cancelled
InProgress → Todo
InProgress → Blocked
InProgress → Done
InProgress → Cancelled
Blocked → InProgress
Blocked → Cancelled
Done → InProgress (reopen)
Done → Cancelled
Cancelled → (no transitions allowed)
```

### Rules
1. **Done Requirement**: A task can only be marked `Done` if its parent project is editable (`CanBeEdited()` = true)
   - Project must not be deleted
   - Project status must not be `Completed` or `Cancelled`

2. **Completion Timestamp**: 
   - Setting status to `Done` populates `completed_at` with current UTC time
   - Reopening a `Done` task clears `completed_at`

3. **Cancellation**: `Cancelled` tasks are excluded from progress calculations

4. **Soft Delete**: Deleted tasks are excluded from lists and analytics

## User Management

### Roles
| Role | Description | Permissions |
|------|-------------|-------------|
| Admin | System administrator | Full access to all departments, users, projects, tasks, audit |
| Manager | Department manager | CRUD projects/tasks in own department, manage users in own department |
| Contributor | Team member | Update assigned tasks, view department projects/tasks |
| Viewer | Read-only access | View department projects/tasks only |

### Rules
1. **Department Scoping**: Users can only access data within their department (except Admins)
2. **Assignment Constraint**: Tasks can only be assigned to users in the same department as the project
3. **Deactivation**: Users are deactivated (not deleted) to preserve history
4. **Self-Modification**: Users cannot modify their own role or department

## Department Management

### Rules
1. **Unique Names**: Department names must be unique (case-insensitive)
2. **Deactivation**: Departments can only be deactivated if no active projects or users exist
3. **Soft Delete**: Departments are deactivated, not deleted

## Progress Calculation

### Formula
```
Progress Percentage = 100 × (Done Tasks) / (Non-Cancelled Tasks)
```

### Scope
- **Project Level**: Calculated per project from its non-cancelled tasks
- **Department Level**: Task-weighted aggregate across all non-cancelled projects in department

### Special Cases
| Scenario | Result |
|----------|--------|
| No eligible tasks | `null` (displayed as "N/A") |
| All eligible tasks done | 100% |
| Some done, some pending | Percentage (rounded to 2 decimals) |
| Cancelled tasks | Excluded from both numerator and denominator |
| Cancelled projects | Excluded from department report |
| Deleted projects/tasks | Excluded from all reports |

### Example
- Project A: 1 done / 1 eligible = 100%
- Project B: 0 done / 9 eligible = 0%
- **Department Progress = 10%** (not 50% average)

## Overdue Determination

A task is **overdue** when:
- Not deleted (`deleted_at` IS NULL)
- Status is not `Done` or `Cancelled`
- Has a due date (`due_at` IS NOT NULL)
- Due date is before report generation time (`due_at < now`)

## Concurrency Control

### Optimistic Locking
- Every entity has a `version` integer (starts at 1)
- Version increments on every successful update
- Updates require `If-Match: <current_version>` header
- Stale updates return `412 Precondition Failed`

### Affected Operations
- Project updates (PUT, PATCH status)
- Task updates (PUT, PATCH status)
- User updates (PUT, password change)
- Department updates (PUT)

## Audit Logging

### Events Logged
| Action | Entities |
|--------|----------|
| Created | Department, User, Project, Task |
| Updated | Department, User, Project, Task |
| Deleted | Department, User, Project, Task |
| StatusChanged | Project, Task |
| Assigned/Unassigned | Task |

### Log Fields
- `actor_user_id`: Who performed the action (nullable for system)
- `action`: Enum value
- `entity_type`: Entity class name
- `entity_id`: Affected entity ID
- `outcome`: Success/Failure
- `trace_id`: HTTP request trace ID
- `metadata`: JSON with additional context (validated allowlist)

### Rules
1. **Atomicity**: Business change and audit log committed in same transaction
2. **Immutability**: Audit logs are never modified or deleted through normal APIs
3. **Retention**: Separate policy (not implemented in MVP)

## Data Validation

### Length Limits
- Department name: 100 chars
- User email: 256 chars
- User display name: 100 chars
- Project name: 200 chars
- Project description: 2000 chars
- Task title: 200 chars
- Task description: 2000 chars
- Trace ID: 100 chars

### Required Fields
- All names/titles: Non-empty after trim
- Emails: Valid format, unique (normalized)
- Departments/Users: Must reference existing active records
- Dates: `due_at` ≥ `start_at` when both present

### Status/Priority Values
- Must be valid enum values (enforced by database CHECK constraints via application)

## Export Behavior

### JSON Export
- Content-Type: `application/json`
- Null progress → `null`
- ISO 8601 timestamps

### CSV Export
- Content-Type: `text/csv; charset=utf-8`
- Content-Disposition: `attachment; filename="..."`
- Null progress → empty cell
- Formula injection protection: prefix `=`, `+`, `-`, `@` with `'`
- Proper escaping of commas, quotes, newlines

## Error Handling

### HTTP Status Codes
| Code | Use Case |
|------|----------|
| 200 | Successful GET, PUT, PATCH |
| 201 | Successful POST (with Location header) |
| 204 | Successful DELETE |
| 400 | Invalid input, validation failure |
| 401 | Missing/invalid authentication |
| 403 | Authenticated but unauthorized |
| 404 | Resource not found |
| 409 | Business rule conflict |
| 412 | Stale version (If-Match mismatch) |
| 429 | Rate limit exceeded |
| 500 | Unexpected server error |
| 503 | Service unavailable (readiness failure) |

### Error Response Format (RFC 7807 Problem Details)
```json
{
  "type": "https://httpstatuses.com/409",
  "title": "Business Rule Violation",
  "status": 409,
  "detail": "Project must have at least one non-cancelled task to be completed",
  "instance": "/api/v1/projects/...",
  "traceId": "abc123"
}
```