# Operations Documentation

## Service Management

### Docker Compose Commands

```bash
# Start all services
docker compose up --build -d

# View logs
docker compose logs -f api
docker compose logs -f db

# Stop services (preserves volumes)
docker compose down

# Stop and remove volumes (DESTRUCTIVE)
docker compose down -v

# Restart single service
docker compose restart api

# Rebuild and restart
docker compose up --build -d api
```

### Service Health

| Service | Health Check | Interval | Timeout |
|---------|-------------|----------|---------|
| PostgreSQL | `pg_isready` | 10s | 5s |
| API Liveness | `GET /health/live` | 30s | 5s |
| API Readiness | `GET /health/ready` | 30s | 5s |

### Startup Sequence
1. PostgreSQL starts
2. Database health check passes
3. Migration container runs EF Core migrations
4. Migration completes successfully
5. API container starts
6. API readiness check passes

## Backup Operations

### Automated Backup
The backup service runs daily via cron in the backup container:
- Uses `pg_dump` custom format (compressed)
- Stores in `/backups` volume
- Retains 7 days by default
- Generates manifest and SHA256 checksum

### Manual Backup
```bash
# Via script (requires DB credentials)
./scripts/backup.sh

# Via docker exec
docker exec business-analytics-backup /scripts/backup.sh
```

### Backup Artifacts
Each backup produces:
- `backup-YYYYMMDD-HHMMSS.dump` - Custom format archive
- `backup-YYYYMMDD-HHMMSS.manifest` - Metadata (size, timestamp, checksum)
- `backup-YYYYMMDD-HHMMSS.sha256` - SHA256 checksum

## Restore Operations

### List Available Backups
```bash
./scripts/restore.sh --list
```

### Verify Backup Integrity
```bash
./scripts/restore.sh --verify backup-20240115-120000.dump
```

### Restore to New Database
```bash
# Creates business_analytics_restore_YYYYMMDD-HHMMSS
./scripts/restore.sh backup-20240115-120000.dump

# Custom target name
./scripts/restore.sh --target-db my_restore_db backup-20240115-120000.dump
```

### Restore Verification Drill (Pre-Production)
```bash
# 1. Create test records
# 2. Generate backup
./scripts/backup.sh

# 3. Restore to test database
./scripts/restore.sh --target-db restore_test backup-<latest>.dump

# 4. Verify schema and data
psql -h localhost -U app_user -d restore_test -c "SELECT COUNT(*) FROM projects;"

# 5. Run department report
psql -h localhost -U app_user -d restore_test -f scripts/department-report.sql

# 6. Clean up
psql -h localhost -U app_user -d postgres -c "DROP DATABASE restore_test;"
```

## Resource Monitoring

### Manual Check
```bash
./scripts/monitor-resources.sh
```

### Output Example
```
[2024-01-15 10:30:00] === Resource Monitoring Report ===
[2024-01-15 10:30:00] Timestamp: 2024-01-15 10:30:00
[2024-01-15 10:30:00] Host Memory Usage: 45.23%
[2024-01-15 10:30:00] Host Disk Usage (/): 32.10%
[2024-01-15 10:30:00] Container business-analytics-db:
[2024-01-15 10:30:00]   CPU: 2.50%
[2024-01-15 10:30:00]   Memory: 150MiB/1GiB (14.65%)
[2024-01-15 10:30:00] Container business-analytics-api:
[2024-01-15 10:30:00]   CPU: 1.20%
[2024-01-15 10:30:00]   Memory: 80MiB/512MiB (15.63%)
[2024-01-15 10:30:00] Backup Directory Size: 45M
[2024-01-15 10:30:00] Container Health Status:
[2024-01-15 10:30:00]   business-analytics-db: healthy
[2024-01-15 10:30:00]   business-analytics-api: healthy
[2024-01-15 10:30:00] === Report Complete ===
```

### Thresholds (Configurable)
| Metric | Default Threshold | Environment Variable |
|--------|------------------|---------------------|
| Host Memory | 80% | `MEMORY_THRESHOLD_MB` |
| Host Disk | 85% | `DISK_THRESHOLD` |
| Container CPU | 80% | `CPU_THRESHOLD` |

### Scheduled Monitoring
Add to crontab for periodic checks:
```bash
# Every 15 minutes
*/15 * * * * /scripts/monitor-resources.sh
```

## Log Rotation

### Docker-Managed Rotation (API stdout/stderr)
Configured in `docker-compose.yml`:
```yaml
logging:
  driver: "local"
  options:
    max-size: "10m"
    max-file: "3"
```

