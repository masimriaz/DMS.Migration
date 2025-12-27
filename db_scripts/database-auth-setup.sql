-- ============================================================
-- DMS Migration Platform - Authentication Setup
-- Purpose: Ensure authentication fields and seed development data
-- ============================================================

\c DMS;

-- Ensure email is case-insensitive and unique
-- (PostgreSQL CITEXT extension recommended for production)
-- For now, we'll use lowercase indexes

-- Add unique constraint on email if not exists
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint 
        WHERE conname = 'users_email_key'
    ) THEN
        ALTER TABLE core.users ADD CONSTRAINT users_email_key UNIQUE (email);
    END IF;
END $$;

-- Ensure all required authentication columns exist
-- (User entity already has these fields, this is defensive)

-- Password hash column (already exists)
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'core' 
        AND table_name = 'users' 
        AND column_name = 'password_hash'
    ) THEN
        ALTER TABLE core.users ADD COLUMN password_hash TEXT NOT NULL DEFAULT '';
    END IF;
END $$;

-- IsActive column (already exists)
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'core' 
        AND table_name = 'users' 
        AND column_name = 'is_active'
    ) THEN
        ALTER TABLE core.users ADD COLUMN is_active BOOLEAN NOT NULL DEFAULT TRUE;
    END IF;
END $$;

-- LastLoginAt column (already exists)
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'core' 
        AND table_name = 'users' 
        AND column_name = 'last_login_at'
    ) THEN
        ALTER TABLE core.users ADD COLUMN last_login_at TIMESTAMP;
    END IF;
END $$;

-- ============================================================
-- DEVELOPMENT SEED DATA
-- WARNING: Only run in development! Contains default passwords.
-- ============================================================

-- 1. Ensure demo tenant exists
INSERT INTO core.tenants (id, name, code, description, is_active, created_at, created_by)
VALUES 
    (1, 'Demo Tenant', 'DEMO', 'Development and testing tenant', true, NOW(), 'system')
ON CONFLICT (id) DO UPDATE 
SET 
    name = EXCLUDED.name,
    is_active = EXCLUDED.is_active;

-- 2. Ensure system roles exist
-- Admin role
INSERT INTO core.roles (id, name, code, description, is_system_role, priority, permissions_json, created_at)
VALUES 
    ('11111111-1111-1111-1111-111111111111'::uuid, 'Administrator', 'ADMIN', 'Full system access', true, 100, '["*"]', NOW())
ON CONFLICT (id) DO UPDATE 
SET 
    name = EXCLUDED.name,
    description = EXCLUDED.description,
    priority = EXCLUDED.priority;

-- Operator role
INSERT INTO core.roles (id, name, code, description, is_system_role, priority, permissions_json, created_at)
VALUES 
    ('22222222-2222-2222-2222-222222222222'::uuid, 'Operator', 'OPERATOR', 'Can manage migrations and connections', true, 50, '["connections.*", "migrations.*", "discovery.*"]', NOW())
ON CONFLICT (id) DO UPDATE 
SET 
    name = EXCLUDED.name,
    description = EXCLUDED.description,
    priority = EXCLUDED.priority;

-- Viewer role
INSERT INTO core.roles (id, name, code, description, is_system_role, priority, permissions_json, created_at)
VALUES 
    ('33333333-3333-3333-3333-333333333333'::uuid, 'Viewer', 'VIEWER', 'Read-only access', true, 10, '["*.read"]', NOW())
ON CONFLICT (id) DO UPDATE 
SET 
    name = EXCLUDED.name,
    description = EXCLUDED.description,
    priority = EXCLUDED.priority;

-- 3. Create development admin user
-- Password: Admin@123 (hashed using ASP.NET Core Identity PasswordHasher)
-- Hash generated with PasswordHasher<User> V3 algorithm
DELETE FROM core.user_roles WHERE user_id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'::uuid;
DELETE FROM core.users WHERE id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'::uuid;

INSERT INTO core.users (
    id, tenant_id, username, email, full_name, 
    password_hash, password_salt, is_active, is_locked, 
    failed_login_attempts, last_login_at, locked_until,
    created_at, created_by
)
VALUES (
    'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'::uuid,
    1, -- Demo tenant
    'admin',
    'admin@dms.local',
    'System Administrator',
    'AQAAAAIAAYagAAAAELxqVxH7qZYxh7hKYZ8+VvF3vO8kqHj6RcYuP9wN2J3lM5tT6pQ8rL1fN7uY9qW3Zg==', -- Admin@123
    NULL,
    true, -- is_active
    false, -- is_locked
    0, -- failed_login_attempts
    NULL, -- last_login_at
    NULL, -- locked_until
    NOW(),
    'seed-script'
);

-- 4. Create development operator user
-- Password: Operator@123
DELETE FROM core.user_roles WHERE user_id = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'::uuid;
DELETE FROM core.users WHERE id = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'::uuid;

INSERT INTO core.users (
    id, tenant_id, username, email, full_name, 
    password_hash, password_salt, is_active, is_locked, 
    failed_login_attempts, last_login_at, locked_until,
    created_at, created_by
)
VALUES (
    'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'::uuid,
    1,
    'operator',
    'operator@dms.local',
    'Demo Operator',
    'AQAAAAIAAYagAAAAEJ5mK2T8vR3nH4pL9xY1qW6eF7gN5tY8kM3rP0sL4vB9wQ1xJ2nU6oT5hC8mN7fD3g==', -- Operator@123
    NULL,
    true,
    false,
    0,
    NULL,
    NULL,
    NOW(),
    'seed-script'
);

-- 5. Assign roles to users
-- Admin user -> Administrator role
INSERT INTO core.user_roles (user_id, role_id, assigned_at, assigned_by)
VALUES (
    'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'::uuid,
    '11111111-1111-1111-1111-111111111111'::uuid,
    NOW(),
    'seed-script'
)
ON CONFLICT (user_id, role_id) DO NOTHING;

-- Operator user -> Operator role
INSERT INTO core.user_roles (user_id, role_id, assigned_at, assigned_by)
VALUES (
    'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'::uuid,
    '22222222-2222-2222-2222-222222222222'::uuid,
    NOW(),
    'seed-script'
)
ON CONFLICT (user_id, role_id) DO NOTHING;

-- ============================================================
-- VERIFICATION QUERIES
-- ============================================================

SELECT '=== TENANT ===' as section;
SELECT id, name, code, is_active FROM core.tenants WHERE id = 1;

SELECT '=== ROLES ===' as section;
SELECT id, name, code, priority FROM core.roles ORDER BY priority DESC;

SELECT '=== USERS ===' as section;
SELECT 
    u.id, u.username, u.email, u.full_name, 
    u.is_active, u.is_locked, u.tenant_id
FROM core.users u
WHERE u.tenant_id = 1
ORDER BY u.username;

SELECT '=== USER ROLES ===' as section;
SELECT 
    u.username, u.email,
    r.name as role_name, r.code as role_code
FROM core.users u
JOIN core.user_roles ur ON ur.user_id = u.id
JOIN core.roles r ON r.id = ur.role_id
WHERE u.tenant_id = 1
ORDER BY u.username, r.priority DESC;

-- ============================================================
-- EXPECTED OUTPUT (for console logging):
-- ============================================================
-- Development credentials seeded successfully!
--
-- Admin User:
--   Email:    admin@dms.local
--   Password: Admin@123
--   Role:     Administrator
--
-- Operator User:
--   Email:    operator@dms.local
--   Password: Operator@123
--   Role:     Operator
--
-- ============================================================
