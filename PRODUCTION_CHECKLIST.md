# Production Deployment Checklist

## Pre-Deployment

### Infrastructure
- [ ] PostgreSQL 16+ provisioned (managed service recommended: RDS, Cloud SQL, Azure Database)
- [ ] Database user created with minimal permissions (SELECT, INSERT, UPDATE, DELETE on tables only)
- [ ] Migration user created with DDL permissions (CREATE, ALTER, DROP) - separate from runtime user
- [ ] SSL/TLS certificates configured for PostgreSQL connections
- [ ] Backup retention policy configured (minimum 30 days)
- [ ] Cross-region backup replication enabled
- [ ] Database monitoring/alerting configured (CPU, memory, connections, disk)

### Application
- [ ] All CI checks pass (build, unit tests, architecture tests, integration tests)
- [ ] Security scan passes (Trivy, dotnet list package --vulnerable)
- [ ] Docker image built and pushed to registry (GHCR, ECR, ACR, GCR)
- [ ] Image scanned for vulnerabilities
- [ ] Environment variables documented in `.env.production.example`
- [ ] Secrets stored in secret manager (AWS Secrets Manager, Azure Key Vault, HashiCorp Vault)
- [ ] JWT key generated: `openssl rand -base64 32`
- [ ] CORS origins configured for production frontend

### Networking
- [ ] Load balancer / reverse proxy configured (nginx, Traefik, ALB, Cloud Armor)
- [ ] TLS termination at load balancer (Let's Encrypt or managed certs)
- [ ] API only accessible via HTTPS
- [ ] Database not publicly accessible (private subnet / VPC)
- [ ] Rate limiting configured at load balancer
- [ ] WAF rules enabled for API protection

### Monitoring & Observability
- [ ] Prometheus metrics endpoint exposed (`/metrics` if added, or via health checks)
- [ ] Grafana dashboards provisioned (API latency, error rates, DB connections, business metrics)
- [ ] Log aggregation configured (ELK, Loki, CloudWatch, Datadog)
- [ ] Alerting rules configured:
  - [ ] API health check failures
  - [ ] Database connection pool exhaustion
  - [ ] Disk space > 80%
  - [ ] Memory/CPU > 80% sustained
  - [ ] Backup job failures
  - [ ] Error rate > 1%
  - [ ] P95 latency > 500ms
- [ ] Distributed tracing configured (OpenTelemetry → Jaeger/Zipkin)

### Security
- [ ] All secrets rotated from development defaults
- [ ] Database passwords rotated
- [ ] JWT signing key rotated
- [ ] API keys for external services rotated
- [ ] Security headers configured (HSTS, CSP, X-Frame-Options, etc.)
- [ ] CORS restricted to known origins only
- [ ] API versioning strategy documented
- [ ] Penetration test completed (or scheduled)

## Deployment Steps

### 1. Database Migration
```bash
# Run migration in isolated environment first
docker run --rm \
  -e ConnectionStrings__DefaultConnection="..." \
  ghcr.io/khiwniti/ice-interview-assessment:latest \
  dotnet ef database update
```

### 2. Deploy Application
```bash
# Pull latest image
docker compose -f docker-compose.yml -f docker-compose.production.yml pull

# Start services (migrate runs first via dependency)
docker compose -f docker-compose.yml -f docker-compose.production.yml up -d
```

### 3. Verify Health
```bash
# Wait for health checks
sleep 30

# Check all services
docker compose -f docker-compose.yml -f docker-compose.production.yml ps

# Test endpoints
curl -f https://api.yourdomain.com/health/live
curl -f https://api.yourdomain.com/health/ready
```

### 4. Run Smoke Tests
```bash
# Execute smoke test script against production
./scripts/smoke-test.sh \
  -h https://api.yourdomain.com \
  -u admin@yourdomain.com \
  -p $PROD_ADMIN_PASSWORD
```

### 5. Verify Business Functionality
- [ ] Create department via API
- [ ] Create user via API
- [ ] Create project with tasks
- [ ] Complete task → verify completed_at
- [ ] Complete project → verify all tasks done
- [ ] Generate department report
- [ ] Export JSON and CSV → verify parity
- [ ] Test cross-department access denied (403)

### 6. Verify Operations
- [ ] Backup job runs successfully
- [ ] Restore test from backup completes
- [ ] Log rotation working
- [ ] Monitoring alerts firing correctly
- [ ] Grafana dashboards showing data

## Post-Deployment

### Validation
- [ ] All health checks green for 10+ minutes
- [ ] Error rate < 0.1%
- [ ] P95 latency < 200ms
- [ ] Business metrics tracking correctly

### Documentation
- [ ] Update runbooks with production endpoints
- [ ] Document rollback procedure
- [ ] Update team on new endpoints/changes
- [ ] Schedule post-deployment review (24h, 1w)

### Rollback Plan
```bash
# If issues detected:
# 1. Stop new version
docker compose -f docker-compose.yml -f docker-compose.production.yml stop api

# 2. Revert to previous image tag
# Update TAG in .env.production to previous SHA

# 3. Restart with previous version
docker compose -f docker-compose.yml -f docker-compose.production.yml up -d api

# 4. If DB migration caused issues, restore from backup
./scripts/restore.sh --target-db business_analytics_restore backup-<timestamp>.dump
```

## Emergency Contacts
- [ ] On-call engineer: _______________
- [ ] Database admin: _______________
- [ ] Security team: _______________
- [ ] Infrastructure team: _______________

## Sign-Off
| Role | Name | Signature | Date |
|------|------|-----------|------|
| Developer | | | |
| QA Lead | | | |
| DevOps | | | |
| Security | | | |
| Product Owner | | | |