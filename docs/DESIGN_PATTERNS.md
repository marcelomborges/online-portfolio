# Design Patterns

Reference for every design pattern adopted in this project — what each one is, why it was chosen, and how to apply it correctly.

**Related:** [ARCHITECTURE.md](./ARCHITECTURE.md) · [BACKLOG.md](./BACKLOG.md)

---

## Table of contents

1. [Layered Architecture — MVC + Service + Repository](#1-layered-architecture--mvc--service--repository)
2. [Repository Pattern](#2-repository-pattern)
3. [Unit of Work](#3-unit-of-work)
4. [Options Pattern](#4-options-pattern)
5. [Result Pattern](#5-result-pattern)
6. [Extension Methods for DI Registration](#6-extension-methods-for-di-registration)
7. [Idempotent Seeder](#7-idempotent-seeder)
8. [Global Query Filters — Multi-tenant Isolation](#8-global-query-filters--multi-tenant-isolation)
9. [BFF (Backend for Frontend)](#9-bff-backend-for-frontend)
10. [Multi-tenant Row Isolation](#10-multi-tenant-row-isolation)

---

## 1. Layered Architecture — MVC + Service + Repository

### What it is

A three-layer structure where each layer has a single, well-defined responsibility and can only call the layer immediately below it.

```
HTTP request
    ↓
Controller        — receives, validates, delegates
    ↓
Service           — business rules, authorization
    ↓
Repository        — database access
    ↓
Database
```

### Why it was adopted

Without clear boundaries, business rules end up scattered across controllers, query logic leaks into controllers, and testing requires spinning up the full HTTP stack. The layered structure enforces:

- **Testability:** services and repositories can be tested independently with mocks
- **Replaceability:** the data source (Postgres today, anything tomorrow) can change without touching business logic
- **Predictability:** a developer reading a controller knows it never touches the DB directly; a developer reading a service knows it never parses HTTP

### Rules

| Layer | Folder | What it does | What it must NOT do |
|---|---|---|---|
| **Controller** | `Controllers/` | Parse HTTP request, call one service method, return HTTP response | Contain business rules, call repositories, call `DbContext` |
| **Service** | `Services/` | Business rules, authorization, orchestration across multiple repositories | Parse HTTP, build `IActionResult`, call `DbContext` directly |
| **Repository** | `Data/Repositories/` | Query + manipulate the EF change tracker | Business rules, call `SaveChanges`, depend on other repositories |

**Never skip a layer.** Controllers never call repositories. Services never call `DbContext` directly.

---

## 2. Repository Pattern

### What it is

Every database operation is encapsulated in a repository class that owns the EF Core `DbSet` access for a specific entity or aggregate. The rest of the application (services) depends on the repository interface, not on EF Core.

### Why it was adopted

- **Testable services:** unit tests for services mock `IUserRepository` instead of spinning up a real database
- **No EF leakage:** LINQ expressions and `DbContext` APIs stay inside the `Data/` folder; a service method reads like business logic, not a query
- **Single place to change:** if an entity query needs a new index hint or a change in eager-loading, it changes in one file

### How to apply

```
Data/
  Repositories/
    IUserRepository.cs           — interface used by services
    UserRepository.cs            — implementation with ApplicationDbContext
```

```csharp
// Interface (in Data/Repositories/)
public interface IUserRepository
{
    Task<ApplicationUser?> FindByEmailAsync(string email, CancellationToken ct = default);
    void Add(ApplicationUser user);
}

// Implementation
public sealed class UserRepository(ApplicationDbContext db) : IUserRepository
{
    public Task<ApplicationUser?> FindByEmailAsync(string email, CancellationToken ct = default)
        => db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public void Add(ApplicationUser user) => db.Users.Add(user);
    // Note: no SaveChanges here — that is the Unit of Work's job
}
```

Register: `services.AddScoped<IUserRepository, UserRepository>();`

---

## 3. Unit of Work

### What it is

EF Core's `DbContext` already buffers all changes in memory and writes them in a single `SaveChanges` call — it is implicitly a Unit of Work. This pattern makes that boundary explicit via `IUnitOfWork`, so services can commit changes without depending on `DbContext` directly.

### Why it was adopted

- **Services stay clean:** a service that injects `IUnitOfWork` clearly signals "I commit transactions" without importing EF Core
- **Explicit transaction boundary:** a service operation that touches multiple repositories commits exactly once at the end, never mid-operation
- **Testable without EF:** in unit tests, `IUnitOfWork` is mocked; tests verify it was called, not that SQL was issued

### How to apply

```csharp
// Data/IUnitOfWork.cs
public interface IUnitOfWork
{
    Task<int> CommitAsync(CancellationToken ct = default);
}

// ApplicationDbContext implements it
public class ApplicationDbContext(...) : IdentityDbContext<...>, IUnitOfWork
{
    public Task<int> CommitAsync(CancellationToken ct = default) => SaveChangesAsync(ct);
}
```

Services inject both a repository (for data access) and `IUnitOfWork` (for committing):

```csharp
public sealed class AuthService(IUserRepository users, IUnitOfWork uow)
{
    public async Task<Result<string>> RegisterAsync(...)
    {
        users.Add(newUser);
        await uow.CommitAsync(ct);   // ← only here, never in the repository
        return Result<string>.Ok(token);
    }
}
```

**Rule:** `CommitAsync` is called only in service methods — never in repositories, controllers, or seeders that go through services.

---

## 4. Options Pattern

### What it is

Every configuration block in `appsettings.json` has a corresponding typed class in `Options/`. Configuration values are never read as raw strings (`configuration["Jwt:Secret"]`) inside services or controllers.

### Why it was adopted

- **No magic strings:** raw `configuration["Key"]` calls are error-prone and invisible to refactoring tools
- **Early validation:** a missing required value throws at startup, not at the first request that needs it
- **Testability:** tests instantiate options objects directly, no need to build `IConfiguration`
- **Discoverability:** a developer seeing `JwtOptions` knows exactly what the `Jwt` section configures

### How to apply

```
Options/
  JwtOptions.cs       — Jwt:Secret, Issuer, Audience, ExpirationMinutes
  DevSeedOptions.cs   — DevSeed:PlatformAdminEmail, PlatformAdminPassword
```

```csharp
// Options/JwtOptions.cs
public sealed class JwtOptions
{
    public const string Section = "Jwt";

    public string Secret { get; init; } = string.Empty;
    public string Issuer { get; init; } = "OnlinePortfolio.Api";
    public string Audience { get; init; } = "OnlinePortfolio.Admin";
    public int ExpirationMinutes { get; init; } = 60;
}

// Registration (Program.cs or an extension)
services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.Section));

// Injection
public sealed class AuthService(IOptions<JwtOptions> jwtOptions) { ... }
```

**Rule:** every new config block gets a class in `Options/` before it is read anywhere. No `configuration["..."]` in services or controllers.

---

## 5. Result Pattern

### What it is

Services return `Result<T>` (success + value) or `Result` (success only) instead of throwing exceptions for expected business failures. `Result<T>` is a discriminated union: it holds either a value (success) or an `Error` (failure), never both.

### Why it was adopted

This API has many expected failure paths — login with wrong password, inviting a duplicate email, accessing a resource the user doesn't own. Throwing exceptions for these:
- Makes control flow non-obvious (reader must know which exceptions can escape which methods)
- Forces `try/catch` blocks in controllers, which is verbose and brittle
- Conflates expected business failures with unexpected runtime errors

With `Result<T>`, every possible outcome is visible in the method signature, and controllers handle it in one `.Match()` call.

### How to apply

**Source:** `Common/Result.cs`

```csharp
// Service returns Result<T>
public async Task<Result<TokenDto>> LoginAsync(LoginRequest req, CancellationToken ct)
{
    var user = await _users.FindByEmailAsync(req.Email, ct);
    if (user is null)
        return Result<TokenDto>.Fail(Error.Validation("Invalid email or password."));

    if (!user.IsActive)
        return Result<TokenDto>.Fail(Error.Forbidden());

    var ok = await _signInManager.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: true);
    if (!ok.Succeeded)
        return Result<TokenDto>.Fail(Error.Validation("Invalid email or password."));

    return Result<TokenDto>.Ok(new TokenDto(...));
}

// Controller maps Result<T> to HTTP
public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
{
    var result = await _auth.LoginAsync(req, ct);
    return result.Match(
        onSuccess: dto => Ok(dto),
        onFailure: err => err.Code switch
        {
            "Auth.Forbidden"     => Forbid(),
            "Validation.Failed"  => Unauthorized(),
            _                    => BadRequest(err.Message),
        });
}
```

**Error factory methods** (defined in `Common/Result.cs`):

| Method | Code | Use case |
|---|---|---|
| `Error.NotFound(resource)` | `{resource}.NotFound` | Entity not found |
| `Error.Conflict(resource)` | `{resource}.Conflict` | Duplicate / already exists |
| `Error.Unauthorized()` | `Auth.Unauthorized` | Not authenticated |
| `Error.Forbidden()` | `Auth.Forbidden` | Authenticated but not allowed |
| `Error.Validation(message)` | `Validation.Failed` | Input/business rule violation |
| `new Error(code, message)` | custom | Specific errors (e.g. `Auth.InvalidCredentials`) |

**Rule:** exceptions are for unexpected runtime failures only. Expected business failures use `Result<T>`. The `GlobalExceptionHandler` handles anything that escapes as an exception.

---

## 6. Extension Methods for DI Registration

### What it is

Each logical group of services is registered through a dedicated `IServiceCollection` extension method, not inline in `Program.cs`.

### Why it was adopted

`Program.cs` with dozens of inline `services.Add*(...)` calls is hard to read and test in isolation. Extension methods:
- Group related registrations (`AddApplicationJwt` owns everything JWT-related)
- Can be called from unit tests to verify the registration itself (e.g. `JwtConfigTests`)
- Make `Program.cs` read like a table of contents, not a configuration dump

### How to apply

```
Data/
  JwtServiceCollectionExtensions.cs    → AddApplicationJwt(configuration)
  IdentityServiceCollectionExtensions.cs → AddApplicationIdentity()
  DatabaseServiceCollectionExtensions.cs → AddApplicationDatabase(configuration)
```

```csharp
// Program.cs stays clean
builder.Services.AddApplicationDatabase(builder.Configuration);
builder.Services.AddApplicationIdentity();
builder.Services.AddApplicationJwt(builder.Configuration);
```

**Rule:** every new group of related services (e.g. email, storage) gets its own extension class before it touches `Program.cs`.

---

## 7. Idempotent Seeder

### What it is

Seeder classes that can be run any number of times without creating duplicates or failing. They check whether data already exists before inserting.

### Why it was adopted

In development, `docker compose down -v` destroys the database. The API must re-seed on the next startup without manual intervention and without duplicate-key errors. The same pattern applies to Identity roles, which must exist in production before any user can be assigned a role.

### How to apply

```csharp
// Check before insert — always
if (!await db.Plans.AnyAsync(ct))
{
    db.Plans.Add(new Plan { Id = StarterPlanId, ... });
    await db.SaveChangesAsync(ct);
}

// Use fixed GUIDs in dev data so every reset produces the same rows
internal static readonly Guid StarterPlanId = new("00000000-0000-0000-0000-000000000001");
```

**Seeders in this project:**

| Class | When it runs | What it seeds |
|---|---|---|
| `IdentityRoleSeeder` | Every startup (prod + dev) | `PlatformAdmin`, `Owner`, `Editor` roles |
| `DevDataSeeder` | Development only (guarded by `IsDevelopment()`) | Starter plan, tenants `ana`/`joao`, PlatformAdmin user |

**Rule:** seeders are always idempotent. Fixed GUIDs are used for dev data to guarantee consistent IDs across resets.

---

## 8. Global Query Filters — Multi-tenant Isolation

### What it is

EF Core's `HasQueryFilter` applies a `WHERE` clause automatically to every query on an entity, without the developer needing to add it explicitly. Used here to enforce tenant isolation at the ORM level.

### Why it was adopted

Multi-tenant row isolation must be applied on 100% of queries. A single `WHERE tenant_id = ?` omission causes a data leak between tenants. Relying on developers to add it manually on every query is a category of bug waiting to happen.

With a global query filter registered on the entity, the filter is impossible to forget — it is applied by default and must be explicitly disabled (`.IgnoreQueryFilters()`) when genuinely needed (e.g. admin cross-tenant queries).

### How it works

```csharp
// In entity configuration (e.g. TenantConfiguration.cs)
builder.HasQueryFilter(t => t.IsActive);

// Tenant-scoped entities (future, once TenantContext middleware exists)
builder.HasQueryFilter(a => a.TenantId == tenantContext.TenantId);
```

The `TenantContext` is a scoped service populated by the request middleware from the authenticated user's JWT — never from the request body or query string.

**Rule:** every entity that carries a `TenantId` must have a global query filter on it. No cross-tenant queries without `.IgnoreQueryFilters()` and a `[Authorize(Roles = "PlatformAdmin")]` guard.

---

## 9. BFF (Backend for Frontend)

### What it is

The browser never calls the ASP.NET API on Render directly. All API traffic from the browser goes through Nuxt server routes (`/api/**`), which act as a proxy to the real API. This Nuxt server layer is the Backend for Frontend.

### Why it was adopted

- **Secrets stay server-side:** the real API URL (`api.onlineportfolio.com.br`) and the JWT are never exposed to the browser
- **Single auth surface:** cookies and tokens are managed by the Nuxt server, not client-side JavaScript
- **CORS eliminated:** the browser only talks to its own origin; CORS is a concern between Nuxt and Render, not between the browser and Render
- **Centralized proxy logic:** auth headers, error normalization, and response shaping happen in one place

### How it works

```
Browser
  → GET {any-host}/api/v1/auth/me        (Nuxt server route)
  → GET api.onlineportfolio.com.br/api/v1/auth/me  (Render, server-to-server)
```

```
frontend/server/api/         — Nuxt server routes (the BFF layer)
  auth/
    login.post.ts
    me.get.ts
```

**Rule:** no `fetch('https://api.onlineportfolio.com.br/...')` in any `.vue` file or client-side composable. All calls go through `/api/**` on the Nuxt server. The only exception is Supabase Storage CDN URLs in public `<img>` tags (read-only).

---

## 10. Multi-tenant Row Isolation

### What it is

All tenants share a single PostgreSQL database. Tenant data is separated by a `TenantId` UUID column on every tenant-scoped entity, enforced at three independent layers.

### Why it was adopted

A single multi-tenant database is simpler to operate and cheaper than per-tenant databases. The risk of cross-tenant data leakage is mitigated by three independent enforcement layers — a failure in one does not expose data if the others hold.

### Three enforcement layers

| Layer | Mechanism | Where |
|---|---|---|
| **ORM** | EF Core global query filters — automatic `WHERE tenant_id = ?` on every query | Entity configurations in `Data/Configurations/` |
| **API middleware** | Every protected request resolves `TenantId` from the JWT user row and sets `TenantContext`; membership is verified before the controller executes | `Middleware/TenantResolutionMiddleware.cs` (DEV-155+) |
| **Code check** | Before returning or mutating any resource, the service verifies `resource.TenantId == currentUser.TenantId` | Every service method that touches tenant data |

### Rules

- `TenantId` is always resolved from the authenticated user's row in the database, **never** from the request body, query string, or URL parameter
- `PlatformAdmin` users have `TenantId = null` and can query across tenants using `.IgnoreQueryFilters()` — this must always be paired with `[Authorize(Roles = "PlatformAdmin")]`
- No direct database access from the frontend (no Supabase JS client for data)
- Supabase Storage paths follow `tenants/{tenantId}/...`; paths are generated server-side
