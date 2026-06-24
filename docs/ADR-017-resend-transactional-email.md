# ADR-017: Email transacional via Resend (substitui SendGrid)

| | |
|---|---|
| **Status** | ✅ Aceito |
| **Data** | 2026-06-21 |
| **Decisores** | Operador / time do projeto |
| **Relacionado** | DEV-014, DEV-107, DEV-011 · [docs/ARCHITECTURE.md](./ARCHITECTURE.md) seção 11 |

---

## Contexto

A arquitetura v1 documentava **SendGrid** (Twilio) como provedor de email transacional (`mail@onlineportfolio.com.br`) para:

- Formulário de contato (visitante → artista)
- Convites de usuário (accept-invite)
- Notificações futuras da plataforma

Critérios do projeto:

- **API .NET** envia email (nunca o browser)
- **Sem caixa postal** no v1 — só envio
- **Custo baixo / free tier** sustentável para MVP solo
- **Boa experiência em C#** — SDK oficial, DI, templates versionados no repo
- Integração com `IEmailService` (abstração já prevista)

### O que mudou no mercado (SendGrid)

1. **Twilio adquiriu o SendGrid** em fevereiro de 2019 (~US$ 3 bi). O produto continua como **Twilio SendGrid**, com integração gradual ao ecossistema Twilio (incl. consolidação de sites em 2026).
2. **Fim do plano gratuito permanente:** em **maio de 2025**, o SendGrid removeu o tier free de 100 emails/dia. O entry point passou a ser **trial (~60 dias)** e depois plano pago (ordem de **~US$ 20/mês** no Essentials).
3. A documentação do projeto ainda citava “100 emails/dia free” — **desatualizado** e incompatível com Epic 0 (custo mínimo).

### Alternativas avaliadas (jun 2026)

| Provedor | Free permanente | SDK .NET | Templates | Notas |
|----------|-----------------|----------|-----------|--------|
| **SendGrid** | ❌ (trial → pago) | Maduro (`SendGrid` NuGet) | Dynamic templates no painel | Histórico no repo; custo cedo |
| **Brevo** | ✅ ~300/dia (~9k/mês) | Oficial, gerado (verboso) | Editor visual + `templateId` | Melhor volume grátis; SDK pesado |
| **Resend** | ✅ 3k/mês (100/dia) | Oficial (`Resend` NuGet, 2025) | HTML/Razor no repo | Melhor DX C#; volume suficiente para v1 |
| **Postmark** | Trial pequeno | Bom | Bom | Pago cedo; excelente deliverability |
| **Amazon SES** | Barato em escala | AWS SDK | Manual | Overhead infra; fora do escopo v1 |

**Discussão:** Brevo ganha em volume grátis e editor visual; **Resend** ganha em **developer experience .NET** (SDK pequeno, `IResend`, exemplos ASP.NET Core) e **templates no Git** (Razor/HTML em `EmailTemplates/`), alinhado ao monorepo e ao fluxo “só devs”. Volume Resend (100/dia) cobre contato + convites no início do produto.

---

## Decisão

Adotar **Resend** como provedor de email transacional v1.

- Remetente: `mail@onlineportfolio.com.br` (identidade de envio; sem caixa postal no v1)
- **Reply-To:** obrigatório quando resposta faz sentido — contato = email do visitante; convites/notificações = email do operador (ex. Outlook pessoal até Workspace)
- Config no Render: `Resend__ApiKey`, `Resend__FromEmail`, `Resend__FromName`
- Implementação: `IEmailService` → `ResendEmailService` (pacote NuGet `Resend`)
- Templates: ficheiros no backend (`EmailTemplates/` — Razor ou HTML estático), versionados no repo
- Autenticação de domínio (DKIM/SPF/DMARC): **DEV-011** ✅ — registros DNS na Vercel

**SendGrid deixa de ser referência** em docs, backlog e env vars.

---

## Consequências

### Positivas

- Free tier **permanente** (3.000 emails/mês) adequado ao MVP
- SDK .NET **oficial** e moderno ([resend.com/docs/send-with-dotnet](https://resend.com/docs/send-with-dotnet))
- Templates no **código** — review em PR, sem drift painel vs prod
- `IEmailService` mantém troca de provedor futura sem mudar controllers

### Trade-offs

- Menos volume grátis que Brevo (100/dia vs 300/dia) — aceitável no v1
- Sem editor drag-and-drop para não-devs — ok para time atual
- DKIM/SPF/DMARC via Resend — configurado em DEV-011 ✅

### Rejeitado

| Opção | Motivo |
|-------|--------|
| Manter SendGrid | Sem free permanente; docs já obsoletas |
| Brevo | SDK C# gerado e verboso; templates fora do repo |
| Postmark / SES | Custo ou complexidade desnecessários no v1 |

---

## Implementação (referência)

```text
backend/OnlinePortfolio.Api/
  Email/
    IEmailService.cs
    ResendEmailService.cs
  EmailTemplates/
    ContactForm.cshtml          # ou .html
    UserInvite.cshtml
```

```csharp
// Program.cs — produção (DEV-107)
builder.Services.AddHttpClient<ResendClient>();
builder.Services.Configure<ResendClientOptions>(o =>
    o.ApiToken = configuration["Resend:ApiKey"]!);  // env: Resend__ApiKey
builder.Services.AddTransient<IResend, ResendClient>();
builder.Services.AddTransient<IEmailService, ResendEmailService>();
```

**Smoke test (DEV-014)** — antes do domínio verificado, enviar de `onboarding@resend.dev`:

```csharp
using Resend;

IResend resend = ResendClient.Create("re_xxxxxxxxx");  // substituir; nunca commitar

await resend.EmailSendAsync(new EmailMessage()
{
    From = "onboarding@resend.dev",
    To = "seu-email@exemplo.com",
    Subject = "Hello World",
    HtmlBody = "<p>Congrats on sending your <strong>first email</strong>!</p>",
});
```

Ver [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md) seção 8.5 · [docs/DEV_COMMANDS.md](./DEV_COMMANDS.md).

**Tickets:** DEV-014 (conta + API key + Render env) ✅ · DEV-107 (código) · DEV-011 (domínio verificado) ✅

---

## Emenda (2026-06-23)

- Remetente alterado de `noreply@` para `mail@onlineportfolio.com.br` — recomendação Resend para deliverability (`noreply` penaliza entrega e o botão Responder).
- `mail@` continua **sem inbox** no v1; respostas vão para **Reply-To**, não para o domínio.
- DMARC (`TXT` `_dmarc` na Vercel DNS) documentado em [EXTERNAL_PROVIDERS](./EXTERNAL_PROVIDERS.md) seção 10.4.

---

## Referências

- [Resend .NET SDK](https://github.com/resend/resend-dotnet)
- [Resend pricing / quotas](https://resend.com/docs/knowledge-base/account-quotas-and-limits)
- [Twilio completes SendGrid acquisition (2019)](https://www.twilio.com/en-us/press/releases/twilio-completes-acquisition-sendgrid)
- [SendGrid free tier removal (2025)](https://agentdeals.dev/email-comparison-2026) — contexto de mercado
