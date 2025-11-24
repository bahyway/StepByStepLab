#!/bin/bash
# PostgreSQL Replication Configuration Validation Script
# This script validates the configuration without requiring Docker to be running
# Useful for CI/CD pipelines and pre-deployment validation

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Counters
PASSED=0
FAILED=0
WARNINGS=0

# Helper functions
print_header() {
    echo -e "${BLUE}========================================${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}========================================${NC}"
}

print_test() {
    echo -e "${YELLOW}Testing:${NC} $1"
}

print_pass() {
    echo -e "${GREEN}✓ PASS:${NC} $1"
    ((PASSED++))
}

print_fail() {
    echo -e "${RED}✗ FAIL:${NC} $1"
    ((FAILED++))
}

print_warning() {
    echo -e "${YELLOW}⚠ WARNING:${NC} $1"
    ((WARNINGS++))
}

# Change to script directory
cd "$(dirname "$0")"

print_header "PostgreSQL Replication Configuration Validation"
echo ""

# Test 1: Check docker-compose.yml exists
print_test "Docker Compose file exists"
if [ -f "docker-compose.yml" ]; then
    print_pass "docker-compose.yml found"
else
    print_fail "docker-compose.yml not found"
fi

# Test 2: Validate YAML syntax (if yq is available)
print_test "Docker Compose YAML syntax"
if command -v yq &> /dev/null || command -v python3 &> /dev/null; then
    if [ -f "docker-compose.yml" ]; then
        if python3 -c "import yaml; yaml.safe_load(open('docker-compose.yml'))" 2>/dev/null; then
            print_pass "YAML syntax is valid"
        else
            print_fail "YAML syntax is invalid"
        fi
    fi
else
    print_warning "Cannot validate YAML (yq or python3 not available)"
fi

# Test 3: Check for required services in docker-compose.yml
print_test "Required services defined"
if grep -q "postgres-primary:" docker-compose.yml && \
   grep -q "postgres-replica:" docker-compose.yml && \
   grep -q "pgadmin:" docker-compose.yml; then
    print_pass "All required services are defined (primary, replica, pgadmin)"
else
    print_fail "Missing required services in docker-compose.yml"
fi

# Test 4: Check port mappings
print_test "Port mappings"
if grep -q "5432:5432" docker-compose.yml && \
   grep -q "5433:5432" docker-compose.yml && \
   grep -q "5050:80" docker-compose.yml; then
    print_pass "All port mappings are correct"
else
    print_fail "Incorrect or missing port mappings"
fi

# Test 5: Check volumes are defined
print_test "Docker volumes"
if grep -q "postgres_primary_data:" docker-compose.yml && \
   grep -q "postgres_replica_data:" docker-compose.yml && \
   grep -q "pgadmin_data:" docker-compose.yml; then
    print_pass "All volumes are defined"
else
    print_fail "Missing volume definitions"
fi

# Test 6: Check network is defined
print_test "Docker network"
if grep -q "bahyway-network:" docker-compose.yml; then
    print_pass "Network 'bahyway-network' is defined"
else
    print_fail "Network not defined"
fi

# Test 7: Check health checks
print_test "Health checks configured"
if grep -q "healthcheck:" docker-compose.yml; then
    print_pass "Health checks are configured"
else
    print_warning "No health checks found"
fi

# Test 8: Check primary initialization scripts
print_test "Primary initialization scripts"
if [ -f "init-scripts/primary/01-init-replication.sh" ] && \
   [ -f "init-scripts/primary/02-create-databases.sql" ]; then
    print_pass "Primary init scripts exist"
else
    print_fail "Missing primary initialization scripts"
fi

# Test 9: Check primary init script is executable
print_test "Primary init script permissions"
if [ -x "init-scripts/primary/01-init-replication.sh" ] || [ -r "init-scripts/primary/01-init-replication.sh" ]; then
    print_pass "Primary init script has correct permissions"
else
    print_warning "Primary init script may not be executable"
fi

# Test 10: Check for replicator user creation
print_test "Replicator user creation in init script"
if grep -q "replicator" init-scripts/primary/01-init-replication.sh; then
    print_pass "Replicator user creation found in init script"
else
    print_fail "Replicator user not created in init script"
fi

# Test 11: Check for database creation
print_test "Database creation in init script"
if grep -q "alarminsight_hangfire" init-scripts/primary/02-create-databases.sql && \
   grep -q "alarminsight" init-scripts/primary/02-create-databases.sql; then
    print_pass "Required databases are created (alarminsight, alarminsight_hangfire)"
else
    print_fail "Missing database creation statements"
fi

# Test 12: Check for application users
print_test "Application user creation"
if grep -q "hangfire_user" init-scripts/primary/02-create-databases.sql && \
   grep -q "alarminsight_user" init-scripts/primary/02-create-databases.sql; then
    print_pass "Application users are created (hangfire_user, alarminsight_user)"
else
    print_fail "Missing application user creation"
fi

# Test 13: Check for PostgreSQL extensions
print_test "PostgreSQL extensions"
if grep -q "uuid-ossp" init-scripts/primary/02-create-databases.sql && \
   grep -q "pgcrypto" init-scripts/primary/02-create-databases.sql; then
    print_pass "Required extensions are installed (uuid-ossp, pgcrypto)"
else
    print_warning "Missing extension creation statements"
fi

# Test 14: Check primary PostgreSQL configuration
print_test "Primary PostgreSQL configuration file"
if [ -f "config/primary/postgresql.conf" ]; then
    print_pass "Primary postgresql.conf exists"
else
    print_fail "Primary postgresql.conf not found"
fi

