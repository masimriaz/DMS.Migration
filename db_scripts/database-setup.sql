-- ============================================================
-- SharePoint Migration Tool - PostgreSQL Database Setup
-- Database: DMS
-- Note: Using existing PostgreSQL container user (cdp_user)
-- ============================================================

-- Drop existing database if exists (CAUTION: This will delete all data)
DROP DATABASE IF EXISTS "DMS";

-- Create database
CREATE DATABASE "DMS"
    WITH 
    OWNER = cdp_user
    ENCODING = 'UTF8'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1;

-- Connect to DMS database
\c DMS

-- ============================================================
-- CREATE SCHEMAS
-- ============================================================

CREATE SCHEMA IF NOT EXISTS core;
CREATE SCHEMA IF NOT EXISTS connections;
CREATE SCHEMA IF NOT EXISTS jobs;
CREATE SCHEMA IF NOT EXISTS validation;
CREATE SCHEMA IF NOT EXISTS reporting;
CREATE SCHEMA IF NOT EXISTS audit;

-- ============================================================
-- CORE SCHEMA - Multi-Tenancy & Users
-- ============================================================

-- Tenants Table
CREATE TABLE core.tenants (
    id SERIAL PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    code VARCHAR(50) NOT NULL UNIQUE,
    description VARCHAR(1000),
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(200) NOT NULL,
    updated_at TIMESTAMPTZ,
    updated_by VARCHAR(200)
);

CREATE INDEX idx_tenants_code ON core.tenants(code);
CREATE INDEX idx_tenants_is_active ON core.tenants(is_active);

-- Roles Table
CREATE TABLE core.roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    code VARCHAR(50) NOT NULL UNIQUE,
    description VARCHAR(500),
    is_system_role BOOLEAN NOT NULL DEFAULT false,
    priority INTEGER NOT NULL DEFAULT 0,
    permissions_json JSONB NOT NULL DEFAULT '[]',
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_roles_code ON core.roles(code);
CREATE INDEX idx_roles_is_system_role ON core.roles(is_system_role);

-- Users Table
CREATE TABLE core.users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id INTEGER NOT NULL REFERENCES core.tenants(id) ON DELETE RESTRICT,
    username VARCHAR(100) NOT NULL,
    email VARCHAR(255) NOT NULL,
    full_name VARCHAR(200) NOT NULL,
    password_hash VARCHAR(500) NOT NULL,
    password_salt VARCHAR(500),
    is_active BOOLEAN NOT NULL DEFAULT true,
    is_locked BOOLEAN NOT NULL DEFAULT false,
    failed_login_attempts INTEGER NOT NULL DEFAULT 0,
    last_login_at TIMESTAMPTZ,
    locked_until TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(200) NOT NULL,
    updated_at TIMESTAMPTZ,
    updated_by VARCHAR(200)
);

CREATE UNIQUE INDEX idx_users_tenant_username ON core.users(tenant_id, username);
CREATE UNIQUE INDEX idx_users_tenant_email ON core.users(tenant_id, email);
CREATE INDEX idx_users_is_active ON core.users(is_active);
CREATE INDEX idx_users_created_at ON core.users(created_at);

-- User Roles Table (Many-to-Many)
CREATE TABLE core.user_roles (
    user_id UUID NOT NULL REFERENCES core.users(id) ON DELETE CASCADE,
    role_id UUID NOT NULL REFERENCES core.roles(id) ON DELETE CASCADE,
    assigned_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    assigned_by VARCHAR(200) NOT NULL,
    PRIMARY KEY (user_id, role_id)
);

CREATE INDEX idx_user_roles_user_id ON core.user_roles(user_id);
CREATE INDEX idx_user_roles_role_id ON core.user_roles(role_id);
CREATE INDEX idx_user_roles_assigned_at ON core.user_roles(assigned_at);

-- ============================================================
-- CONNECTIONS SCHEMA
-- ============================================================

