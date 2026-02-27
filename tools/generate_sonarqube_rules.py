#!/usr/bin/env python3
"""Generate SonarQube.yaml from Sonar C# catalog.

This script fetches all C# rules from SonarQube public API and writes
`roslyn-nav-rules/SonarQube.yaml`.

Rules are always included in the catalog, and a best-effort predicate mapping
is attached when a rule can be approximated with the current RoslynNavigator
predicate model.
"""

from __future__ import annotations

import math
import re
from pathlib import Path
from typing import Any

import requests

API_URL = "https://next.sonarqube.com/sonarqube/api/rules/search"
OUT_FILE = Path("roslyn-nav-rules/SonarQube.yaml")
PAGE_SIZE = 500


SEVERITY_MAP = {
    "BLOCKER": "error",
    "CRITICAL": "error",
    "MAJOR": "warning",
    "MINOR": "info",
    "INFO": "info",
}


# High-confidence Sonar mappings supported by current snapshot schema.
KEY_PREDICATE_OVERRIDES: dict[str, dict[str, Any]] = {
    "csharpsquid:S107": {"parameter_count_min": 8},
    "csharpsquid:S3776": {"cognitive_complexity": 15},
    "csharpsquid:S2068": {"has_hardcoded_secret": True},
    "csharpsquid:S2077": {"has_sql_string_concatenation": True},
    "csharpsquid:S2221": {"catches_general_exception": True},
    "csharpsquid:S112": {"throws_general_exception": True},
    "csharpsquid:S2245": {"uses_insecure_random": True},
    "csharpsquid:S4790": {"uses_weak_crypto": True},
}


def fetch_rules() -> list[dict[str, Any]]:
    first = requests.get(
        API_URL, params={"languages": "cs", "ps": PAGE_SIZE, "p": 1}, timeout=60
    )
    first.raise_for_status()
    payload = first.json()

    total = payload["total"]
    pages = math.ceil(total / PAGE_SIZE)

    rules = list(payload.get("rules", []))
    for page in range(2, pages + 1):
        response = requests.get(
            API_URL,
            params={"languages": "cs", "ps": PAGE_SIZE, "p": page},
            timeout=60,
        )
        response.raise_for_status()
        rules.extend(response.json().get("rules", []))

    unique = {rule["key"]: rule for rule in rules}
    return list(unique.values())


def sonar_id(rule_key: str) -> str:
    match = re.search(r"S(\d+)", rule_key)
    if match:
        return f"sonar-cs-S{match.group(1)}"
    normalized = re.sub(r"[^A-Za-z0-9]+", "-", rule_key).strip("-").lower()
    return f"sonar-cs-{normalized}"


def escape(value: str) -> str:
    return value.replace("\\", "\\\\").replace('"', '\\"')


def infer_predicate(rule_key: str, name: str) -> dict[str, Any] | None:
    if rule_key in KEY_PREDICATE_OVERRIDES:
        return KEY_PREDICATE_OVERRIDES[rule_key]

    return None


def write_yaml(rules: list[dict[str, Any]]) -> None:
    def sort_key(rule: dict[str, Any]) -> tuple[int, str]:
        match = re.search(r"S(\d+)", rule["key"])
        return (int(match.group(1)) if match else 10**9, rule["key"])

    rules.sort(key=sort_key)

    lines: list[str] = []
    mapped_count = 0

    lines.append("# SonarQube C# rule catalog for RoslynNavigator")
    lines.append(f"# Source: {API_URL}?languages=cs")
    lines.append(f"# Generated with {len(rules)} rules.")
    lines.append("# Rules with `predicate` are executable by `roslyn-nav check`.")
    lines.append("")
    lines.append("rules:")

    for rule in rules:
        key = rule["key"]
        title = rule.get("name", key)
        severity = SEVERITY_MAP.get(
            str(rule.get("severity", "MAJOR")).upper(), "warning"
        )
        rid = sonar_id(key)
        predicate = infer_predicate(key, title)
        if predicate:
            mapped_count += 1

        lines.append(f"  - id: {rid}")
        lines.append(f'    title: "{escape(title)}"')
        lines.append(f"    severity: {severity}")
        lines.append(f'    message: "Sonar rule {escape(key)}: {escape(title)}"')
        if predicate:
            lines.append("    predicate:")
            for pred_key, pred_value in predicate.items():
                if isinstance(pred_value, bool):
                    bool_text = "true" if pred_value else "false"
                    lines.append(f"      {pred_key}: {bool_text}")
                elif isinstance(pred_value, int):
                    lines.append(f"      {pred_key}: {pred_value}")
                else:
                    lines.append(f'      {pred_key}: "{escape(str(pred_value))}"')

    header = [
        f"# Executable mapped rules: {mapped_count}",
        "",
    ]
    lines = lines[:4] + header + lines[4:]

    OUT_FILE.parent.mkdir(parents=True, exist_ok=True)
    OUT_FILE.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(
        f"Wrote {OUT_FILE} with {len(rules)} rules ({mapped_count} mapped predicates)"
    )


def main() -> None:
    rules = fetch_rules()
    write_yaml(rules)


if __name__ == "__main__":
    main()