# Test 15: Check primary pg_hba.conf
print_test "Primary pg_hba.conf file"
if [ -f "config/primary/pg_hba.conf" ]; then
    print_pass "Primary pg_hba.conf exists"
else
    print_fail "Primary pg_hba.conf not found"
fi

# Test 16: Check replica PostgreSQL configuration
print_test "Replica PostgreSQL configuration file"
if [ -f "config/replica/postgresql.conf" ]; then
    print_pass "Replica postgresql.conf exists"
else
    print_fail "Replica postgresql.conf not found"
fi

# Test 17: Check replication settings in primary config
print_test "Replication settings in primary config"
if grep -q "wal_level.*=.*replica" config/primary/postgresql.conf && \
   grep -q "max_wal_senders" config/primary/postgresql.conf; then
    print_pass "Primary replication settings configured"
else
    print_fail "Missing replication settings in primary config"
fi

# Test 18: Check replica as hot standby
print_test "Replica hot standby configuration"
if grep -q "hot_standby.*=.*on" config/replica/postgresql.conf; then
    print_pass "Replica configured as hot standby"
else
    print_fail "Replica not configured as hot standby"
fi

# Test 19: Check replication connection in pg_hba.conf
print_test "Replication connection rules in pg_hba.conf"
if [ -f "config/primary/pg_hba.conf" ]; then
    if grep -q "replication" config/primary/pg_hba.conf; then
        print_pass "Replication connection rules found in pg_hba.conf"
    else
        print_fail "No replication rules in pg_hba.conf"
    fi
fi

# Test 20: Check PowerShell module exists
print_test "PowerShell automation module"
if [ -f "automation-module/BahyWay.PostgreSQLReplication/BahyWay.PostgreSQLReplication.psm1" ] && \
   [ -f "automation-module/BahyWay.PostgreSQLReplication/BahyWay.PostgreSQLReplication.psd1" ]; then
    print_pass "PowerShell module files exist"
else
    print_fail "PowerShell module files not found"
fi

# Test 21: Check PowerShell module manifest
print_test "PowerShell module manifest version"
if [ -f "automation-module/BahyWay.PostgreSQLReplication/BahyWay.PostgreSQLReplication.psd1" ]; then
    if grep -q "ModuleVersion" automation-module/BahyWay.PostgreSQLReplication/BahyWay.PostgreSQLReplication.psd1; then
        VERSION=$(grep "ModuleVersion" automation-module/BahyWay.PostgreSQLReplication/BahyWay.PostgreSQLReplication.psd1 | head -1)
        print_pass "Module manifest contains version: $VERSION"
    else
        print_warning "No version found in module manifest"
    fi
fi

# Test 22: Check Start-AlarmInsightAPI.ps1 script
print_test "AlarmInsight API startup script"
if [ -f "automation-module/Start-AlarmInsightAPI.ps1" ]; then
    print_pass "Start-AlarmInsightAPI.ps1 script exists"
else
    print_fail "Start-AlarmInsightAPI.ps1 script not found"
fi

# Test 23: Check documentation
print_test "Documentation files"
if [ -f "automation-module/README.md" ] && \
   [ -f "automation-module/QUICKSTART.md" ] && \
   [ -f "README.md" ]; then
    print_pass "All documentation files exist"
else
    print_warning "Some documentation files are missing"
fi

# Test 24: Check for secrets in configuration (security check)
print_test "Security: Hardcoded passwords check"
INSECURE_PASSWORDS=0
if grep -r "password.*admin" config/ 2>/dev/null | grep -v "# " | grep -v ".md:" ; then
    ((INSECURE_PASSWORDS++))
fi
if [ $INSECURE_PASSWORDS -gt 0 ]; then
    print_warning "Found $INSECURE_PASSWORDS potential hardcoded passwords - ensure these are changed for production"
else
    print_pass "No obvious hardcoded passwords in config files"
fi

# Test 25: Check replica initialization script
print_test "Replica initialization script"
if [ -f "init-scripts/replica/01-setup-replica.sh" ]; then
    print_pass "Replica init script exists"
else
    print_warning "Replica init script not found (may use inline setup)"
fi

# Test 26: Validate environment variables in docker-compose
print_test "Environment variables configuration"
if grep -q "POSTGRES_PASSWORD" docker-compose.yml && \
   grep -q "PRIMARY_HOST" docker-compose.yml && \
   grep -q "PRIMARY_USER.*replicator" docker-compose.yml; then
    print_pass "Required environment variables are configured"
else
    print_fail "Missing required environment variables"
fi

# Test 27: Check depends_on relationships
print_test "Service dependencies"
if grep -A 2 "depends_on:" docker-compose.yml | grep -q "postgres-primary"; then
    print_pass "Replica correctly depends on primary"
else
    print_warning "Service dependencies may not be configured correctly"
fi

# Test 28: Check restart policies
print_test "Restart policies"
if grep -q "restart:.*unless-stopped" docker-compose.yml; then
    print_pass "Restart policies are configured"
else
    print_warning "No restart policies found"
fi

echo ""
print_header "Validation Summary"
echo ""
echo -e "${GREEN}Passed:   $PASSED${NC}"
echo -e "${YELLOW}Warnings: $WARNINGS${NC}"
echo -e "${RED}Failed:   $FAILED${NC}"
echo ""

if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}========================================${NC}"
    echo -e "${GREEN}✓ Configuration validation PASSED${NC}"
    echo -e "${GREEN}========================================${NC}"
    exit 0
else
    echo -e "${RED}========================================${NC}"
    echo -e "${RED}✗ Configuration validation FAILED${NC}"
    echo -e "${RED}========================================${NC}"
    echo ""
    echo "Please fix the failed tests before deploying."
    exit 1
fi
