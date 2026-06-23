#!/usr/bin/env python3
"""
Regenerate docs/BACKLOG_LINEAR.csv from docs/BACKLOG.md (optional export for Linear).

Usage:
  python scripts/generate-linear-import.py

Labels in CSV use ", " between values — required by @linear/import (LinearCsvImporter).
"""

from __future__ import annotations

import csv
import re
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BACKLOG = ROOT / "docs" / "BACKLOG.md"
OUTPUT = ROOT / "docs" / "BACKLOG_LINEAR.csv"

# Linear CSV export header (Settings > Import/Export > Export CSV)
HEADERS = [
    "ID",
    "Team",
    "Title",
    "Description",
    "Status",
    "Estimate",
    "Priority",
    "Project ID",
    "Project",
    "Creator",
    "Assignee",
    "Labels",
    "Cycle Number",
    "Cycle Name",
    "Cycle Start",
    "Cycle End",
    "Created",
    "Updated",
    "Started",
    "Triaged",
    "Completed",
    "Canceled",
    "Archived",
    "Due Date",
    "Parent issue",
    "Roadmaps",
    "Project Milestone ID",
    "Project Milestone",
    "SLA Status",
]

PRIORITY_MAP = {
    "P0": "Urgent",
    "P1": "High",
    "P2": "Medium",
    "P3": "Low",
}

ISSUE_HEADING = re.compile(
    r"^### ((?:DEV|UT|IT|SEC)-[\w]+) — (.+)$", re.MULTILINE
)
EPIC_HEADING = re.compile(r"^## (.+)$", re.MULTILINE)
FIELD_ROW = re.compile(r"^\| \*\*(.+?)\*\* \| (.+?) \|$", re.MULTILINE)
OBS_META = re.compile(
    r"^- \*\*(Phase|Area|Priority|Depends on|Status):\*\* (.+)$",
    re.MULTILINE,
)
DESCRIPTION = re.compile(
    r"^\*\*(?:Description|Descrição):\*\*\s*\n?(.*?)(?=\n\*\*|\Z)",
    re.MULTILINE | re.DOTALL,
)
ACCEPTANCE = re.compile(
    r"^\*\*(?:Acceptance criteria|Critérios de aceitação):\*\*\s*\n"
    r"((?:- \[[ x]\].+(?:\n|$))+)",
    re.MULTILINE,
)

SKIP_TITLE_MARKERS = ("~~", "→ ver")


def strip_markdown_link(text: str) -> str:
    return re.sub(r"\[([^\]]+)\]\([^)]+\)", r"\1", text)


def clean_title(raw: str) -> str:
    title = raw.strip()
    title = title.replace("✅", "").strip()
    title = re.sub(r"\s+", " ", title)
    return title


def parse_table_fields(block: str) -> dict[str, str]:
    fields: dict[str, str] = {}
    for match in FIELD_ROW.finditer(block):
        key = match.group(1).strip()
        value = strip_markdown_link(match.group(2).strip())
        fields[key] = value
    for match in OBS_META.finditer(block):
        key = match.group(1).strip()
        value = strip_markdown_link(match.group(2).strip())
        fields.setdefault(key, value)
    return fields


def is_done(block: str, fields: dict[str, str], acceptance: str) -> bool:
    status = fields.get("Status", "")
    if "done" in status.lower() or "✅" in status:
        return True
    items = re.findall(r"- \[([ x])\]", acceptance)
    return bool(items) and all(mark == "x" for mark in items)


def build_description(
    issue_id: str,
    epic: str,
    fields: dict[str, str],
    description: str,
    acceptance: str,
) -> str:
    lines = [
        f"**Backlog ID:** `{issue_id}`",
        f"**Epic:** {epic}",
    ]
    for key in ("Phase", "Area", "Priority", "Depends on", "Status"):
        if key in fields and fields[key] not in ("—", "-", ""):
            lines.append(f"**{key}:** {fields[key]}")
    lines.extend(["", "## Description", "", description.strip()])
    if acceptance.strip():
        lines.extend(["", "## Acceptance criteria", "", acceptance.strip()])
    lines.extend(
        [
            "",
            "## References",
            "",
            "- docs/BACKLOG.md",
            "- docs/ARCHITECTURE.md",
            "- docs/EXTERNAL_PROVIDERS.md",
        ]
    )
    return "\n".join(lines)


