# Database Schema — PostgreSQL (Multi-tenant)

PostgreSQL schema for the Online Portfolio platform. Managed via **EF Core migrations** in the .NET API against **Supabase Postgres** (prod) or Compose Postgres (local).

**Related:** [docs/ARCHITECTURE.md](./ARCHITECTURE.md) · [BACKLOG.md](./BACKLOG.md)

**Escopo deste documento:**

| In scope (inicial) | Fora do escopo (migrations futuras) |
|---|---|
| Multi-tenant core (`tenants`, `plans`) | `artworks`, `artwork_images`, `categories` |
| Usuários admin + roles (`users`) | Metadados de Storage, mensagens de contato |
| **Login + add user (convite PlatformAdmin)** | Convites self-service do Owner (v2) |
| Stub mínimo `tenant_settings` | CMS completo, exposições, billing |

Credenciais via **ASP.NET Identity completo** (mesmo Postgres, migrations EF). `ApplicationUser` + roles em `AspNetRoles` / `AspNetUserRoles` — ver [docs/ARCHITECTURE.md](./ARCHITECTURE.md).

**Domínios (referência):** site público por tenant em `{slug}.onlineportfolio.com.br`; admin centralizado em `app.onlineportfolio.com.br` — ver [docs/ARCHITECTURE.md](./ARCHITECTURE.md).

---

## Table of contents

