# Language convention

Single source of truth for **human and AI-authored text** in this repository.

**Related:** [AGENT_GUIDE.md](./AGENT_GUIDE.md) · [FRONTEND_COMPONENTS.md](./FRONTEND_COMPONENTS.md)

---

## Rule of thumb

| Layer | Language |
|---|---|
| **Everything in the repo** | **English** |
| **Product UI only** | **Brazilian Portuguese (pt-BR)** — mandatory for end users |

There is **one exception**: strings that a **visitor, artist, or admin user** sees in the product.

---

## English (default)

Use English for:

| Area | Examples |
|---|---|
| **Source code** | Class, method, variable, enum, file names |
| **Database** | Table/column names, migrations, EF entities |
| **API contracts** | Paths (`/api/v1/auth/login`), JSON property names, OpenAPI |
| **Git** | Commit messages, PR titles, branch names ([Conventional Commits](./CONVENTIONAL_COMMITS.md)) |
| **Documentation** | `docs/**`, ADRs, README, runbooks, BACKLOG acceptance criteria |
| **Code comments & XML docs** | `//`, `///`, JSDoc, block comments in `.vue` `<script>` |
| **Logs & internal exceptions** | Serilog, server-side messages not returned to clients |
| **Tests** | Test method names, arrange/act descriptions |
| **CI / infra** | Workflow names, job steps, scripts (except user-facing output) |
| **Agent rules** | `.cursor/rules/**`, `AGENTS.md`, `.github/instructions/**` |
| **Email subject lines (optional)** | May be pt-BR if product-facing — prefer pt-BR for artist-facing mail |

**Legacy note:** Older git history or Linear issues may still quote Portuguese. All **current** docs (`docs/**`, agent rules) are English; pt-BR appears only in product UI strings and examples in this file.

---

## Brazilian Portuguese — product UI only (mandatory)

Use **pt-BR** for any text shown to **users** of the platform:

| Surface | Location | Examples |
|---|---|---|
| **Admin / login** | `frontend/components/app/`, `pages/login.vue`, `pages/admin/**` | Labels, buttons, validation messages, empty states |
| **Platform marketing** | `frontend/components/platform/`, apex pages | Headlines, CTAs (unless explicitly English brand copy) |
| **Public tenant sites** | `frontend/components/public/tenants/{slug}/` | Hero, gallery, contact, about |
| **Error pages** | `frontend/error.vue` | “Página não encontrada”, “Algo deu errado” |
| **API user-facing errors** | Login, contact, invite responses | Generic login failure, validation errors returned to UI |
| **Transactional email body** | Resend templates | Invite, contact notification — pt-BR for Brazilian artists |

### Do **not** put pt-BR in

- Variable names (`mensagemErro` → `errorMessage`; pt-BR only in the **string value**)
- Code comments explaining logic
- Documentation or BACKLOG
- Developer-only stubs visible in repo but not shipped (prefer removing or English)

### i18n (future)

If the product adds i18n later, default locale remains **`pt-BR`**; English UI is out of scope for v1 unless explicitly requested.

---

## Quick checklist for agents

Before opening a PR:

1. **Code & docs** you added/changed — English?
2. **User-visible strings** in Vue/API/email — pt-BR?
3. **Commits & PR title** — English Conventional Commits?
4. **No mixed language** in the same identifier or comment block?

---

## Examples

**Good — API error returned to login form (pt-BR):**

```csharp
return BadRequest(new { message = "Credenciais inválidas." });
```

**Good — log line (English):**

```csharp
_logger.LogInformation("Seeded Identity role {Role}", roleName);
```

**Good — Vue template (pt-BR):**

```vue
<UButton label="Entrar" />
<p>Não foi possível entrar. Tente novamente.</p>
```

**Bad — Portuguese comment:**

```typescript
// Verifica se o tenant está ativo
```

**Good:**

```typescript
// Returns false when tenant is inactive or missing
```

---

*Last updated: 2026-06-25*
