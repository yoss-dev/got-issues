---
id: T-0013
title: Enforce the migration boundary with database privileges, not convention
type: technical
status: ready
priority: low
owner: none
implemented_by: none
accepted_by: none
depends_on: [T-0001]
adrs: [ADR-0003]
created: 2026-08-30
updated: 2026-08-31
---

# T-0013: Enforce the migration boundary with database privileges, not convention

## Problem / Context

Raised during T-0001's independent review (`claude-rev-2c8d`, 2026-08-30) and deferred as a design change beyond that ticket's scope.

[ADR-0003](../../architecture/adr/ADR-0003-initial-technology-stack.md) requires migrations to be applied by an explicit step, never silently at API startup. T-0001 satisfies this and the reviewer confirmed it by experiment — schema dropped, API started alone, zero tables created.

But the guarantee rests on **an `args.Contains("--migrate")` branch**, and the API and migrator connect as the **same PostgreSQL superuser with full DDL rights**. Nothing at the database level prevents the API from altering the schema; the boundary is a reviewed invariant, not an enforced one. As the reviewer put it, that is a meaningful distinction once more people and more agents touch the code.

## Desired Outcome

The API's database role is incapable of DDL, so the migration boundary holds even if application code tries to cross it.

## User / Business Value

Turns a rule that reviewers must remember into one the database enforces. The value is in the failure case: a well-meant `Database.Migrate()` added later fails loudly at the permission boundary instead of silently mutating a schema.

## Scope

### In Scope

- Separate roles: a migration role owning the schema with DDL rights, and an application role with DML only.
- Compose wiring so the migrator and the API connect as their respective roles.
- Grants covering existing and future tables, so a new migration does not silently leave the app without access.
- Verification that the API genuinely cannot perform DDL.

### Out of Scope

- Row-level security or per-tenant isolation — the system is single-tenant.
- Secret management beyond what T-0001 established.
- Auditing DDL.

## Acceptance Criteria

- [ ] AC1: Given the API's database role, when it attempts any DDL (create/alter/drop a table), then PostgreSQL refuses it.
- [ ] AC2: Given the API's role, when it performs normal reads and writes, then they succeed — the restriction costs no application functionality.
- [ ] AC3: Given the migration step, when it runs, then it applies migrations successfully under its own role.
- [ ] AC4: Given a migration that adds a table, when it is applied, then the application role can use that new table without a manual grant.
- [ ] AC5: Given a clean clone, when the stack starts, then role creation and grants happen automatically — no manual database setup.
- [ ] AC6: Given the two roles, when the repository is inspected, then neither password is committed — both come from the environment as T-0001 established ([SECURITY.md](../../standards/SECURITY.md)).
- [ ] AC7: Given `tools/smoke.sh`, when it runs against the role-separated stack, then all its checks still pass — least privilege must not break the stack it protects, and the migration step in particular still exits 0.

## Examples / Scenarios

- API attempts `CREATE TABLE`: permission denied.
- API inserts and queries: succeeds.
- New migration adds a table; the API reads it with no extra grant (AC4 — the step most likely to be missed, since default privileges must be set up in advance).

## Dependencies

**T-0001** — the stack, roles, and migration step must exist.

## Risks / Unknowns

- **AC4 is the subtle one.** Grants on existing tables do not cover future ones; `ALTER DEFAULT PRIVILEGES` must be set for the migration role, and it applies only to objects created by that role. Easy to get wrong in a way that surfaces later as a runtime permission error.
- Bootstrapping roles on first start without embedding credentials needs care with `.env` and the postgres image's init scripts.
- Low priority deliberately: the current convention holds and is reviewed. This hardens it; it fixes no present defect.
- **The identity host shares the database** and runs its own migrations into the `identity` schema ([T-0010](T-0010-duende-identity-host.md)). A third role, or an explicit decision that the identity migrator keeps its current rights, is needed — otherwise AC5 quietly breaks the identity stack, and the smoke tier (AC7) is what would catch it. Named here so it is designed rather than discovered.

## Testing Notes

AC1 is the point of the ticket and must be a real negative test — the API's connection attempting DDL and being refused.

