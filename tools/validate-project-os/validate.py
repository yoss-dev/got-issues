#!/usr/bin/env python3
"""Consistency validator for project-os state.

Checks that the framework's explicit state actually agrees with itself:
ticket frontmatter, backlog index, sprint table, ADR index, and
relative links. Stdlib only. Exit 0 = consistent, 1 = findings.

Run from anywhere inside the repo:  python3 tools/validate-project-os/validate.py
"""
import glob
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OS_DIR = os.path.join(ROOT, "project-os")

TICKET_STATUSES = {"backlog", "ready", "committed", "in-progress", "blocked",
                   "in-acceptance", "done", "dropped"}
OWNED_STATUSES = {"in-progress", "blocked"}  # only these may carry an owner
READY_PLUS = {"ready", "committed", "in-progress", "blocked", "in-acceptance", "done"}
IN_SPRINT_STATUSES = {"committed", "in-progress", "blocked", "in-acceptance"}
ADR_STATUS_RE = re.compile(r"^(Proposed|Accepted|Rejected|Deprecated|Superseded by ADR-\d{4})$")

findings = []


def fail(msg):
    findings.append(msg)


def read(path):
    with open(path, encoding="utf-8") as f:
        return f.read()


def frontmatter(text):
    m = re.match(r"^---\n(.*?)\n---\n", text, re.S)
    if not m:
        return None
    fm = {}
    for line in m.group(1).splitlines():
        mm = re.match(r"^([\w-]+):\s*([^#]*?)\s*(#.*)?$", line)
        if mm:
            fm[mm.group(1)] = mm.group(2).strip()
    return fm


def rel(path):
    return os.path.relpath(path, ROOT)


def section(text, heading):
    m = re.search(rf"^## {re.escape(heading)}\s*$(.*?)(?=^## |\Z)", text, re.M | re.S)
    return m.group(1) if m else ""


# ---------- tickets ----------
tickets = {}  # id -> {"status": ..., "owner": ..., "path": ...}
for path in sorted(glob.glob(os.path.join(OS_DIR, "product", "tickets", "T-*.md"))):
    name = os.path.basename(path)
    text = read(path)
    fm = frontmatter(text)
    if fm is None:
        fail(f"{rel(path)}: missing YAML frontmatter")
        continue
    tid = fm.get("id", "")
    if not re.match(r"^T-\d{4}$", tid):
        fail(f"{rel(path)}: frontmatter id {tid!r} is not T-NNNN")
        continue
    if not name.startswith(tid + "-"):
        fail(f"{rel(path)}: filename does not start with its id {tid}")
    if tid in tickets:
        fail(f"{rel(path)}: duplicate ticket id {tid} (also {rel(tickets[tid]['path'])})")
        continue
    status = fm.get("status", "")
    if status not in TICKET_STATUSES:
        fail(f"{rel(path)}: illegal status {status!r}")
    owner = fm.get("owner", "none") or "none"
    if status in OWNED_STATUSES and owner == "none":
        fail(f"{rel(path)}: status {status} requires an owner")
    if status not in OWNED_STATUSES and owner != "none":
        fail(f"{rel(path)}: status {status} must not carry owner {owner!r}")

    # evidence gates
    ttype = fm.get("type", "")
    if status in READY_PLUS:
        if not re.search(r"-\s*\[x\]", section(text, "Definition of Ready"), re.I):
            fail(f"{rel(path)}: status {status} but the DoR checkbox is not ticked")
        if ttype not in ("spike", "chore") and not re.search(r"^\s*-\s*\[[ x]\]", section(text, "Acceptance Criteria"), re.M):
            fail(f"{rel(path)}: status {status} but no acceptance criteria items")
    implemented_by = fm.get("implemented_by", "none") or "none"
    accepted_by = fm.get("accepted_by", "none") or "none"
    if status in {"in-acceptance", "done"} and implemented_by == "none":
        fail(f"{rel(path)}: status {status} requires implemented_by")
    if status == "done":
        if not re.search(r"-\s*\[x\]", section(text, "Definition of Done"), re.I):
            fail(f"{rel(path)}: done but the DoD checkbox is not ticked")
        if accepted_by == "none":
            fail(f"{rel(path)}: done requires accepted_by")
        elif implemented_by != "none" and accepted_by == implemented_by:
            fail(f"{rel(path)}: accepted_by == implemented_by ({accepted_by!r}) — independence violated")
    elif accepted_by != "none":
        fail(f"{rel(path)}: accepted_by set but status is {status}, not done")

    tickets[tid] = {"status": status, "owner": owner, "path": path}

# ---------- backlog index ----------
backlog_path = os.path.join(OS_DIR, "product", "BACKLOG.md")
backlog = read(backlog_path)
active_ids, completed_ids = set(), set()
for m in re.finditer(r"^\|\s*\d+\s*\|\s*\[(T-\d{4})\]\(([^)]+)\)\s*\|[^|]*\|[^|]*\|\s*([\w-]+)\s*\|[^|]*\|",
                     backlog, re.M):
    tid, link, status = m.group(1), m.group(2), m.group(3)
    active_ids.add(tid)
    if tid not in tickets:
        fail(f"BACKLOG.md: Active row {tid} has no ticket file")
        continue
    if not os.path.exists(os.path.join(OS_DIR, "product", link)):
        fail(f"BACKLOG.md: Active row {tid} links to missing {link}")
    if status != tickets[tid]["status"]:
        fail(f"BACKLOG.md: {tid} listed as {status!r} but ticket file says {tickets[tid]['status']!r}")
    if tickets[tid]["status"] in {"done", "dropped"}:
        fail(f"BACKLOG.md: {tid} is {tickets[tid]['status']} but still in the Active table")
