-- Dev bootstrap until EF migrations (DEV-004) own the schema.
-- Auto-runs on first `docker compose up` (empty Postgres volume).
-- Re-seed after wipe: see README Docker Compose section.

CREATE EXTENSION IF NOT EXISTS "pgcrypto";

CREATE TABLE IF NOT EXISTS plans (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name varchar(100) NOT NULL,
    max_storage_mb int NOT NULL,
    max_artworks int NOT NULL,
    custom_domain_allowed boolean NOT NULL DEFAULT false,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS tenants (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    plan_id uuid NOT NULL REFERENCES plans(id),
    slug varchar(63) NOT NULL UNIQUE,
    display_name varchar(200) NOT NULL,
    custom_domain varchar(253) UNIQUE,
    custom_domain_verified_at timestamptz,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT chk_tenants_slug_format CHECK (slug ~ '^[a-z0-9]([a-z0-9-]{1,61}[a-z0-9])?$')
);

CREATE TABLE IF NOT EXISTS tenant_settings (
    tenant_id uuid PRIMARY KEY REFERENCES tenants(id) ON DELETE CASCADE,
    contact_email varchar(320),
    bio text,
    updated_at timestamptz NOT NULL DEFAULT now()
);

INSERT INTO plans (id, name, max_storage_mb, max_artworks, custom_domain_allowed)
VALUES ('11111111-1111-1111-1111-111111111111', 'Starter', 500, 50, false)
ON CONFLICT (id) DO NOTHING;

INSERT INTO tenants (id, plan_id, slug, display_name)
VALUES
    ('22222222-2222-2222-2222-222222222222', '11111111-1111-1111-1111-111111111111', 'ana', 'Ana Silva Studio'),
    ('33333333-3333-3333-3333-333333333333', '11111111-1111-1111-1111-111111111111', 'joao', 'João Sculptures')
ON CONFLICT (id) DO NOTHING;

INSERT INTO tenant_settings (tenant_id)
VALUES
    ('22222222-2222-2222-2222-222222222222'),
    ('33333333-3333-3333-3333-333333333333')
ON CONFLICT (tenant_id) DO NOTHING;
