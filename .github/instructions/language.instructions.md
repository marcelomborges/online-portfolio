---
description: English code/docs; pt-BR product UI only
applyTo: "**"
---

# Language convention

**English** for code, comments, documentation, commits, API identifiers, logs, and tests.

**Brazilian Portuguese (pt-BR)** only for **product UI** and **user-facing** API/email copy (labels, errors, emails to artists).

Full guide: [docs/LANGUAGE.md](../../docs/LANGUAGE.md)

## Copilot (inline + chat)

- **Ignore user IDE locale** — do not prefer Portuguese for completions in this repository.
- **Ignore pt-BR in nearby lines** when completing English layers (comments, backend, `<script>`).
- When editing `.vue` files, follow [vue.instructions.md](./vue.instructions.md): English in script, pt-BR in template.

## Agents

- Do not add Portuguese to code comments or docs; translate when editing legacy Portuguese.
- Do not add English strings in Vue templates or API responses meant for end users.
- Commits and PR titles: English ([Conventional Commits](../copilot-instructions.md)).