**Which tier, decided 2026-08-31.** AC1–AC4 belong to the **integration tier**: Testcontainers gives a real PostgreSQL where both roles can be created, and the negative test is a single connection attempt. AC5 and AC7 belong to the **smoke tier** ([T-0015](T-0015-compose-stack-smoke-test.md)), because "a clean clone creates the roles automatically" is a property of `compose.yaml` and its init path, which `WebApplicationFactory` cannot reach. Splitting them this way means neither tier is asked to prove something it structurally cannot — the mistake T-0003 made and T-0015 was created to repair.

**Mutate first** ([TESTING.md](../../standards/TESTING.md)): AC1 — grant the application role DDL and confirm the negative test fails. A permission test that passes because the statement was malformed, or because the table already existed, is the classic false green here; the mutation is what distinguishes "refused" from "did not happen".

## Relevant ADRs & Documentation

- [ADR-0003](../../architecture/adr/ADR-0003-initial-technology-stack.md) — the explicit migration step
- [SECURITY.md](../../standards/SECURITY.md) — least privilege
- [T-0001](T-0001-runnable-compose-stack.md) — where the convention is established

## Definition of Ready

- [x] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — evaluated 2026-08-31 during `refinement-session`. All nine universal items hold. Item 8 strengthened: each criterion now names the tier that can actually verify it. Conditional items: security is the subject (least privilege, SECURITY.md) and AC6 keeps the new credential out of the repository; data/migration impact identified — roles and default privileges are themselves schema state; no UX; no ADR-bar decision, since ADR-0003 already mandates the boundary and this changes only its enforcement.

## Definition of Done

- [ ] Meets [DoD](../../governance/DEFINITION_OF_DONE.md) — checked by `complete-ticket`.

---

## Work Log

### 2026-08-30 — Software Engineer (claude-sm-9d4e)

- **Did:** Created to capture a T-0001 review deferral as a linked ticket (DoD item 4).
- **Decided:** Priority `low` — the boundary currently holds; this makes it enforced rather than reviewed.
- **Remaining:** Refinement, then implementation.
- **Open questions / blockers:** none.
- **Branch / PR:** n/a
- **Test state:** n/a — not started.

### 2026-08-31 — Refinement (claude-sm-9d4e) — PO · BA · ENG · ARCH · QA · SEC

**The gap this refinement found: the identity host shares the database.** [T-0010](T-0010-duende-identity-host.md)
runs its own migration step into an `identity` schema, and the ticket's role design accounted
only for the API and its migrator. Introducing two roles without deciding what the identity
migrator connects as would break the identity stack on a clean start — and it would break it in
`compose.yaml`, where only the smoke tier looks. Recorded in Risks, with **AC7** as the check
that would catch it.

**Product (PO).** Value is entirely in the failure case: a well-meant `Database.Migrate()` added
later fails at the permission boundary instead of silently mutating a schema. Still worth doing,
still correctly low priority — it hardens a convention that currently holds.

**Analysis (BA).** Added **AC6** (no committed passwords — the ticket introduces a second
credential and said nothing about it) and **AC7** (the smoke tier still passes). AC4 remains the
subtle one and the Risks entry explaining `ALTER DEFAULT PRIVILEGES` is the most useful sentence
in the ticket.

**Engineering (ENG).** Implementable with the postgres image's init scripts plus grants in the
migration step. The bootstrapping-without-embedded-credentials concern is real and named.

**Architecture (ARCH).** No ADR-bar decision: [ADR-0003](../../architecture/adr/ADR-0003-initial-technology-stack.md)
already mandates the boundary; this changes only how it is enforced, from reviewed convention to
database privilege.

**QA.** Each criterion now has a decided tier, so no criterion is assigned to a tier that cannot
reach it — the mistake that produced T-0015 in the first place. AC1's mutation is specified,
because a permission test that passes for the wrong reason looks identical to one that passes.

**Security.** This *is* the security ticket: least privilege, [SECURITY.md](../../standards/SECURITY.md).
AC6 keeps the new credential out of the repository.

**Sizing.** Within the guideline, assuming the identity-role question is answered during
implementation rather than reopened as design.

- **Did:** Applied all six perspectives; found the identity-host role gap; added AC6 and AC7;
  assigned each criterion to the tier that can actually verify it.
- **Decided:** AC1–AC4 in the integration tier, AC5 and AC7 in the smoke tier.
- **Remaining:** implementation.
- **Open questions / blockers:** none blocking. The identity migrator's role is a design choice
  for the implementer, recorded so it is chosen rather than discovered.
- **DoR verdict:** **ready.**
- **Branch / PR:** n/a
- **Test state:** n/a — not started.
