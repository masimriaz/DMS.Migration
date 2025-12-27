-- ============================================================
-- Discovery Schema - EF Core Migration
-- Run this after EF migration or manually execute
-- ============================================================

-- Create discovery schema
CREATE SCHEMA IF NOT EXISTS discovery;

-- Discovery Runs Table
CREATE TABLE IF NOT EXISTS discovery.discovery_runs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id INTEGER NOT NULL,
    source_connection_id INTEGER NOT NULL,
    name VARCHAR(200) NOT NULL,
    description VARCHAR(1000),
    correlation_id VARCHAR(100) NOT NULL,
    status INTEGER NOT NULL DEFAULT 0,
    scope_url VARCHAR(2000) NOT NULL,
    configuration_json JSONB NOT NULL DEFAULT '{}',
    progress_percentage INTEGER NOT NULL DEFAULT 0,
    current_step VARCHAR(200),
    total_sites_scanned INTEGER NOT NULL DEFAULT 0,
    total_lists_scanned INTEGER NOT NULL DEFAULT 0,
    total_items_scanned INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(200) NOT NULL,
    started_at TIMESTAMPTZ,
    completed_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ,
    error_message VARCHAR(4000),
    retry_count INTEGER NOT NULL DEFAULT 0,
    is_archived BOOLEAN NOT NULL DEFAULT false,
    archived_at TIMESTAMPTZ,
    
    CONSTRAINT fk_discovery_runs_tenant FOREIGN KEY (tenant_id) REFERENCES core.tenants(id) ON DELETE RESTRICT,
    CONSTRAINT fk_discovery_runs_connection FOREIGN KEY (source_connection_id) REFERENCES connections.connections(id) ON DELETE RESTRICT
);

CREATE INDEX idx_discovery_runs_tenant_id ON discovery.discovery_runs(tenant_id);
CREATE INDEX idx_discovery_runs_source_connection_id ON discovery.discovery_runs(source_connection_id);
CREATE INDEX idx_discovery_runs_status ON discovery.discovery_runs(status);
CREATE INDEX idx_discovery_runs_correlation_id ON discovery.discovery_runs(correlation_id);
CREATE INDEX idx_discovery_runs_created_at ON discovery.discovery_runs(created_at);
CREATE INDEX idx_discovery_runs_tenant_status ON discovery.discovery_runs(tenant_id, status);

-- Discovery Items Table
CREATE TABLE IF NOT EXISTS discovery.discovery_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    discovery_run_id UUID NOT NULL,
    tenant_id INTEGER NOT NULL,
    item_type INTEGER NOT NULL,
    parent_item_id UUID,
    path VARCHAR(2000) NOT NULL,
    level INTEGER NOT NULL DEFAULT 0,
    title VARCHAR(500) NOT NULL,
    internal_name VARCHAR(500),
    share_point_id UUID,
    item_count INTEGER NOT NULL DEFAULT 0,
    folder_count INTEGER NOT NULL DEFAULT 0,
    size_in_bytes BIGINT NOT NULL DEFAULT 0,
    versioning_enabled BOOLEAN,
    major_version_limit INTEGER,
    minor_version_limit INTEGER,
    sample_version_count INTEGER,
    checked_out_items_count INTEGER,
    has_unique_permissions BOOLEAN,
    unique_permission_count INTEGER,
    content_types_json JSONB,
    columns_json JSONB,
    template_type VARCHAR(100),
    has_custom_pages BOOLEAN,
    has_web_parts BOOLEAN,
    metadata_json JSONB NOT NULL DEFAULT '{}',
    created_date TIMESTAMPTZ,
    modified_date TIMESTAMPTZ,
    discovered_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT fk_discovery_items_run FOREIGN KEY (discovery_run_id) REFERENCES discovery.discovery_runs(id) ON DELETE CASCADE,
    CONSTRAINT fk_discovery_items_tenant FOREIGN KEY (tenant_id) REFERENCES core.tenants(id) ON DELETE RESTRICT,
    CONSTRAINT fk_discovery_items_parent FOREIGN KEY (parent_item_id) REFERENCES discovery.discovery_items(id) ON DELETE RESTRICT
);

CREATE INDEX idx_discovery_items_discovery_run_id ON discovery.discovery_items(discovery_run_id);
CREATE INDEX idx_discovery_items_tenant_id ON discovery.discovery_items(tenant_id);
CREATE INDEX idx_discovery_items_parent_item_id ON discovery.discovery_items(parent_item_id);
CREATE INDEX idx_discovery_items_item_type ON discovery.discovery_items(item_type);
CREATE INDEX idx_discovery_items_path ON discovery.discovery_items(path);
CREATE INDEX idx_discovery_items_run_type ON discovery.discovery_items(discovery_run_id, item_type);
CREATE INDEX idx_discovery_items_tenant_run ON discovery.discovery_items(tenant_id, discovery_run_id);

