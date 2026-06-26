---
description: Vue SFC language split — English script, pt-BR template only
applyTo: "**/*.vue"
---

# Vue single-file components

## Copilot inline completions

When the cursor is in **`<script>`** or **`<script setup>`**:

- Complete **comments in English** (never Portuguese).
- Complete **developer-facing** strings in English.
- Do **not** mirror pt-BR from `<template>` into script suggestions.

When the cursor is in **`<template>`**:

- Complete **user-visible copy in Brazilian Portuguese (pt-BR)** — labels, headings, buttons, validation, empty states, error text shown in the page.

## Examples

**Good — script (English comment):**

```typescript
// Redirect unauthenticated users to login (app host only)
```

**Bad — script (Portuguese comment suggested from template context):**

```typescript
// Redireciona usuários não autenticados para o login
```

**Good — template (pt-BR):**

```vue
<p>Área de login em breve.</p>
```

**Good — user-facing API/error message in script (pt-BR):**

```typescript
statusMessage: 'Login disponível apenas em app.onlineportfolio.com.br',
```

Reference: [docs/LANGUAGE.md](../../docs/LANGUAGE.md)
