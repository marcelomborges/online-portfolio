---
applyTo: "backend/**"
---

# Backend (ASP.NET Core)

- ASP.NET Identity **completo**: `AddIdentity<ApplicationUser, IdentityRole<Guid>>()`, `RoleManager`, seed `PlatformAdmin` / `Owner` / `Editor`.
- JWT issued and validated by API (`Jwt__Secret`); not Supabase JWT.
- **Security:** `[Authorize(Roles)]` + tenant membership on every protected action; never trust body `TenantId`.
- EF Core only for Postgres; snake_case columns; migrations via CI (`dotnet ef database update`), not on prod API startup.
- Multi-tenant: global query filters on tenant-scoped entities; validate membership on every write.
- Platform routes: `[Authorize(Roles = "PlatformAdmin")]`.
- Secrets (SendGrid, Supabase service role, JWT) — Render env only.
- **Cross-stack:** API/DTO/auth/env changes usually need matching frontend proxy, types, or pages — see `docs/AGENT_GUIDE.md` § Git flow.
- See `docs/DATABASE.md` and `docs/ARCHITECTURE.md` §9.
