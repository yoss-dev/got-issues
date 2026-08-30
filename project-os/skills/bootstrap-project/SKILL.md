---
name: bootstrap-project
description: Initialize this delivery framework for a new software project by gathering product, technical, and engineering facts and populating PROJECT.md, vision, architecture, and standards.
---

# Skill: bootstrap-project

## Purpose

Turn the framework's placeholder documents into a project-specific operating system: capture what the project is, how it will be built, and how the team will work — distinguishing confirmed facts from defaults, assumptions, and open questions.

## When to Use

Once, when adopting this framework for a new (or newly onboarded existing) project. Re-run partially when a major pivot invalidates recorded facts.

## Active Persona(s)

Scrum Master (process), with Product Owner (product sections) and Architect (technical sections) perspectives.

## Inputs

- A human stakeholder available for questions, **or** an explicit instruction to bootstrap from provided material/an existing codebase without one.
- Any existing material: codebase, briefs, contracts, prior docs.

## Preconditions

- `PROJECT.md` still carries its "NOT YET BOOTSTRAPPED" banner, or the human has asked for a re-run.

## Context to Load

1. `PROJECT.md`
2. `product/PRODUCT_VISION.md`, `product/USER_PERSONAS.md`
3. `architecture/ARCHITECTURE.md`
4. `standards/*.md` (project-specific sections)
5. `governance/GLOSSARY.md`
6. The existing codebase's build/config files, if onboarding an existing project.

## Procedure

0. **Verify the adoption:** confirm this is a project copy (not the foundation repository itself — the foundation is never bootstrapped); the framework tree sits at `project-os/` in the monorepo root per ADR-0002; the root `.claude/skills` symlink resolves so agent harnesses discover the skills (replace with a copy/junction on Windows); and the lineage table in `FOUNDATION.md` is filled in (source, foundation version, copy date).
1. **Gather product facts.** Ask (or extract from material): project name; description; problem being solved; target users; primary use cases; product goals; non-goals; known constraints; success criteria. Batch questions; don't interrogate one item at a time.
2. **Gather technical facts.** Languages; runtime; frameworks; frontend/backend stacks; database/storage; API style; infrastructure and hosting; authentication and authorization; testing frameworks; package/build tools; CI/CD; observability; supported environments; external integrations. When onboarding an existing codebase, derive these from the code and mark them `[confirmed]` with a note of where verified.
3. **Gather engineering practices.** Monorepo skeleton tailoring (which of `apps/`, `libs/`, `tools/`, `infra/` this project needs — prune or rename, recording the result in `PROJECT.md`; the monorepo model itself is fixed by ADR-0002); the project-specific section of `standards/GIT.md` (hosting platform, trunk protection, governance path protection via CODEOWNERS or equivalent, merge strategy, release scheme) and the shared remote — configure it, or record solo mode in `PROJECT.md` §6 per GIT.md; wire `tools/validate-project-os/validate.py` into CI; review expectations; test expectations; deployment strategy; security requirements (incl. compliance); documentation expectations; supported platforms; performance expectations.
4. **Classify every fact** as `[confirmed]` (human said it / verified in reality), `[default]` (sensible default adopted, human informed), `[assumption]` (believed, unverified), or `[open]`. **Never record an assumption as a confirmed fact.** When the human is unavailable, prefer `[assumption]`/`[open]` over guessing upward.
5. **Populate the artifacts:** `PROJECT.md` (all sections; remove the banner only when §1–§6 have no `[open]` items that block starting work); `product/PRODUCT_VISION.md`; `product/USER_PERSONAS.md` (at least one persona or an explicit `[open]`); `architecture/ARCHITECTURE.md` (what is known; `[open]` elsewhere); project-specific sections of each `standards/*.md`; domain terms into `GLOSSARY.md`.
6. **Record technology decisions that were genuinely *decided* now** (not inherited) and meet the ADR bar as ADRs via `create-adr` — typically one "initial technology stack" ADR rather than ten small ones.
7. **List remaining `[open]` questions** in `PROJECT.md` §7 with blocking status.
8. **Report back** to the human: what was confirmed, what was defaulted, what was assumed, and which open questions need answers first.

## Validation

- No `*TBD*` remains without an explicit `[open]` tag and, if blocking, a §7 entry.
- Every technical-profile row is tagged; nothing silently promoted to `[confirmed]`.
- Standards' "project-specific" sections are either filled or explicitly marked open — not left as template text that looks authoritative.

## Outputs

Populated `PROJECT.md`, vision, user personas, architecture overview, standards; optional stack ADR; open-questions list.

## State Changes

May modify: `PROJECT.md`, `product/PRODUCT_VISION.md`, `product/USER_PERSONAS.md`, `architecture/ARCHITECTURE.md`, `standards/*.md`, `governance/GLOSSARY.md`, plus ADR files via `create-adr`. MUST NOT modify: governance rules, DoR/DoD, templates, skills.

## Failure / Escalation

- Stakeholder unavailable and material insufficient → populate what is derivable, mark the rest `[open]`, report; do not invent a product.
- Contradictory inputs (brief says X, code says Y) → record both, mark `[open]`, escalate per WoW §13.

## Example

Onboarding an existing Python/FastAPI service: the agent derives stack facts from `pyproject.toml` and CI config (`[confirmed]`), records "trunk-based development" as `[default]` after the lead shrugs, marks "target users beyond the internal ops team?" as `[open]` Q2 in `PROJECT.md` §7, and files ADR-0002 "Continue on FastAPI + PostgreSQL" only because the human explicitly chose to keep the stack after discussing a rewrite.