-- Connections Table
CREATE TABLE connections.connections (
    id SERIAL PRIMARY KEY,
    tenant_id INTEGER NOT NULL REFERENCES core.tenants(id) ON DELETE RESTRICT,
    name VARCHAR(200) NOT NULL,
    description VARCHAR(1000),
    role INTEGER NOT NULL, -- 1=Source, 2=Target
    type INTEGER NOT NULL, -- 1=SharePointOnPrem, 2=SharePointOnline, 3=OneDrive, 4=FileShare
    status INTEGER NOT NULL DEFAULT 0, -- 0=Draft, 1=Verified, 2=Failed, 3=Disabled
    endpoint_url VARCHAR(500) NOT NULL,
    authentication_mode VARCHAR(50),
    username VARCHAR(200),
    throttling_profile INTEGER NOT NULL DEFAULT 1,
    preserve_authorship BOOLEAN NOT NULL DEFAULT true,
    preserve_timestamps BOOLEAN NOT NULL DEFAULT true,
    replace_illegal_characters BOOLEAN NOT NULL DEFAULT true,
    last_verified_at TIMESTAMPTZ,
    last_verification_result TEXT,
    last_verification_diagnostics TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(200) NOT NULL,
    updated_at TIMESTAMPTZ,
    updated_by VARCHAR(200),
    is_deleted BOOLEAN NOT NULL DEFAULT false
);

CREATE INDEX idx_connections_tenant_status ON connections.connections(tenant_id, status);
CREATE INDEX idx_connections_tenant_type ON connections.connections(tenant_id, type);
CREATE INDEX idx_connections_tenant_role ON connections.connections(tenant_id, role);
CREATE INDEX idx_connections_is_deleted ON connections.connections(is_deleted);
CREATE INDEX idx_connections_created_at ON connections.connections(created_at);

-- Connection Secrets Table
CREATE TABLE connections.connection_secrets (
    id SERIAL PRIMARY KEY,
    connection_id INTEGER NOT NULL UNIQUE REFERENCES connections.connections(id) ON DELETE CASCADE,
    encrypted_secret TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ
);

CREATE INDEX idx_connection_secrets_connection_id ON connections.connection_secrets(connection_id);

-- Connection Verification Runs Table
CREATE TABLE connections.connection_verification_runs (
    id SERIAL PRIMARY KEY,
    connection_id INTEGER NOT NULL REFERENCES connections.connections(id) ON DELETE CASCADE,
    tenant_id INTEGER NOT NULL,
    status INTEGER NOT NULL, -- 0=NotStarted, 1=Running, 2=Success, 3=Failed
    started_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    completed_at TIMESTAMPTZ,
    result TEXT,
    diagnostics TEXT,
    diagnostics_json JSONB,
    error_message TEXT,
    initiated_by VARCHAR(200) NOT NULL
);

CREATE INDEX idx_conn_verification_connection_started ON connections.connection_verification_runs(connection_id, started_at);
CREATE INDEX idx_conn_verification_tenant_status ON connections.connection_verification_runs(tenant_id, status);
CREATE INDEX idx_conn_verification_started_at ON connections.connection_verification_runs(started_at);

-- ============================================================
-- JOBS SCHEMA - Migration Planning & Execution
-- ============================================================

-- Migration Plans Table
CREATE TABLE jobs.migration_plans (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id INTEGER NOT NULL REFERENCES core.tenants(id) ON DELETE RESTRICT,
    name VARCHAR(200) NOT NULL,
    description VARCHAR(1000),
    source_connection_id INTEGER REFERENCES connections.connections(id) ON DELETE SET NULL,
    target_connection_id INTEGER REFERENCES connections.connections(id) ON DELETE SET NULL,
    status INTEGER NOT NULL DEFAULT 0, -- 0=Draft, 1=Ready, 2=InProgress, 3=Completed, 4=Failed, 5=Cancelled
    configuration_json JSONB NOT NULL DEFAULT '{}',
    mapping_rules_json JSONB NOT NULL DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(200) NOT NULL,
    updated_at TIMESTAMPTZ,
    updated_by VARCHAR(200)
);