for m in re.finditer(r"^\|\s*\[(T-\d{4})\]\(([^)]+)\)\s*\|[^|]*\|[^|]*\|\s*([\w-]+)\s*\|[^|]*\|",
                     backlog, re.M):
    tid, _, outcome = m.group(1), m.group(2), m.group(3)
    completed_ids.add(tid)
    if tid in tickets and outcome != tickets[tid]["status"]:
        fail(f"BACKLOG.md: Completed row {tid} outcome {outcome!r} != ticket status {tickets[tid]['status']!r}")
for tid, t in tickets.items():
    table = completed_ids if t["status"] in {"done", "dropped"} else active_ids
    which = "Completed" if t["status"] in {"done", "dropped"} else "Active"
    if tid not in table:
        fail(f"BACKLOG.md: {tid} ({t['status']}) missing from the {which} table")

# ---------- sprint ----------
sprint_path = os.path.join(OS_DIR, "delivery", "CURRENT_SPRINT.md")
sprint = read(sprint_path)
sprint_active = "No active sprint" not in sprint
sprint_ids = set()
if sprint_active:
    for m in re.finditer(r"^\|\s*\[(T-\d{4})\]\(([^)]+)\)\s*\|[^|]*\|\s*([\w-]+)\s*\|\s*([^|]*?)\s*\|[^|]*\|",
                         sprint, re.M):
        tid, link, status, owner = m.groups()
        sprint_ids.add(tid)
        if tid not in tickets:
            fail(f"CURRENT_SPRINT.md: row {tid} has no ticket file")
            continue
        if status != tickets[tid]["status"]:
            fail(f"CURRENT_SPRINT.md: {tid} shown as {status!r} but ticket file says {tickets[tid]['status']!r}")
        towner = tickets[tid]["owner"]
        if owner in ("", "—", "-"):
            owner = "none"
        if owner != towner:
            fail(f"CURRENT_SPRINT.md: {tid} owner {owner!r} != ticket owner {towner!r}")
# sprint membership: sprint-only statuses require an active sprint AND a table row
for tid, t in sorted(tickets.items()):
    if t["status"] in IN_SPRINT_STATUSES:
        if not sprint_active:
            fail(f"{rel(t['path'])}: status {t['status']} but no sprint is active")
        elif tid not in sprint_ids:
            fail(f"CURRENT_SPRINT.md: {tid} is {t['status']} but missing from the Committed Work table")

# ---------- ADRs ----------
adr_dir = os.path.join(OS_DIR, "architecture", "adr")
adr_index = read(os.path.join(adr_dir, "README.md"))
adr_files = {}
for path in sorted(glob.glob(os.path.join(adr_dir, "ADR-*.md"))):
    aid = os.path.basename(path)[:8]
    if aid in adr_files:
        fail(f"{rel(path)}: duplicate ADR id {aid}")
    adr_files[aid] = path
    m = re.search(r"^## Status\s*\n+([^\n]+)", read(path), re.M)
    if not m or not ADR_STATUS_RE.match(m.group(1).strip()):
        got = m.group(1).strip() if m else "<missing>"
        fail(f"{rel(path)}: illegal or missing ADR status {got!r}")
    if f"[{aid}]" not in adr_index:
        fail(f"adr/README.md: {aid} exists but is not in the index")
for m in re.finditer(r"\[(ADR-\d{4})\]\(([^)]+)\)", adr_index):
    if m.group(1) not in adr_files:
        fail(f"adr/README.md: index lists {m.group(1)} but no such file exists")

# ---------- relative links ----------
for path in glob.glob(os.path.join(ROOT, "**", "*.md"), recursive=True):
    if os.sep + ".git" + os.sep in path:
        continue
    d = os.path.dirname(path)
    for m in re.finditer(r"\]\(([^)#\s]+?)(#[^)]*)?\)", read(path)):
        target = m.group(1)
        if target.startswith(("http://", "https://", "mailto:")) or "NNN" in target:
            continue
        base = d
        # the ticket template's links are relative to product/tickets/, its destination
        if path.endswith(os.path.join("templates", "TICKET_TEMPLATE.md")) and target.startswith("../../"):
            base = os.path.join(OS_DIR, "product", "tickets")
        if not os.path.exists(os.path.normpath(os.path.join(base, target))):
            fail(f"{rel(path)}: broken link -> {target}")

# ---------- ideas ----------
idea_ids = re.findall(r"^## (IDEA-\d{3}):", read(os.path.join(OS_DIR, "product", "IDEAS.md")), re.M)
for dup in {i for i in idea_ids if idea_ids.count(i) > 1}:
    fail(f"IDEAS.md: duplicate id {dup}")

# ---------- report ----------
if findings:
    print(f"project-os validation: {len(findings)} finding(s)\n")
    for f in findings:
        print(f"  ✗ {f}")
    sys.exit(1)
print(f"project-os validation: OK ({len(tickets)} tickets, {len(adr_files)} ADRs)")
