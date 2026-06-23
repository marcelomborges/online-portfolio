---
applyTo: "backend/**"
---

# Backend (ASP.NET Core)

- ASP.NET Identity **completo**: `AddIdentity<ApplicationUser, IdentityRole<Guid>>()`, `RoleManager`, seed `PlatformAdmin` / `Owner` / `Editor`.
- JWT issued and validated by API (`Jwt__Secret`); not Supabase JWT.
- **Security:** `[Authorize(Roles)]` + tenant membership on every protected action; never trust body `TenantId`.
- EF Core only for Postgres; snake_case columns; migrations via CI (`dotnet ef database update`), not on prod API startup.
- **PKs:** `Guid` / Postgres `uuid` on domain entities; FKs reference parent PKs; public tenant URLs use **slug** — [docs/DATABASE.md](./DATABASE.md).
- Multi-tenant: global query filters on tenant-scoped entities; validate membership on every write.
- Platform routes: `[Authorize(Roles = "PlatformAdmin")]`.
- Secrets (SendGrid, Supabase service role, JWT) — Render env only.
- **Tests:** xUnit · Moq · FluentAssertions · Coverlet · `dotnet test` — see `docs/ARCHITECTURE.md`; project `OnlinePortfolio.Api.Tests`.
- **Cross-stack:** API/DTO/auth/env changes usually need matching frontend proxy, types, or pages — see `docs/AGENT_GUIDE.md`.
- See `docs/DATABASE.md` and `docs/ARCHITECTURE.md`.