CREATE INDEX idx_migration_plans_tenant_status ON jobs.migration_plans(tenant_id, status);
CREATE INDEX idx_migration_plans_created_at ON jobs.migration_plans(created_at);

-- Migration Jobs Table
CREATE TABLE jobs.migration_jobs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id INTEGER NOT NULL REFERENCES core.tenants(id) ON DELETE RESTRICT,
    migration_plan_id UUID REFERENCES jobs.migration_plans(id) ON DELETE SET NULL,
    name VARCHAR(200) NOT NULL,
    description VARCHAR(1000),
    type INTEGER NOT NULL, -- 1=Discovery, 2=Migration, 3=Validation, 4=Incremental
    status INTEGER NOT NULL DEFAULT 0, -- 0=Draft, 1=Pending, 2=Queued, 3=Running, 4=Paused, 5=Completed, 6=Failed, 7=Cancelled
    source_connection_id INTEGER REFERENCES connections.connections(id) ON DELETE SET NULL,
    target_connection_id INTEGER REFERENCES connections.connections(id) ON DELETE SET NULL,
    configuration_json JSONB NOT NULL DEFAULT '{}',
    total_items INTEGER NOT NULL DEFAULT 0,
    processed_items INTEGER NOT NULL DEFAULT 0,
    successful_items INTEGER NOT NULL DEFAULT 0,
    failed_items INTEGER NOT NULL DEFAULT 0,
    skipped_items INTEGER NOT NULL DEFAULT 0,
    total_bytes BIGINT NOT NULL DEFAULT 0,
    processed_bytes BIGINT NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(200) NOT NULL,
    started_at TIMESTAMPTZ,
    completed_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ,
    error_message TEXT,
    diagnostics_json JSONB
);

CREATE INDEX idx_migration_jobs_tenant_status ON jobs.migration_jobs(tenant_id, status);
CREATE INDEX idx_migration_jobs_tenant_type ON jobs.migration_jobs(tenant_id, type);
CREATE INDEX idx_migration_jobs_created_at ON jobs.migration_jobs(created_at);
CREATE INDEX idx_migration_jobs_started_at ON jobs.migration_jobs(started_at);

-- Migration Tasks Table
CREATE TABLE jobs.migration_tasks (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    migration_job_id UUID NOT NULL REFERENCES jobs.migration_jobs(id) ON DELETE CASCADE,
    name VARCHAR(200) NOT NULL,
    description VARCHAR(1000),
    status INTEGER NOT NULL DEFAULT 0, -- 0=Pending, 1=Running, 2=Completed, 3=Failed, 4=Skipped, 5=Retrying
    sequence INTEGER NOT NULL,
    source_path VARCHAR(2000),
    target_path VARCHAR(2000),
    task_data_json JSONB NOT NULL DEFAULT '{}',
    items_processed INTEGER NOT NULL DEFAULT 0,
    items_total INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    started_at TIMESTAMPTZ,
    completed_at TIMESTAMPTZ,
    error_message TEXT,
    retry_count INTEGER NOT NULL DEFAULT 0
);

CREATE INDEX idx_migration_tasks_job_status ON jobs.migration_tasks(migration_job_id, status);
CREATE INDEX idx_migration_tasks_job_sequence ON jobs.migration_tasks(migration_job_id, sequence);
CREATE INDEX idx_migration_tasks_created_at ON jobs.migration_tasks(created_at);

