# PROJECT.md — Project Facts, Constraints, and Configuration

> **Status: NOT YET BOOTSTRAPPED.** Run the [`bootstrap-project`](skills/bootstrap-project/SKILL.md) skill to populate this file. Until then, every value below is a placeholder and MUST NOT be treated as a project fact.

This file is the single source of truth for *what this project is* and *how it is built*. It sits at precedence level 2, directly below the [Way of Working](governance/WAY_OF_WORKING.md). Agents load it for almost every activity.

## Fact status convention

Every statement in this file carries one of these tags. Agents MUST respect them and MUST NOT promote a tag without evidence or a human decision:

- `[confirmed]` — stated by a human stakeholder or verified against reality (code, infra, contract).
- `[default]` — a reasonable industry default adopted because nobody objected. Safe to rely on; cheap to change.
- `[assumption]` — believed but unverified. An agent relying on an assumption for a significant decision must first escalate or verify.
- `[open]` — an unresolved question. Work that depends on it is blocked or must be escalated.

## 1. Identity

- **Project name:** *TBD* `[open]`
- **One-line description:** *TBD* `[open]`
- **Repository purpose:** product monorepo; delivery framework self-contained in `project-os/` per [ADR-0002](architecture/adr/ADR-0002-monorepo-with-self-contained-project-os.md) `[confirmed]`

## 2. Problem and users

- **Problem being solved:** *TBD* `[open]`
- **Target users:** *TBD* `[open]` (detail in [product/USER_PERSONAS.md](product/USER_PERSONAS.md))
- **Primary use cases:** *TBD* `[open]`

## 3. Goals, non-goals, success criteria

- **Product goals:** *TBD* `[open]`
- **Non-goals (explicitly out of scope):** *TBD* `[open]`
- **Success criteria:** *TBD* `[open]`

## 4. Constraints

Hard constraints that override ticket-level convenience (budget, compliance, deadlines, mandated technology, data residency, licensing):

- *TBD* `[open]`

## 5. Technical profile

| Aspect | Choice | Status |
| --- | --- | --- |
| Programming language(s) | *TBD* | `[open]` |
| Runtime | *TBD* | `[open]` |
| Backend framework | *TBD* | `[open]` |
| Frontend stack | *TBD* | `[open]` |
| Database / storage | *TBD* | `[open]` |
| API style | *TBD* | `[open]` |
| Authentication | *TBD* | `[open]` |
| Authorization model | *TBD* | `[open]` |
| Package / build tooling | *TBD* | `[open]` |
| Testing frameworks | *TBD* | `[open]` |
| CI/CD system | *TBD* | `[open]` |
| Hosting / cloud provider | *TBD* | `[open]` |
| Infrastructure approach | *TBD* | `[open]` |
| Observability | *TBD* | `[open]` |
| Supported environments | *TBD* | `[open]` |
| External integrations | *TBD* | `[open]` |

Significant technology choices made after bootstrap require an [ADR](architecture/adr/README.md); this table then links to it.

## 6. Engineering practices

| Practice | Decision | Status |
| --- | --- | --- |
| Source repository layout | Monorepo (`apps/`, `libs/`, `tools/`, `infra/`, `project-os/`) per [ADR-0002](architecture/adr/ADR-0002-monorepo-with-self-contained-project-os.md); skeleton tailored at bootstrap `[confirmed]` | |
| Branching strategy | Trunk-based, ticket branches, two commit lanes — see [standards/GIT.md](standards/GIT.md) `[default]` | |
| Code review | Every merge to trunk reviewed ([standards/GIT.md](standards/GIT.md)) `[default]` | |
| Test expectations | See [standards/TESTING.md](standards/TESTING.md) `[default]` | |
| Deployment strategy | *TBD* | `[open]` |
| Security requirements | See [standards/SECURITY.md](standards/SECURITY.md) `[default]` | |
| Documentation expectations | See [standards/DOCUMENTATION.md](standards/DOCUMENTATION.md) `[default]` | |
| Supported platforms | *TBD* | `[open]` |
| Performance expectations | *TBD* | `[open]` |

## 7. Open questions

Questions that block or shape work. Remove entries only when answered (record the answer above with `[confirmed]`).

| # | Question | Blocking? | Raised by / date |
| --- | --- | --- | --- |
| Q1 | Project not yet bootstrapped — run `bootstrap-project` | Yes | framework / 2026-08-30 |
