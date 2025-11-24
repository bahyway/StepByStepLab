#!/bin/bash
# Initialize PostgreSQL Primary for Replication
# BahyWay PostgreSQL HA Setup

set -e

echo "================================================"
echo "Initializing PostgreSQL Primary for Replication"
echo "================================================"

# Create replication user
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    -- Create replication user
    DO \$\$
    BEGIN
        IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'replicator') THEN
            CREATE ROLE replicator WITH REPLICATION LOGIN PASSWORD 'replicator_pass';
            RAISE NOTICE 'Created replication user: replicator';
        ELSE
            RAISE NOTICE 'Replication user already exists: replicator';
        END IF;
    END
    \$\$;

    -- Grant necessary permissions
    GRANT CONNECT ON DATABASE postgres TO replicator;
EOSQL

echo "✓ Replication user created successfully"
echo "================================================"