-- Task Checkpoints Table
CREATE TABLE jobs.task_checkpoints (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    migration_task_id UUID NOT NULL REFERENCES jobs.migration_tasks(id) ON DELETE CASCADE,
    checkpoint_type VARCHAR(50) NOT NULL,
    checkpoint_data JSONB NOT NULL DEFAULT '{}',
    items_processed INTEGER NOT NULL DEFAULT 0,
    items_total INTEGER NOT NULL DEFAULT 0,
    bytes_processed BIGINT NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_task_checkpoints_task_created ON jobs.task_checkpoints(migration_task_id, created_at);

-- ============================================================
-- VALIDATION SCHEMA
-- ============================================================

-- Validation Runs Table
CREATE TABLE validation.validation_runs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id INTEGER NOT NULL REFERENCES core.tenants(id) ON DELETE RESTRICT,
    migration_job_id UUID REFERENCES jobs.migration_jobs(id) ON DELETE SET NULL,
    name VARCHAR(200) NOT NULL,
    type INTEGER NOT NULL, -- 1=PreMigration, 2=PostMigration, 3=ContentComparison, etc.
    status INTEGER NOT NULL DEFAULT 0, -- 0=NotStarted, 1=Running, 2=Completed, 3=Failed, 4=PartiallyCompleted
    source_connection_id INTEGER REFERENCES connections.connections(id) ON DELETE SET NULL,
    target_connection_id INTEGER REFERENCES connections.connections(id) ON DELETE SET NULL,
    parameters_json JSONB NOT NULL DEFAULT '{}',
    started_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    completed_at TIMESTAMPTZ,
    total_checks INTEGER NOT NULL DEFAULT 0,
    passed_checks INTEGER NOT NULL DEFAULT 0,
    failed_checks INTEGER NOT NULL DEFAULT 0,
    warning_checks INTEGER NOT NULL DEFAULT 0,
    error_message TEXT,
    created_by VARCHAR(200) NOT NULL
);

CREATE INDEX idx_validation_runs_tenant_status ON validation.validation_runs(tenant_id, status);
CREATE INDEX idx_validation_runs_tenant_type ON validation.validation_runs(tenant_id, type);
CREATE INDEX idx_validation_runs_started_at ON validation.validation_runs(started_at);

-- Validation Findings Table
CREATE TABLE validation.validation_findings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    validation_run_id UUID NOT NULL REFERENCES validation.validation_runs(id) ON DELETE CASCADE,
    severity INTEGER NOT NULL, -- 0=Info, 1=Warning, 2=Error, 3=Critical
    category VARCHAR(100) NOT NULL,
    title VARCHAR(200) NOT NULL,
    description VARCHAR(2000) NOT NULL,
    source_path VARCHAR(2000),
    target_path VARCHAR(2000),
    details_json JSONB NOT NULL DEFAULT '{}',
    recommended_action VARCHAR(1000),
    is_resolved BOOLEAN NOT NULL DEFAULT false,
    detected_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_validation_findings_run_severity ON validation.validation_findings(validation_run_id, severity);
CREATE INDEX idx_validation_findings_run_category ON validation.validation_findings(validation_run_id, category);
CREATE INDEX idx_validation_findings_detected_at ON validation.validation_findings(detected_at);

-- ============================================================
-- REPORTING SCHEMA
-- ============================================================

-- Reports Table
CREATE TABLE reporting.reports (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id INTEGER NOT NULL REFERENCES core.tenants(id) ON DELETE RESTRICT,
    name VARCHAR(200) NOT NULL,
    type INTEGER NOT NULL, -- 1=MigrationSummary, 2=ValidationReport, 3=AuditTrail, etc.
    status INTEGER NOT NULL DEFAULT 0, -- 0=Generating, 1=Ready, 2=Failed, 3=Expired
    migration_job_id UUID REFERENCES jobs.migration_jobs(id) ON DELETE SET NULL,
    validation_run_id UUID REFERENCES validation.validation_runs(id) ON DELETE SET NULL,
    parameters_json JSONB NOT NULL DEFAULT '{}',
    generated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMPTZ,
    generated_by VARCHAR(200) NOT NULL,
    total_size_bytes BIGINT NOT NULL DEFAULT 0
);

CREATE INDEX idx_reports_tenant_type ON reporting.reports(tenant_id, type);
CREATE INDEX idx_reports_tenant_status ON reporting.reports(tenant_id, status);
CREATE INDEX idx_reports_generated_at ON reporting.reports(generated_at);
CREATE INDEX idx_reports_expires_at ON reporting.reports(expires_at);

