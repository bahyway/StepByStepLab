#!/bin/bash
# Setup PostgreSQL Replica (Standby Mode)
# BahyWay PostgreSQL HA Setup

set -e

echo "================================================"
echo "PostgreSQL Replica - Standby Mode"
echo "================================================"
echo "This replica is initialized via pg_basebackup in docker-compose"
echo "No additional initialization required"
echo "Replica will stream from primary: ${PRIMARY_HOST:-postgres-primary}"
echo "================================================"
