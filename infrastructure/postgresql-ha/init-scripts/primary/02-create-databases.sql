-- Create Databases and Users for BahyWay AlarmInsight API
-- BahyWay PostgreSQL HA Setup

-- ================================================
-- Create Hangfire Database and User
-- ================================================
CREATE DATABASE alarminsight_hangfire
    WITH
    OWNER = postgres
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.utf8'
    LC_CTYPE = 'en_US.utf8'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1;

\c alarminsight_hangfire

-- Create Hangfire user
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'hangfire_user') THEN
        CREATE ROLE hangfire_user WITH LOGIN PASSWORD 'hangfire_pass';
        RAISE NOTICE 'Created user: hangfire_user';
    ELSE
        RAISE NOTICE 'User already exists: hangfire_user';
    END IF;
END
$$;

-- Grant privileges to Hangfire user
GRANT ALL PRIVILEGES ON DATABASE alarminsight_hangfire TO hangfire_user;
GRANT ALL ON SCHEMA public TO hangfire_user;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO hangfire_user;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO hangfire_user;

-- ================================================
-- Create AlarmInsight Application Database and User
-- ================================================
\c postgres

CREATE DATABASE alarminsight
    WITH
    OWNER = postgres
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.utf8'
    LC_CTYPE = 'en_US.utf8'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1;

\c alarminsight

-- Create AlarmInsight user
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'alarminsight_user') THEN
        CREATE ROLE alarminsight_user WITH LOGIN PASSWORD 'alarminsight_pass';
        RAISE NOTICE 'Created user: alarminsight_user';
    ELSE
        RAISE NOTICE 'User already exists: alarminsight_user';
    END IF;
END
$$;

-- Grant privileges to AlarmInsight user
GRANT ALL PRIVILEGES ON DATABASE alarminsight TO alarminsight_user;
GRANT ALL ON SCHEMA public TO alarminsight_user;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO alarminsight_user;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO alarminsight_user;

-- ================================================
-- Create Extensions (Optional but useful)
-- ================================================
\c alarminsight_hangfire
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

\c alarminsight
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- ================================================
-- Summary
-- ================================================
\c postgres

SELECT 'Database initialization complete!' AS status;
SELECT datname, pg_encoding_to_char(encoding) AS encoding, datcollate, datctype
FROM pg_database
WHERE datname IN ('alarminsight_hangfire', 'alarminsight');