-- Report Files Table
CREATE TABLE reporting.report_files (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    report_id UUID NOT NULL REFERENCES reporting.reports(id) ON DELETE CASCADE,
    file_name VARCHAR(255) NOT NULL,
    file_type VARCHAR(50) NOT NULL,
    size_bytes BIGINT NOT NULL,
    storage_path VARCHAR(1000) NOT NULL,
    content_hash VARCHAR(128),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_report_files_report_id ON reporting.report_files(report_id);
CREATE INDEX idx_report_files_created_at ON reporting.report_files(created_at);

-- ============================================================
-- AUDIT SCHEMA - Logging & Observability
-- ============================================================

-- Audit Events Table
CREATE TABLE audit.audit_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id INTEGER NOT NULL REFERENCES core.tenants(id) ON DELETE RESTRICT,
    entity_type VARCHAR(100) NOT NULL,
    entity_id VARCHAR(100) NOT NULL,
    action VARCHAR(100) NOT NULL,
    old_values_json JSONB,
    new_values_json JSONB,
    metadata_json JSONB,
    timestamp TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    user_id UUID REFERENCES core.users(id) ON DELETE SET NULL,
    username VARCHAR(200) NOT NULL,
    ip_address VARCHAR(45),
    user_agent VARCHAR(500)
);

CREATE INDEX idx_audit_events_tenant_timestamp ON audit.audit_events(tenant_id, timestamp);
CREATE INDEX idx_audit_events_entity ON audit.audit_events(entity_type, entity_id);
CREATE INDEX idx_audit_events_user_id ON audit.audit_events(user_id);
CREATE INDEX idx_audit_events_timestamp ON audit.audit_events(timestamp);

-- Job Logs Table (High-Volume)
CREATE TABLE audit.job_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    migration_job_id UUID NOT NULL REFERENCES jobs.migration_jobs(id) ON DELETE CASCADE,
    migration_task_id UUID REFERENCES jobs.migration_tasks(id) ON DELETE CASCADE,
    level INTEGER NOT NULL, -- 0=Trace, 1=Debug, 2=Information, 3=Warning, 4=Error, 5=Critical
    message VARCHAR(4000) NOT NULL,
    exception TEXT,
    stack_trace TEXT,
    details_json JSONB NOT NULL DEFAULT '{}',
    timestamp TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_job_logs_job_timestamp ON audit.job_logs(migration_job_id, timestamp);
CREATE INDEX idx_job_logs_job_level ON audit.job_logs(migration_job_id, level);
CREATE INDEX idx_job_logs_task_id ON audit.job_logs(migration_task_id);
CREATE INDEX idx_job_logs_timestamp ON audit.job_logs(timestamp);

-- ============================================================
-- SEED DATA
-- ============================================================

-- Insert Demo Tenant
INSERT INTO core.tenants (id, name, code, description, is_active, created_at, created_by, updated_at, updated_by)
VALUES (1, 'Demo Corporation', 'DEMO', 'Demonstration tenant for SharePoint migration tool', true, CURRENT_TIMESTAMP, 'System', CURRENT_TIMESTAMP, 'System');

-- Insert System Roles
INSERT INTO core.roles (id, name, code, description, is_system_role, priority, permissions_json, created_at)
VALUES 
    ('11111111-1111-1111-1111-111111111111', 'Administrator', 'ADMIN', 'Full system access with all permissions', true, 100, '["*"]', CURRENT_TIMESTAMP),
    ('22222222-2222-2222-2222-222222222222', 'Migration Operator', 'OPERATOR', 'Can create and manage migrations', true, 50, '["connections.view", "connections.create", "migrations.view", "migrations.create", "migrations.execute"]', CURRENT_TIMESTAMP),
    ('33333333-3333-3333-3333-333333333333', 'Viewer', 'VIEWER', 'Read-only access to view migrations and reports', true, 10, '["connections.view", "migrations.view", "reports.view"]', CURRENT_TIMESTAMP);

