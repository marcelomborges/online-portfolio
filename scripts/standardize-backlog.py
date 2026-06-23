#!/usr/bin/env python3
"""Normalize docs/BACKLOG.md issue blocks to a fixed four-section layout."""

from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BACKLOG = ROOT / "docs" / "BACKLOG.md"

ISSUE_HEADING = re.compile(
    r"^### ((?:DEV|UT|IT|SEC)-[\w]+) — (.+)$", re.MULTILINE
)
FIELD_TABLE = re.compile(
    r"\| Field \| Value \|\n\|[-| ]+\|\n(?:\| \*\*(.+?)\*\* \| (.+?) \|\n?)+",
    re.MULTILINE,
)
FIELD_ROW = re.compile(r"^\| \*\*(.+?)\*\* \| (.+?) \|$", re.MULTILINE)
DESC = re.compile(
    r"^\*\*(?:Description|Descrição):\*\*\s*\n?(.*?)(?=\n\*\*|\Z)",
    re.MULTILINE | re.DOTALL,
)
ACCEPTANCE = re.compile(
    r"^\*\*(?:Acceptance criteria|Critérios de aceitação):\*\*\s*\n"
    r"((?:- \[[ x]\].+(?:\n|$))+)",
    re.MULTILINE,
)
OBSERVATIONS = re.compile(
    r"^\*\*Observações:\*\*\s*\n(.*?)(?=\n---\s*$|\Z)",
    re.MULTILINE | re.DOTALL,
)
OBS_META = re.compile(
    r"^- \*\*(Phase|Area|Priority|Depends on|Status):\*\* (.+)$",
    re.MULTILINE,
)
META_KEYS = ("Phase", "Area", "Priority", "Depends on", "Status")


def parse_fields(block: str) -> tuple[dict[str, str], str]:
    fields: dict[str, str] = {}
    match = FIELD_TABLE.search(block)
    if not match:
        return fields, block
    table_text = match.group(0)
    for row in FIELD_ROW.finditer(table_text):
        fields[row.group(1).strip()] = row.group(2).strip()
    remainder = block[: match.start()] + block[match.end() :]
    return fields, remainder.strip()


def parse_observations_meta(text: str) -> tuple[dict[str, str], str]:
    fields: dict[str, str] = {}
    for match in OBS_META.finditer(text):
        fields[match.group(1)] = match.group(2).strip()
    remainder = OBS_META.sub("", text).strip()
    return fields, remainder


def parse_issue_body(block: str) -> tuple[str, str, str, str]:
    lines = block.splitlines()
    title = lines[0]
    body = "\n".join(lines[1:]).strip()
    body = re.sub(r"\n---\s*$", "", body).strip()

    fields, body = parse_fields(body)

    description = ""
    desc_match = DESC.search(body)
    if desc_match:
        description = desc_match.group(1).strip()
        body = (body[: desc_match.start()] + body[desc_match.end() :]).strip()

    acceptance = ""
    ac_match = ACCEPTANCE.search(body)
    if ac_match:
        acceptance = ac_match.group(1).rstrip()
        body = (body[: ac_match.start()] + body[ac_match.end() :]).strip()

    extra_notes = ""
    obs_match = OBSERVATIONS.search(body)
    if obs_match:
        obs_body = obs_match.group(1).strip()
        obs_fields, extra_notes = parse_observations_meta(obs_body)
        for key, value in obs_fields.items():
            fields.setdefault(key, value)
        body = (body[: obs_match.start()] + body[obs_match.end() :]).strip()

    body = re.sub(r"\n---\s*$", "", body).strip()
    if body.strip():
        extra_notes = f"{extra_notes}\n\n{body.strip()}".strip() if extra_notes else body.strip()

    observations: list[str] = []
    for key in META_KEYS:
        if key in fields and fields[key] not in ("—", "-", ""):
            observations.append(f"- **{key}:** {fields[key]}")

    if extra_notes:
        if observations:
            observations.append("")
        observations.append(extra_notes)

    obs_text = "\n".join(observations) if observations else "- *(nenhuma)*"
    if not description:
        description = "—"

    return title, description, acceptance, obs_text


def format_issue(title: str, description: str, acceptance: str, observations: str) -> str:
    parts = [
        title,
        "",
        "**Descrição:**",
        description,
        "",
        "**Critérios de aceitação:**",
    ]
    if acceptance.strip():
        parts.append(acceptance.rstrip())
    else:
        parts.append("- [ ] *(definir)*")
    parts.extend(["", "**Observações:**", observations.rstrip()])
    return "\n".join(parts)


def transform(content: str) -> str:
    matches = list(ISSUE_HEADING.finditer(content))
    if not matches:
        return content

    out: list[str] = []
    cursor = 0
    for i, match in enumerate(matches):
        out.append(content[cursor : match.start()])
        end = matches[i + 1].start() if i + 1 < len(matches) else len(content)
        block = content[match.start() : end]
        block = re.sub(r"\n---\s*$", "", block.rstrip())
        title, description, acceptance, observations = parse_issue_body(block)
        out.append(format_issue(title, description, acceptance, observations))
        out.append("\n\n---\n\n")
        cursor = end

    tail = content[cursor:].lstrip("\n")
    if tail:
        out.append(tail if tail.endswith("\n") else tail + "\n")
    result = "".join(out)
    result = re.sub(r"(\n---\n\n){2,}", "\n---\n\n", result)
    return result


def main() -> None:
    original = BACKLOG.read_text(encoding="utf-8")
    transformed = transform(original)
    BACKLOG.write_text(transformed, encoding="utf-8")
    count = len(list(ISSUE_HEADING.finditer(transformed)))
    print(f"Standardized {count} issues in {BACKLOG.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