def build_labels(area: str, issue_id: str) -> str:
    """Return Labels cell for Linear CSV import.

    @linear/import splits on ', ' (comma + space), not bare comma — see
    LinearCsvImporter.ts: row.Labels.split(', ')
    """
    labels: list[str] = []
    seen: set[str] = set()

    def add(label: str) -> None:
        normalized = label.strip()
        key = normalized.lower()
        if normalized and key not in seen:
            seen.add(key)
            labels.append(normalized)

    prefix = issue_id.split("-")[0].lower()
    type_map = {
        "dev": "feature",
        "ut": "unit-test",
        "it": "integration-test",
        "sec": "security",
    }
    add(type_map.get(prefix, prefix))
    for part in re.split(r"[,·]+", area):
        add(part.strip())

    return ", ".join(labels)


def parse_epics(content: str) -> list[tuple[int, str]]:
    epics: list[tuple[int, str]] = []
    for match in EPIC_HEADING.finditer(content):
        name = match.group(1).strip()
        if name.startswith("Epic ") or name in (
            "Unit tests",
            "Integration tests",
            "Security",
        ):
            epics.append((match.start(), name))
    return epics


def epic_for_position(epics: list[tuple[int, str]], pos: int) -> str:
    current = "Uncategorized"
    for start, name in epics:
        if start <= pos:
            current = name
        else:
            break
    return current


def parse_issues(content: str) -> list[dict]:
    epics = parse_epics(content)
    issues: list[dict] = []
    created = datetime(2025, 6, 21, 12, 0, 0, tzinfo=timezone.utc).strftime(
        "%Y-%m-%dT%H:%M:%S.000Z"
    )
    completed_ts = datetime(2025, 6, 21, 18, 0, 0, tzinfo=timezone.utc).strftime(
        "%Y-%m-%dT%H:%M:%S.000Z"
    )

    for match in ISSUE_HEADING.finditer(content):
        issue_id = match.group(1)
        raw_title = match.group(2)
        if any(marker in raw_title for marker in SKIP_TITLE_MARKERS):
            continue

        start = match.start()
        next_heading = ISSUE_HEADING.search(content, match.end())
        end = next_heading.start() if next_heading else len(content)
        block = content[start:end]

        fields = parse_table_fields(block)
        desc_match = DESCRIPTION.search(block)
        description = desc_match.group(1).strip() if desc_match else ""
        ac_match = ACCEPTANCE.search(block)
        acceptance = ac_match.group(1).rstrip() if ac_match else ""

        epic = epic_for_position(epics, start)
        title = clean_title(f"{issue_id} — {raw_title}")
        priority_raw = fields.get("Priority", "P2")
        priority = PRIORITY_MAP.get(priority_raw.split()[0], "Medium")
        done = is_done(block, fields, acceptance)
        status = "Done" if done else "Backlog"
        area = fields.get("Area", "")

        issues.append(
            {
                "ID": issue_id,
                "Team": "Online Portfolio",
                "Title": title,
                "Description": build_description(
                    issue_id, epic, fields, description, acceptance
                ),
                "Status": status,
                "Estimate": "",
                "Priority": priority,
                "Project ID": "",
                "Project": epic,
                "Creator": "",
                "Assignee": "",
                "Labels": build_labels(area, issue_id),
                "Cycle Number": "",
                "Cycle Name": "",
                "Cycle Start": "",
                "Cycle End": "",
                "Created": created,
                "Updated": created,
                "Started": completed_ts if done else "",
                "Triaged": "",
                "Completed": completed_ts if done else "",
                "Canceled": "",
                "Archived": "",
                "Due Date": "",
                "Parent issue": fields.get("Depends on", "")
                if fields.get("Depends on", "") not in ("—", "-", "")
                else "",
                "Roadmaps": "",
                "Project Milestone ID": "",
                "Project Milestone": "",
                "SLA Status": "",
            }
        )

    return issues


def main() -> None:
    content = BACKLOG.read_text(encoding="utf-8")
    issues = parse_issues(content)
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)

    with OUTPUT.open("w", encoding="utf-8", newline="") as f:
        writer = csv.DictWriter(
            f,
            fieldnames=HEADERS,
            quoting=csv.QUOTE_ALL,
            lineterminator="\n",
        )
        writer.writeheader()
        writer.writerows(issues)

    print(f"Wrote {len(issues)} issues to {OUTPUT.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