-- Insert Admin User
INSERT INTO core.users (id, tenant_id, username, email, full_name, password_hash, is_active, created_at, created_by)
VALUES ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 1, 'admin', 'admin@democorp.com', 'System Administrator', 'demo-hash-change-in-production', true, CURRENT_TIMESTAMP, 'System');

-- Assign Admin Role to Admin User
INSERT INTO core.user_roles (user_id, role_id, assigned_at, assigned_by)
VALUES ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '11111111-1111-1111-1111-111111111111', CURRENT_TIMESTAMP, 'System');

-- Insert Sample Connections
INSERT INTO connections.connections (id, tenant_id, name, description, role, type, status, endpoint_url, authentication_mode, username, throttling_profile, preserve_authorship, preserve_timestamps, replace_illegal_characters, created_at, created_by, is_deleted)
VALUES 
    (1, 1, 'SharePoint 2016 Source', 'Legacy SharePoint 2016 on-premises environment', 1, 1, 0, 'https://sharepoint2016.democorp.local', 'ServiceAccount', 'sp_migration@democorp.local', 1, true, true, true, CURRENT_TIMESTAMP, 'admin', false),
    (2, 1, 'SharePoint Online Target', 'Modern SharePoint Online tenant', 2, 2, 0, 'https://democorp.sharepoint.com', 'AppOnly', 'app@democorp.onmicrosoft.com', 1, true, true, true, CURRENT_TIMESTAMP, 'admin', false);

-- Insert Sample Migration Plan
INSERT INTO jobs.migration_plans (id, tenant_id, name, description, source_connection_id, target_connection_id, status, configuration_json, mapping_rules_json, created_at, created_by)
VALUES ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 1, 'Legacy to Modern Migration', 'Migrate from SharePoint 2016 to SharePoint Online', 1, 2, 0, 
    '{"preserveVersionHistory": true, "migratePermissions": true, "migrateWorkflows": false, "maxFileSizeMB": 15000, "parallelism": 5}',
    '{"siteMapping": [{"source": "/sites/legacy", "target": "/sites/modern"}], "userMapping": []}',
    CURRENT_TIMESTAMP, 'admin');

-- Insert Sample Migration Job
INSERT INTO jobs.migration_jobs (id, tenant_id, migration_plan_id, name, description, type, status, source_connection_id, target_connection_id, configuration_json, created_at, created_by)
VALUES ('cccccccc-cccc-cccc-cccc-cccccccccccc', 1, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'Demo Migration Job - Site Collection Alpha', 'Initial test migration of site collection /sites/alpha', 2, 0, 1, 2,
    '{"sourceSiteUrl": "/sites/alpha", "targetSiteUrl": "/sites/alpha-migrated", "includeSubsites": true, "includeDocumentLibraries": true, "includeListData": true}',
    CURRENT_TIMESTAMP, 'admin');

-- Insert Initial Audit Event
INSERT INTO audit.audit_events (id, tenant_id, entity_type, entity_id, action, new_values_json, metadata_json, timestamp, user_id, username, ip_address, user_agent)
VALUES (gen_random_uuid(), 1, 'Database', 'System', 'Seed', '{"message": "Initial database seeding completed"}', '{"environment": "Development"}', CURRENT_TIMESTAMP, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'admin', '127.0.0.1', 'DatabaseSeeder/1.0');

-- ============================================================
-- COMPLETION
-- ============================================================

-- Grant permissions (optional, adjust based on your needs)
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA core TO cdp_user;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA connections TO cdp_user;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA jobs TO cdp_user;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA validation TO cdp_user;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA reporting TO cdp_user;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA audit TO cdp_user;

GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA core TO cdp_user;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA connections TO cdp_user;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA jobs TO cdp_user;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA validation TO cdp_user;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA reporting TO cdp_user;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA audit TO cdp_user;

-- Display completion message
SELECT 'Database DMS created successfully with all tables and seed data!' AS status;
