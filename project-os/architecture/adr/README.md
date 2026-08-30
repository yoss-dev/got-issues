# Architecture Decision Records

ADRs capture **significant** architectural decisions: what was decided, why, what else was considered, and what it costs. They are the project's decision memory — the answer to "why on earth is it built this way?" two years later.

## When an ADR is required (the bar)

Record an ADR for decisions that materially affect: architecture or system boundaries; data models; major dependencies (frameworks, platforms, paid services); infrastructure; public APIs; security architecture; or cross-cutting engineering conventions. Rule of thumb: **if reversing the decision later would be expensive, or if a future engineer would ask "why?", write the ADR.**

Do **not** write ADRs for routine implementation details (naming, local structure, choice among already-approved libraries). An ADR for trivia devalues the ones that matter.

## Conventions

- Created via the [`create-adr`](../../skills/create-adr/SKILL.md) skill, from [`templates/ADR_TEMPLATE.md`](../../templates/ADR_TEMPLATE.md).
- Files: `ADR-NNNN-short-slug.md` in this directory. IDs sequential, never reused. Next ID: **ADR-0005**.
- Statuses: `Proposed` → `Accepted` | `Rejected`; `Accepted` → `Superseded by ADR-XXXX` | `Deprecated`.
- **ADRs are immutable once Accepted** except for status changes and links. Changing a decision means a *new* ADR that supersedes the old one — never editing history.
- Acceptance authority: the Software Architect persona accepts ADRs whose options do not differ materially in business consequence; otherwise a human decides ([WoW §13](../../governance/WAY_OF_WORKING.md)).
- Every ADR links the tickets that motivated it; every affected ticket links the ADR.

## Index

| ID | Title | Status | Date |
| --- | --- | --- | --- |
| [ADR-0001](ADR-0001-record-architecture-decisions.md) | Record architecture decisions as ADRs in this repository | Accepted | 2026-08-30 |
| [ADR-0002](ADR-0002-monorepo-with-self-contained-project-os.md) | Build the product as a monorepo with the framework self-contained under project-os/ | Accepted | 2026-08-30 |
| [ADR-0003](ADR-0003-initial-technology-stack.md) | Build Got Issues as a .NET 10 API on PostgreSQL with Duende IdentityServer, running entirely under Docker Compose | Accepted | 2026-08-30 |
| [ADR-0004](ADR-0004-contract-first-openapi-code-generation.md) | Generate server contracts and clients from a hand-authored OpenAPI specification using OpenAPI Generator | Accepted | 2026-08-30 |