### Linux Log Rotation (Script Logs)
Logrotate config at `deploy/logrotate.conf`:
```bash
# Install
sudo cp deploy/logrotate.conf /etc/logrotate.d/business-analytics

# Test
sudo logrotate -d /etc/logrotate.d/business-analytics

# Force run
sudo logrotate -f /etc/logrotate.d/business-analytics
```

### Manual Rotation
```bash
./scripts/rotate-logs.sh
```

### Log Directories
| Directory | Contents | Rotation |
|-----------|----------|----------|
| `/var/log/business-analytics/` | General operations | Daily, 5 files |
| `/var/log/business-analytics/operations/` | Backup, restore, health | Size 10M, 5 files |
| `/var/log/business-analytics/monitoring/` | Resource reports | Daily, 30 files |

## Health Checks

### Quick Health Check
```bash
./scripts/health-check.sh
```

### Exit Codes
- `0` - All checks passed
- `1` - One or more checks failed

### Checks Performed
1. Database connectivity (`pg_isready`)
2. Database query execution (`SELECT 1`)
3. Container running status
4. API liveness endpoint
5. API readiness endpoint

## Environment-Specific Configuration

### Development
```env
ASPNETCORE_ENVIRONMENT=Development
POSTGRES_PASSWORD=dev_password_change_me
JWT_KEY=dev-secret-key-change-in-production-min-32-chars-long
CORS_ORIGIN_0=http://localhost:3000
```

### Production
```env
ASPNETCORE_ENVIRONMENT=Production
POSTGRES_PASSWORD=<strong-random-password>
JWT_KEY=<256-bit-base64-secret>
CORS_ORIGIN_0=https://app.yourdomain.com
# Enable HTTPS
ASPNETCORE_URLS=http://+:8080
# Use reverse proxy for TLS termination
```

### Database Users
| User | Purpose | Permissions |
|------|---------|-------------|
| `postgres` | Admin | Superuser (initial setup only) |
| `app_user` (migration) | Migrations | CREATE, ALTER, DROP on schema |
| `app_user` (runtime) | API | SELECT, INSERT, UPDATE, DELETE on tables |

## Disaster Recovery

### RTO/RPO Targets
| Metric | Target |
|--------|--------|
| RPO (Recovery Point Objective) | 24 hours (daily backup) |
| RTO (Recovery Time Objective) | < 30 minutes |

### Recovery Procedure
1. Provision new PostgreSQL instance
2. Restore latest backup: `./scripts/restore.sh backup-latest.dump`
3. Update connection strings
4. Start API container
5. Verify health checks
6. Run smoke test: `./scripts/smoke-test.sh`

### Backup Storage
**Critical**: The local backup volume is NOT disaster recovery.
- Copy backups to off-site storage (S3, Azure Blob, GCS)
- Test restore from off-site regularly
- Encrypt backups at rest

## Maintenance Windows

### Planned Maintenance
1. Schedule during low-traffic period
2. Notify stakeholders
3. Take backup before changes
4. Apply migrations via migration container
5. Verify health checks
6. Run smoke tests

### Emergency Procedures

#### API Unresponsive
```bash
docker compose restart api
./scripts/health-check.sh
```

#### Database Connection Issues
```bash
docker compose logs db
docker compose restart db
./scripts/health-check.sh
```

#### Disk Full
```bash
df -h
./scripts/rotate-logs.sh
# Clean old backups if needed
find /backups -name "backup-*.dump" -mtime +7 -delete
```

## Security Operations

### Secret Rotation
```bash
# 1. Generate new JWT key
openssl rand -base64 32

# 2. Update .env
JWT_KEY=<new-key>

# 3. Restart API
docker compose up -d api

# 4. All existing tokens invalidated
```

### Database Password Rotation
```bash
# 1. Generate new password
openssl rand -base64 24

# 2. Update PostgreSQL
psql -U postgres -c "ALTER USER app_user PASSWORD '<new-password>';"

# 3. Update .env
POSTGRES_PASSWORD=<new-password>

# 4. Restart services
docker compose up -d
```

## Performance Tuning

### Database
- Monitor slow queries: `pg_stat_statements`
- Adjust `work_mem`, `shared_buffers` based on workload
- Ensure autovacuum runs adequately
- Consider partitioning for large `system_logs` table

### API
- Connection pooling: `Npgsql` default (100 max)
- Increase if needed: `Pooling=true;Maximum Pool Size=200`
- Monitor `ThreadPool` starvation under load

### Container Resources
```yaml
# docker-compose.yml overrides
deploy:
  resources:
    limits:
      cpus: '2'
      memory: 1G
    reservations:
      cpus: '0.5'
      memory: 512M
```