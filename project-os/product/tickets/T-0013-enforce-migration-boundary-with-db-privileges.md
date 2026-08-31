---
id: T-0013
title: Enforce the migration boundary with database privileges, not convention
type: technical
status: backlog
priority: low
owner: none
implemented_by: none
accepted_by: none
depends_on: [T-0001]
adrs: [ADR-0003]
created: 2026-08-30
updated: 2026-08-30
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

## Testing Notes

AC1 is the point of the ticket and must be a real negative test — the API's connection attempting DDL and being refused. If [T-0003](T-0003-automated-test-harness.md) has landed, this belongs in the integration tier.

## Relevant ADRs & Documentation

- [ADR-0003](../../architecture/adr/ADR-0003-initial-technology-stack.md) — the explicit migration step
- [SECURITY.md](../../standards/SECURITY.md) — least privilege
- [T-0001](T-0001-runnable-compose-stack.md) — where the convention is established

## Definition of Ready

- [ ] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — not yet refined.

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
