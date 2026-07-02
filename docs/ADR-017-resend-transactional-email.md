# ADR-017: Transactional email via Resend (replaces SendGrid)

| | |
|---|---|
| **Status** | ✅ Accepted |
| **Date** | 2026-06-21 |
| **Deciders** | Operator / project team |
| **Related** | DEV-014, DEV-107, DEV-011 · [docs/ARCHITECTURE.md](./ARCHITECTURE.md) section 11 |

---

## Context

The v1 architecture documented **SendGrid** (Twilio) as the transactional email provider (`mail@onlineportfolio.com.br`) for:

- Contact form (visitor → artist)
- User invites (accept-invite)
- Future platform notifications

Project criteria:

- **.NET API** sends email (never the browser)
- **No mailbox** in v1 — send only
- **Low cost / sustainable free tier** for solo MVP
- **Good C# experience** — official SDK, DI, templates versioned in repo
- Integration with `IEmailService` (abstraction already planned)

### What changed in the market (SendGrid)

1. **Twilio acquired SendGrid** in February 2019 (~US$ 3B). The product continues as **Twilio SendGrid**, with gradual integration into the Twilio ecosystem (including site consolidation in 2026).
2. **End of permanent free plan:** in **May 2025**, SendGrid removed the 100 emails/day free tier. Entry point became **trial (~60 days)** then paid plan (around **~US$ 20/month** on Essentials).
3. Project documentation still cited "100 emails/day free" — **outdated** and incompatible with Epic 0 (minimum cost).

### Alternatives evaluated (Jun 2026)

| Provider | Permanent free | .NET SDK | Templates | Notes |
|----------|----------------|----------|-----------|--------|
| **SendGrid** | ❌ (trial → paid) | Mature (`SendGrid` NuGet) | Dynamic templates in dashboard | History in repo; cost early |
| **Brevo** | ✅ ~300/day (~9k/month) | Official, generated (verbose) | Visual editor + `templateId` | Best free volume; heavy SDK |
| **Resend** | ✅ 3k/month (100/day) | Official (`Resend` NuGet, 2025) | HTML/Razor in repo | Best C# DX; sufficient volume for v1 |
| **Postmark** | Small trial | Good | Good | Paid early; excellent deliverability |
| **Amazon SES** | Cheap at scale | AWS SDK | Manual | Infra overhead; out of v1 scope |

**Discussion:** Brevo wins on free volume and visual editor; **Resend** wins on **.NET developer experience** (small SDK, `IResend`, ASP.NET Core examples) and **templates in Git** (Razor/HTML in `EmailTemplates/`), aligned with the monorepo and "devs only" workflow. Resend volume (100/day) covers contact + invites at product start.

---

## Decision

Adopt **Resend** as the v1 transactional email provider.

- Sender: `mail@onlineportfolio.com.br` (send identity; no mailbox in v1)
- **Reply-To:** required when a reply makes sense — contact = visitor email; invites/notifications = operator email (e.g. personal Outlook until Workspace)
- Render config: `Resend__ApiKey`, `Resend__FromEmail`, `Resend__FromName`
- Implementation: `IEmailService` → `ResendEmailService` (`Resend` NuGet package)
- Templates: files in backend (`EmailTemplates/` — Razor or static HTML), versioned in repo
- Domain authentication (DKIM/SPF/DMARC): **DEV-011** ✅ — DNS records on Vercel

**SendGrid is no longer referenced** in docs, backlog, or env vars.

---

## Consequences

### Positive

- **Permanent** free tier (3,000 emails/month) suitable for MVP
- **Official** modern .NET SDK ([resend.com/docs/send-with-dotnet](https://resend.com/docs/send-with-dotnet))
- Templates in **code** — PR review, no dashboard vs prod drift
- `IEmailService` keeps future provider swap without changing controllers

### Trade-offs

- Less free volume than Brevo (100/day vs 300/day) — acceptable in v1
- No drag-and-drop editor for non-devs — ok for current team
- DKIM/SPF/DMARC via Resend — configured in DEV-011 ✅

### Rejected

| Option | Reason |
|-------|--------|
| Keep SendGrid | No permanent free; docs already obsolete |
| Brevo | Generated verbose C# SDK; templates outside repo |
| Postmark / SES | Unnecessary cost or complexity in v1 |

---

## Implementation (reference)

```text
backend/OnlinePortfolio.Api/
  Email/
    IEmailService.cs
    ResendEmailService.cs
  EmailTemplates/
    ContactForm.cshtml          # or .html
    UserInvite.cshtml
```

```csharp
// Program.cs — production (DEV-107)
builder.Services.AddHttpClient<ResendClient>();
builder.Services.Configure<ResendClientOptions>(o =>
    o.ApiToken = configuration["Resend:ApiKey"]!);  // env: Resend__ApiKey
builder.Services.AddTransient<IResend, ResendClient>();
builder.Services.AddTransient<IEmailService, ResendEmailService>();
```

**Smoke test (DEV-014)** — before domain verification, send from `onboarding@resend.dev`:

```csharp
using Resend;

IResend resend = ResendClient.Create("re_xxxxxxxxx");  // replace; never commit

await resend.EmailSendAsync(new EmailMessage()
{
    From = "onboarding@resend.dev",
    To = "your-email@example.com",
    Subject = "Hello World",
    HtmlBody = "<p>Congrats on sending your <strong>first email</strong>!</p>",
});
```

See [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md) section 8.5 · [docs/DEV_COMMANDS.md](./DEV_COMMANDS.md).

**Tickets:** DEV-014 (account + API key + Render env) ✅ · DEV-107 (code) · DEV-011 (verified domain) ✅

---

## Amendment (2026-06-23)

- Sender changed from `noreply@` to `mail@onlineportfolio.com.br` — Resend recommendation for deliverability (`noreply` hurts delivery and the Reply button).
- `mail@` remains **without inbox** in v1; replies go to **Reply-To**, not the domain.
- DMARC (`TXT` `_dmarc` on Vercel DNS) documented in [EXTERNAL_PROVIDERS](./EXTERNAL_PROVIDERS.md) section 10.4.

---

## References

- [Resend .NET SDK](https://github.com/resend/resend-dotnet)
- [Resend pricing / quotas](https://resend.com/docs/knowledge-base/account-quotas-and-limits)
- [Twilio completes SendGrid acquisition (2019)](https://www.twilio.com/en-us/press/releases/twilio-completes-acquisition-sendgrid)
- [SendGrid free tier removal (2025)](https://agentdeals.dev/email-comparison-2026) — market context