1. [Design principles](#1-design-principles)
   - [Primary keys & identifiers](#primary-keys--identifiers)
2. [Auth vs application data](#2-auth-vs-application-data)
3. [Entity relationship diagram](#3-entity-relationship-diagram)
4. [Tables (initial migration)](#4-tables-initial-migration)
5. [Enums and constraints](#5-enums-and-constraints)
6. [Indexes](#6-indexes)
7. [EF Core mapping notes](#7-ef-core-mapping-notes)
8. [Seed data (dev)](#8-seed-data-dev)
9. [Login & add user flows](#9-login--add-user-flows)
10. [Future tables (reference)](#10-future-tables-reference)
11. [Migration order](#11-migration-order)

---

## 1. Design principles

| Principle | Implementation |
|---|---|
| **Shared schema** | One database, all tenants |
| **Row isolation** | `tenant_id` on every tenant-owned row |
| **API-only access** | No direct browser → Postgres; EF in .NET API |
| **Auth** | **ASP.NET Identity completo** — `ApplicationUser` + `AspNetRoles` + JWT |
| **Membership in DB** | `users`: `tenant_id` + `role` por conta |
| **Slug uniqueness** | Global unique on `tenants.slug`; tenant-scoped slugs later on content tables |
| **Primary keys** | **`uuid` / `Guid`** on all domain entities — see [Primary keys & identifiers](#primary-keys--identifiers) |

### Primary keys & identifiers

**Status:** Decided for v1 (SaaS multi-tenant + ASP.NET Identity `Guid`).

| Topic | Decision |
|---|---|
| **PK type** | Postgres `uuid` (`gen_random_uuid()`), EF `Guid` — **not** `serial` / `bigint` for domain tables |
| **Storage** | Native Postgres type `uuid` (16 bytes) — **not** `varchar` |
| **API JSON** | `"id": "550e8400-e29b-41d4-a716-446655440000"` (string with hyphens) |
| **Public URLs** | **`slug`** for tenant/site routing (`{slug}.onlineportfolio.com.br`); resource IDs in admin API as uuid |
| **Foreign keys** | Same `uuid` references parent PK (`tenant_id → tenants.id`, `user_id → users.id`, …) — **expected** in many tables |

#### Why uuid (not serial)

- Aligns with **`IdentityUser<Guid>`** — no mixed PK types across FK graph.
- **Non-guessable** IDs in admin API (reduces enumeration / IDOR risk vs sequential integers).
- Safe for **distributed generation** (seeds, invites, Storage paths) without central sequences.
- Standard for **Postgres SaaS** (Supabase, B2B products); performance cost is acceptable at this product’s scale.

#### FKs in multiple tables

Repeating `tenant_id` (or other FKs) across tenant-owned rows is **normal relational design**, not duplication of entity data — each row stores a **reference** (16 bytes), not a copy of the parent row. The same pattern would apply with `bigint` PKs.

#### Performance

- Random uuid (v4) indexes are slightly larger than `bigint`; inserts can fragment B-tree pages at **very high** write volume.
- For expected tenant/artwork scale, this is **not** a v1 bottleneck.
- **Indexes that matter more:** `(tenant_id)`, `(tenant_id, slug) UNIQUE`, FK columns indexed for joins.
- **Future (only if metrics prove need):** UUID v7 (time-ordered) or `bigint` PK + `public_id uuid` on a specific hot table — not the default for the whole schema.

#### Do not

- Expose **sequential integer** PKs on public or multi-tenant admin APIs.
- Use one “global uuid” as PK for unrelated entity types — each table has **its own** PK; FKs point to the correct parent.
- Store uuid as untyped string in Postgres when `uuid` type exists.

**Cross-ref:** [docs/ARCHITECTURE.md](./ARCHITECTURE.md) · Identity `Guid` in [docs/ARCHITECTURE.md](./ARCHITECTURE.md)

---

## 2. Auth vs application data

```text
┌─────────────────────────┐         ┌──────────────────────────────┐
│  ASP.NET Identity       │         │  Application DB (EF)         │
│  (same Postgres)        │         │                              │
│  ─────────────────      │         │  tenants                     │
│  users.id (uuid)        │◄────────│  users (Identity + profile)  │
│  users.email, password_hash │         │  users.tenant_id → tenants   │
│  AspNetRoles / UserRoles    │         │  (role via Identity)         │
└─────────────────────────┘         └──────────────────────────────┘
         ▲                                       ▲
         │ POST /auth/login                      │ GET /auth/me
         │ (via Nuxt proxy)                      │ JWT validated in API
    Nuxt admin (app.onlineportfolio.com.br)      │
                                                 API (.NET)
```

| Concern | Where |
|---|---|
| Email + senha | ASP.NET Identity (`users` / `AspNetUsers`) |
| “Qual tenant é este usuário?” | `users.tenant_id` |
| “Qual role?” | `AspNetUserRoles` → `AspNetRoles` (PlatformAdmin, Owner, Editor) |
| “Pode acessar o admin?” | Role Identity + `tenants.is_active` + `users.is_active` |
| “Quem adiciona usuários ao tenant?” | **PlatformAdmin** via API de convite — v1 |
| JWT | Emitido e validado pela **API** (`Jwt__Secret`) |

### User types (summary)

```text
PlatformAdmin (você)
  tenant_id = NULL, role AspNetRoles = PlatformAdmin
  → gerencia todos os tenants
  → convida / lista / desativa usuários em qualquer tenant

Usuário do tenant (artista/equipe)
  tenant_id = {uuid}
  role = Owner | Editor   (AspNetUserRoles)
  → login em app.onlineportfolio.com.br (URL única)
  → admin só do seu tenant (conteúdo vem depois; login MVP primeiro)
```

Each tenant has **at least one** tenant user (typically `Owner`). Multiple users per tenant are allowed (e.g. Owner + Editors).

---

## 3. Entity relationship diagram

### Initial scope (login + multi-tenant)

```mermaid
erDiagram
    plans ||--o{ tenants : "has"
    tenants ||--o| tenant_settings : "has"
    tenants ||--o{ users : "has"
    plans {
        uuid id PK
        varchar name
        int max_storage_mb
        int max_artworks
        bool custom_domain_allowed
        timestamptz created_at
    }
    tenants {
        uuid id PK
        uuid plan_id FK
        varchar slug UK
        varchar display_name
        varchar custom_domain UK "nullable"
        timestamptz custom_domain_verified_at "nullable"
        bool is_active
        timestamptz created_at
        timestamptz updated_at
    }
    tenant_settings {
        uuid tenant_id PK_FK
        varchar contact_email "nullable"
        text bio "nullable"
        timestamptz updated_at
    }
    users {
        uuid id PK
        varchar email UK
        uuid tenant_id FK "nullable for PlatformAdmin"
        bool is_active
        bool email_confirmed
        timestamptz created_at
        timestamptz last_login_at "nullable"
        uuid invited_by_user_id FK "nullable"
    }
    AspNetRoles {
        uuid id PK
        varchar name UK
    }
    AspNetUserRoles {
        uuid user_id FK
        uuid role_id FK
    }
    AspNetRoles ||--o{ AspNetUserRoles : "has"
    users ||--o{ AspNetUserRoles : "has"
```

### Deferred (not in first migration)

```text
artworks, artwork_images, categories  →  Phase 1 public gallery / Phase 3 uploads
subscriptions, invoices               →  Phase 4 billing (optional — skip if not charging)
audit_logs                            →  optional later
```

---

## 4. Tables (initial migration)

Naming: PostgreSQL **snake_case** table/column names (Npgsql default with EF naming convention or explicit configuration).

### 4.1 `plans`

Platform subscription tiers. Minimal seed for FK integrity; limits enforced in later phases.

| Column | Type | Nullable | Description |
|---|---|---|---|
| `id` | `uuid` | NO | PK, `gen_random_uuid()` |
| `name` | `varchar(100)` | NO | e.g. `Starter`, `Pro` |
| `max_storage_mb` | `int` | NO | Default limit (future) |
| `max_artworks` | `int` | NO | Default limit (future) |
| `custom_domain_allowed` | `boolean` | NO | Default `false` |
| `created_at` | `timestamptz` | NO | UTC default `now()` |

---

### 4.2 `tenants`

One row per artist / studio on the platform.

| Column | Type | Nullable | Description |
|---|---|---|---|
| `id` | `uuid` | NO | PK |
| `plan_id` | `uuid` | NO | FK → `plans.id` |
| `slug` | `varchar(63)` | NO | Subdomain: `{slug}.onlineportfolio.com.br` |
| `display_name` | `varchar(200)` | NO | Public name |
| `custom_domain` | `varchar(253)` | YES | e.g. `ana-art.com.br` |
| `custom_domain_verified_at` | `timestamptz` | YES | Set when DNS verified |
| `is_active` | `boolean` | NO | Default `true`; inactive → 404 public |
| `created_at` | `timestamptz` | NO | UTC |
| `updated_at` | `timestamptz` | NO | UTC |

**Slug rules:** lowercase alphanumeric + hyphen; 3–63 chars; unique globally.

---

### 4.3 `tenant_settings`

One-to-one with tenant. Stub for login MVP — extended when gallery/settings UI is built.

| Column | Type | Nullable | Description |
|---|---|---|---|
| `tenant_id` | `uuid` | NO | PK + FK → `tenants.id` ON DELETE CASCADE |
| `contact_email` | `varchar(320)` | YES | Contact form destination (later) |
| `bio` | `text` | YES | Artist bio (later) |
| `updated_at` | `timestamptz` | NO | UTC |

---

### 4.4 `AspNetUsers` / `ApplicationUser`

Perfil + credenciais — tabela gerada pelo **Identity completo** (`AspNetUsers` no Postgres; documentada aqui como `users` por clareza).

| Column | Type | Nullable | Description |
|---|---|---|---|
| `id` | `uuid` | NO | PK; Identity user id |
| `email` | `varchar(320)` | NO | Login; unique globally |
| `password_hash` | `text` | NO | Identity-managed (nullable só durante convite pendente) |
| `tenant_id` | `uuid` | YES | FK → `tenants.id`; NULL só para `PlatformAdmin` |
| `is_active` | `boolean` | NO | Default `true`; desativar sem apagar |
| `email_confirmed` | `boolean` | NO | False até accept-invite |
| `created_at` | `timestamptz` | NO | UTC |
| `last_login_at` | `timestamptz` | YES | Atualizado no login ou `/auth/me` |
| `invited_by_user_id` | `uuid` | YES | FK → `users.id`; PlatformAdmin que convidou |

Mais colunas padrão Identity: `NormalizedEmail`, `SecurityStamp`, `ConcurrencyStamp`, `LockoutEnd`, etc.

**Role:** via `AspNetUserRoles` — **não** coluna `role` em `users`.

### 4.5 Identity roles (AspNetRoles)

Seed na migration:

| `name` | `tenant_id` do usuário |
|---|---|
| `PlatformAdmin` | NULL |
| `Owner` | obrigatório |
| `Editor` | obrigatório |

Um usuário = **uma role** no v1 (`UserManager.AddToRoleAsync`).

**Rules:**
- **Multiple users per tenant** allowed (`Owner`, `Editor`).
- **PlatformAdmin** has `tenant_id = NULL` — not tied to a single tenant.
- **One email = one account** globally (unique on `email`).
- **v1:** only **PlatformAdmin** creates invites for tenant users.

---

## 5. Enums and constraints

### Roles (AspNetRoles)

| Value | `tenant_id` | Quem | v1 |
|---|---|---|---|
| `PlatformAdmin` | NULL | Você (operador) | Convida/gerencia usuários em qualquer tenant |
| `Owner` | Obrigatório | Artista / admin do studio | Admin do tenant |
| `Editor` | Obrigatório | Assistente | Mesmo fluxo de convite que Owner |

### Who can manage tenant users

| Action | PlatformAdmin | Owner | Editor |
|---|---|---|---|
| Invite user to tenant | Yes (any tenant) | No (v2) | No |
| List tenant users | Yes (any tenant) | No (v2: own tenant) | No |
| Deactivate tenant user | Yes | No (v2) | No |
| Change user role | Yes | No (v2) | No |

### Check constraints

```sql
-- Tenant slug format (optional at DB level; validate in API too)
ALTER TABLE tenants ADD CONSTRAINT chk_tenants_slug_format
  CHECK (slug ~ '^[a-z0-9]([a-z0-9-]{1,61}[a-z0-9])?$');

-- Role ↔ tenant_id: validado na API (PlatformAdmin => tenant_id NULL)
-- AspNetUserRoles + regra de negócio no invite/login
```

### Foreign keys

```text
tenants.plan_id        → plans.id
tenant_settings.tenant_id → tenants.id  (CASCADE delete)
users.tenant_id        → tenants.id     (RESTRICT delete)
users.invited_by_user_id → users.id     (SET NULL on delete; optional)
```

---

## 6. Indexes

| Table | Index | Type | Purpose |
|---|---|---|---|
| `tenants` | `slug` | UNIQUE | Subdomain resolution |
| `tenants` | `custom_domain` | UNIQUE (partial) | `WHERE custom_domain IS NOT NULL` |
| `tenants` | `is_active` | BTREE | Filter active tenants |
| `users` | `email` | UNIQUE | Login lookup |
| `users` | `tenant_id` | BTREE | List users per tenant |
| `users` | `(tenant_id, email)` | UNIQUE | One membership row per email per tenant |

Note: email is globally unique — the same person cannot be admin of two tenants with one login in v1.

---

## 7. EF Core mapping notes

### Identity completo

```csharp
public class ApplicationUser : IdentityUser<Guid>
{
    public Guid? TenantId { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? InvitedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
}

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public DbSet<Plan> Plans { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantSettings> TenantSettings { get; set; }
}
```

| Regra | Detalhe |
|---|---|
| **DI** | `AddIdentity<ApplicationUser, IdentityRole<Guid>>()` + `AddDefaultTokenProviders()` |
| **Roles** | Seed `PlatformAdmin`, `Owner`, `Editor` via `RoleManager` |
| **Invite** | `UserManager.CreateAsync` + `AddToRoleAsync(role)` |
| **Authorize** | `[Authorize(Roles = "PlatformAdmin")]` + tenant check no service |
| **JWT** | Claims incluem roles de `UserManager.GetRolesAsync` |

Migration EF gera todas as tabelas Identity (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, …).

### DbSets (initial)

```csharp
DbSet<Plan> Plans
DbSet<Tenant> Tenants
DbSet<TenantSettings> TenantSettings
DbSet<ApplicationUser> Users   // AspNetUsers + perfil tenant
```

### Conventions

| Topic | Approach |
|---|---|
| **PK** | `Guid` / `uuid` — see [docs/DATABASE.md](./DATABASE.md) |
| **Timestamps** | `DateTimeOffset` UTC |
| **Soft delete** | Not used v1; use `is_active` |
| **Global filters** | `User` filtered by `tenant_id` when in tenant context; **no filter** on `User` for platform admin queries |
| **Naming** | `UseSnakeCaseNamingConvention()` (EFCore.NamingConventions) |

### Global query filters (when tenant content exists)

For login MVP, filters on `User` are applied in **service layer** (admin may be platform-scoped). When `artworks` arrive:

```csharp
modelBuilder.Entity<Artwork>()
    .HasQueryFilter(a => a.TenantId == _tenantContext.TenantId);
```

Do **not** apply a blind global filter on `users` that hides `PlatformAdmin` rows.

---

## 8. Seed data (dev)

Minimum seed for local Docker and integration tests:

```text
plans:
  - id: fixed uuid, name: Starter, max_storage_mb: 500, max_artworks: 50

tenants:
  - slug: ana,   display_name: Ana Silva Studio,   plan: Starter
  - slug: joao,  display_name: João Sculptures,     plan: Starter

tenant_settings:
  - one row per tenant (empty bio/contact ok)

users:
  - PlatformAdmin: seeded with password hash (dev only)
  - Tenant users: created by invite API with email_confirmed=false until accept-invite

**Platform admin (você):** seed de `users` (`PlatformAdmin`, `tenant_id` NULL) + senha.

**Usuários do tenant (por artista):** API de convite do PlatformAdmin → Resend → accept-invite → login.

```text
1. PlatformAdmin → POST .../tenants/{tenantId}/users/invite { email, role }
2. API → INSERT user (pending) + `AddToRoleAsync(role)` + Resend com link de convite
3. Artist opens app.onlineportfolio.com.br/accept-invite?token=...
4. POST /auth/accept-invite { token, password } → email_confirmed=true
5. POST /auth/login → JWT + redirect to /admin
```

Repeat step 1 for each additional user on the same tenant (e.g. second Owner or Editor).

---

## 9. Login & add user flows

Admin MVP tem **duas funções**: autenticar usuários e permitir que o PlatformAdmin **adicione usuários** aos tenants. Sem auto-cadastro.

**URL de login (frontend):** **única** — `app.onlineportfolio.com.br/login`. Não existe login em `{slug}.onlineportfolio.com.br`.

### 9.1 Login flow

```text
1. User opens app.onlineportfolio.com.br/login  (unique URL for all roles)
2. Nuxt form → server route → POST /api/v1/auth/login { email, password }
3. API validates via ASP.NET Identity → issues JWT (httpOnly cookie via proxy)
4. API returns user + tenant summary; updates last_login_at
5. Nuxt redirects:
   - Owner / Editor → /admin (tenant from API response, not from hostname)
   - PlatformAdmin → /platform/tenants
6. Subsequent requests: Nuxt proxy forwards cookie → API validates JWT
```

### 9.2 Add user flow (PlatformAdmin)

```text
1. PlatformAdmin logged in → /platform/tenants/{tenantId}/users
2. Form: email + role (Owner | Editor) → "Add user"
3. Nuxt proxy → POST /api/v1/platform/tenants/{tenantId}/users/invite
4. API → INSERT pending user + Resend invite email (accept-invite link)
5. Invitee sets password via POST /auth/accept-invite
6. Invitee logs in at app.onlineportfolio.com.br/login (seção 9.1)
```

### 9.3 API endpoints (login + add user MVP)

| Method | Path | Auth | Who |
|---|---|---|---|
| `POST` | `/api/v1/auth/login` | Public | Email + password → JWT |
| `POST` | `/api/v1/auth/logout` | JWT | Clear session |
| `POST` | `/api/v1/auth/accept-invite` | Public (token) | Set password after invite |
| `GET` | `/api/v1/auth/me` | JWT | Any admin user |
| `GET` | `/api/v1/platform/tenants` | PlatformAdmin | List tenants |
| `POST` | `/api/v1/platform/tenants` | PlatformAdmin | Create tenant |
| `GET` | `/api/v1/platform/tenants/{tenantId}/users` | PlatformAdmin | List users for tenant |
| `POST` | `/api/v1/platform/tenants/{tenantId}/users/invite` | PlatformAdmin | Invite email + role |
| `PATCH` | `/api/v1/platform/tenants/{tenantId}/users/{userId}` | PlatformAdmin | Deactivate or change role |

Tenant users (`Owner`/`Editor`) **cannot** call platform user routes in v1.

### User creation

| Scenario | Who creates `users` row |
|---|---|
| PlatformAdmin (você) | Seed migration / dev seed com senha |
| Owner / Editor do tenant | API de convite (pendente) → concluído no accept-invite |
| Auto-cadastro | Desabilitado |

```json
{ "tenant_id": "uuid", "role": "Owner" }
```

API still verifies tenant exists and is active before insert.

---

## 10. Future tables (reference)

Not created in the **initial login migration**. Documented for alignment with [docs/ARCHITECTURE.md](./ARCHITECTURE.md).

### `artworks` (Phase 1 — public gallery)

| Column | Notes |
|---|---|
| `id`, `tenant_id`, `title`, `slug`, `description` | `(tenant_id, slug)` unique |
| `is_published`, `published_at`, `sort_order` | Public filter |
| `created_at`, `updated_at` | |

### `artwork_images` (Phase 3)

| Column | Notes |
|---|---|
| `id`, `tenant_id`, `artwork_id`, `storage_path`, `public_url` | |
| `alt_text`, `sort_order`, `width`, `height` | |

### `categories` (optional)

| Column | Notes |
|---|---|
| `id`, `tenant_id`, `name`, `slug`, `sort_order` | `(tenant_id, slug)` unique |

---

## 11. Migration order

| # | Migration name (suggested) | Contents |
|---|---|---|
| 1 | `InitialMultiTenantAndUsers` | `plans`, `tenants`, `tenant_settings`, `users`, indexes, checks |
| 2 | `SeedPlansAndDevTenants` | Dev seed (optional separate or `HasData`) |
| 3 | `AddArtworks` | Phase 1 gallery (later) |
| 4 | `AddArtworkImages` | Phase 3 (later) |

**Production migrations:** applied by GitHub Actions (`dotnet ef database update`) via Supabase **session pooler** (port 5432). Local dev uses **localhost** (Compose). Direct `db.*.supabase.co` is not used in this project. See [docs/ARCHITECTURE.md](./ARCHITECTURE.md) seção 7.

---

## SQL reference (initial migration)

Illustrative DDL — **source of truth is EF migration**, not this snippet.

```sql
CREATE TABLE plans (
    id                      uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name                    varchar(100) NOT NULL,
    max_storage_mb          int NOT NULL DEFAULT 500,
    max_artworks            int NOT NULL DEFAULT 50,
    custom_domain_allowed   boolean NOT NULL DEFAULT false,
    created_at              timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE tenants (
    id                          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    plan_id                     uuid NOT NULL REFERENCES plans(id),
    slug                        varchar(63) NOT NULL,
    display_name                varchar(200) NOT NULL,
    custom_domain               varchar(253),
    custom_domain_verified_at   timestamptz,
    is_active                   boolean NOT NULL DEFAULT true,
    created_at                  timestamptz NOT NULL DEFAULT now(),
    updated_at                  timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_tenants_slug UNIQUE (slug),
    CONSTRAINT uq_tenants_custom_domain UNIQUE (custom_domain)
);

CREATE TABLE tenant_settings (
    tenant_id       uuid PRIMARY KEY REFERENCES tenants(id) ON DELETE CASCADE,
    contact_email   varchar(320),
    bio             text,
    updated_at      timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE users (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id           uuid REFERENCES tenants(id) ON DELETE RESTRICT,
    email               varchar(320) NOT NULL,
    password_hash       text,
    is_active           boolean NOT NULL DEFAULT true,
    email_confirmed     boolean NOT NULL DEFAULT false,
    created_at          timestamptz NOT NULL DEFAULT now(),
    last_login_at       timestamptz,
    invited_by_user_id  uuid REFERENCES users(id) ON DELETE SET NULL,
    CONSTRAINT uq_users_email UNIQUE (email)
);

-- Identity completo: migration EF também cria AspNetRoles, AspNetUserRoles,
-- AspNetUserClaims, AspNetUserTokens, AspNetRoleClaims, …
-- Roles seed: PlatformAdmin, Owner, Editor

CREATE INDEX ix_users_tenant_id ON users(tenant_id);
CREATE INDEX ix_tenants_is_active ON tenants(is_active);
```

---

*Última atualização: 2025-06-21 — Identity completo + JWT; BFF; login centralizado*