-- Discovery Metrics Table
CREATE TABLE IF NOT EXISTS discovery.discovery_metrics (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    discovery_run_id UUID NOT NULL,
    tenant_id INTEGER NOT NULL,
    metric_key VARCHAR(100) NOT NULL,
    metric_category VARCHAR(100) NOT NULL,
    numeric_value BIGINT NOT NULL DEFAULT 0,
    string_value VARCHAR(500),
    json_value JSONB,
    calculated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT fk_discovery_metrics_run FOREIGN KEY (discovery_run_id) REFERENCES discovery.discovery_runs(id) ON DELETE CASCADE,
    CONSTRAINT fk_discovery_metrics_tenant FOREIGN KEY (tenant_id) REFERENCES core.tenants(id) ON DELETE RESTRICT
);

CREATE INDEX idx_discovery_metrics_discovery_run_id ON discovery.discovery_metrics(discovery_run_id);
CREATE INDEX idx_discovery_metrics_tenant_id ON discovery.discovery_metrics(tenant_id);
CREATE INDEX idx_discovery_metrics_metric_key ON discovery.discovery_metrics(metric_key);
CREATE INDEX idx_discovery_metrics_run_key ON discovery.discovery_metrics(discovery_run_id, metric_key);

-- Discovery Warnings Table
CREATE TABLE IF NOT EXISTS discovery.discovery_warnings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    discovery_run_id UUID NOT NULL,
    tenant_id INTEGER NOT NULL,
    discovery_item_id UUID,
    severity INTEGER NOT NULL,
    category VARCHAR(100) NOT NULL,
    code VARCHAR(100) NOT NULL,
    message VARCHAR(1000) NOT NULL,
    detailed_message VARCHAR(4000),
    item_path VARCHAR(2000),
    recommendation_json JSONB,
    detected_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT fk_discovery_warnings_run FOREIGN KEY (discovery_run_id) REFERENCES discovery.discovery_runs(id) ON DELETE CASCADE,
    CONSTRAINT fk_discovery_warnings_tenant FOREIGN KEY (tenant_id) REFERENCES core.tenants(id) ON DELETE RESTRICT,
    CONSTRAINT fk_discovery_warnings_item FOREIGN KEY (discovery_item_id) REFERENCES discovery.discovery_items(id) ON DELETE SET NULL
);

CREATE INDEX idx_discovery_warnings_discovery_run_id ON discovery.discovery_warnings(discovery_run_id);
CREATE INDEX idx_discovery_warnings_tenant_id ON discovery.discovery_warnings(tenant_id);
CREATE INDEX idx_discovery_warnings_discovery_item_id ON discovery.discovery_warnings(discovery_item_id);
CREATE INDEX idx_discovery_warnings_severity ON discovery.discovery_warnings(severity);
CREATE INDEX idx_discovery_warnings_run_severity ON discovery.discovery_warnings(discovery_run_id, severity);

-- Discovery Exports Table
CREATE TABLE IF NOT EXISTS discovery.discovery_exports (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    discovery_run_id UUID NOT NULL,
    tenant_id INTEGER NOT NULL,
    format INTEGER NOT NULL,
    export_type INTEGER NOT NULL,
    file_name VARCHAR(500) NOT NULL,
    file_path VARCHAR(2000),
    file_size_bytes BIGINT NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(200) NOT NULL,
    expires_at TIMESTAMPTZ,
    is_downloaded BOOLEAN NOT NULL DEFAULT false,
    download_count INTEGER NOT NULL DEFAULT 0,
    
    CONSTRAINT fk_discovery_exports_run FOREIGN KEY (discovery_run_id) REFERENCES discovery.discovery_runs(id) ON DELETE CASCADE,
    CONSTRAINT fk_discovery_exports_tenant FOREIGN KEY (tenant_id) REFERENCES core.tenants(id) ON DELETE RESTRICT
);

CREATE INDEX idx_discovery_exports_discovery_run_id ON discovery.discovery_exports(discovery_run_id);
CREATE INDEX idx_discovery_exports_tenant_id ON discovery.discovery_exports(tenant_id);
CREATE INDEX idx_discovery_exports_created_at ON discovery.discovery_exports(created_at);
CREATE INDEX idx_discovery_exports_expires_at ON discovery.discovery_exports(expires_at);

-- Grant permissions (adjust as needed)
GRANT USAGE ON SCHEMA discovery TO cdp_user;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA discovery TO cdp_user;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA discovery TO cdp_user;

-- Success message
SELECT 'Discovery schema created successfully!' AS status;
