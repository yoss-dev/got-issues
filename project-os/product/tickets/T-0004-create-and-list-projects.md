---
id: T-0004
title: Create and list projects
type: feature
status: committed
priority: high
owner: none
implemented_by: none
accepted_by: none
depends_on: [T-0002, T-0003, T-0009]
adrs: [ADR-0004, ADR-0003]
created: 2026-08-30
updated: 2026-08-31
---

# T-0004: Create and list projects

## Problem / Context

Promoted from [IDEA-001](../IDEAS.md). Projects are the top-level container in the product — issues live inside them, and nothing else in the domain can exist first. Until a project can be created and retrieved, there is no product, only infrastructure.

This is also the first *product* capability to go through the contract-first pipeline built in T-0002, so it doubles as the real-world test of that pipeline ([ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md)).

## Desired Outcome

An authenticated caller can create a project and retrieve the list of projects, through endpoints declared in `spec/openapi.yaml` and implemented behind generated contracts.

## User / Business Value

Sam gets the structure that a flat list cannot provide — work grouped by project rather than piled together. Priya gets the first addressable resource to build against. For the PoC, this is the first evidence that the contract-first method produces working product capability, not just scaffolding.

## Scope

### In Scope

- Specification of the project resource in `spec/openapi.yaml`: create and list operations, schemas, error responses, required scopes.
- **A project key**: short, uppercase, unique across the deployment, and **immutable once set**. It is the human-quotable half of every issue identifier (`GOTI-123`), so it is addressable in API paths.
- **Removal of T-0002's disposable placeholder resource** from the specification and of its generated output — projects is the first real resource, and the placeholder exists only until it arrives.
- Implementation behind the generated controller contracts.
- Persistence via EF Core, with the migration that introduces the projects table.
- Pagination on the list endpoint — mandatory per [ENGINEERING.md](../../standards/ENGINEERING.md).
- Validation of project input, declared in the specification (not only in code).
- Unit and integration tests per [TESTING.md](../../standards/TESTING.md), including the unauthenticated-caller case.

### Out of Scope

- Issues (T-0005) and anything that lives inside a project.
- Updating, archiving, or deleting projects — deliberately deferred until the questions in *Risks* are answered.
- Project membership or per-project permissions — roles are global, so no such concept exists ([GLOSSARY](../../governance/GLOSSARY.md)).
- Defining the role policies themselves — that is T-0009; this ticket only applies them.
- Any UI.

## Acceptance Criteria

- [ ] AC1: Given a caller holding the `admin` role, when they create a project with a valid name and key, then it is persisted and returned with both.
- [ ] AC1b: Given a key that is not short uppercase alphanumeric (as declared in the specification), when a project is created, then the API returns 400 with a problem document naming the key.
- [ ] AC1c: Given a key already in use, when a project is created with it, then the API returns 409 and no second project exists — keys are unique across the deployment.
- [ ] AC1d: Given an existing project, when any operation attempts to change its key, then the key does not change — it is immutable, because every issue identifier derives from it.
- [ ] AC2: Given a caller holding only the `member` role, when they attempt to create a project, then the API returns 403 and nothing is persisted — project creation is an admin act (`PROJECT.md` §5).
- [ ] AC2b: Given a caller of either role, when they request the project list, then it is returned — listing is not restricted.
- [ ] AC2c: Given an unauthenticated or invalid-token caller, when they attempt either operation, then the API returns 401 — distinct from the 403 of AC2.
- [ ] AC3: Given invalid input (as declared in the specification), when a project is created, then the API returns 400 with an `application/problem+json` body naming the offending field.
- [ ] AC4: Given more projects exist than one page holds, when the list is requested, then results are paginated and the response carries what a client needs to fetch the next page — no unbounded result set is ever returned.
- [ ] AC5: Given the specification, when `./tools/generate.sh` is run and the drift check follows, then the diff is empty — the endpoints were designed in the spec, not in the controller.
- [ ] AC6: Given the endpoints, when they are exercised by integration tests against real PostgreSQL, then behaviour matches what the specification declares.

## Examples / Scenarios

- An `admin` creates a project with key `GOTI`, then a `member` lists it: it appears with its key.
- Creating a second project with key `GOTI`: 409, one project remains.
- Key `goti` or `Got Issues!` or a 40-character key: 400, naming the key.
- Two simultaneous creates with the same key: exactly one succeeds.
- A `member` attempts to create one: 403, nothing written.
- Create with a missing or empty name: 400 with a problem document, not a 500.
- Create two projects with the same name: **behaviour undecided — see Risks.**
- List when no projects exist: empty page, 200, not 404.
- List with more items than the page size: pagination behaves and the caller can reach the rest.

## Technical Notes

The specification comes first: design the resource in `spec/openapi.yaml`, regenerate, then implement the generated interface. A controller declaring its own routes is a review rejection ([ENGINEERING.md](../../standards/ENGINEERING.md)).

## Dependencies

- **T-0002** — the contract-first pipeline must exist before a resource can be specified and generated.
- **T-0003** — the test harness, for AC6.
- **T-0009** — the `admin` policy must exist before creation can be restricted to it.

## Risks / Unknowns

- ~~**Project keys are undecided.**~~ **Settled by the PO, 2026-08-31: keys, and per-project issue numbering.** The consequence this ticket carries is **immutability** — once an issue is `GOTI-1`, renaming the project's key orphans every reference to it, in this system and in every commit message and chat log outside it. AC1d makes that a criterion rather than an assumption. Renaming, if it is ever wanted, is a separate ticket with a migration, not a field update.
- **Key uniqueness must hold under concurrency**, not merely be checked. Two simultaneous creates with the same key must not both succeed; a read-then-insert check without a unique constraint behind it will let them. The constraint is the guarantee, the check is the error message.
- **Archiving projects is out of scope but is an admin act** when it arrives (maintainer, 2026-08-30) — recorded so the follow-up ticket inherits the rule rather than rediscovering it.
- Name uniqueness is unspecified — duplicates allowed, or rejected? The *key* is unique (AC1c); whether two projects may share a display name is a separate and much less consequential question, and the ticket's default is that they may.
- This is the first real exercise of the generated `aspnetcore` contracts. If the output proves unworkable, [ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md) requires superseding rather than a quiet workaround, and this ticket's Work Log is where that evidence gets recorded.

## Testing Notes

Integration tests through `WebApplicationFactory` against PostgreSQL in Testcontainers; the 401 case in AC2 is required, not optional ([SECURITY.md](../../standards/SECURITY.md)). The drift check in AC5 is part of the suite.

## Relevant ADRs & Documentation

- [ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md) — contract-first pipeline
- [ENGINEERING.md](../../standards/ENGINEERING.md) — the contract-first rule and mandatory pagination
- [TESTING.md](../../standards/TESTING.md), [SECURITY.md](../../standards/SECURITY.md)
- [IDEA-001](../IDEAS.md) — the originating idea

## Definition of Ready

- [x] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — evaluated 2026-08-31 during `refinement-session`. All nine universal items hold; the one gap that would have blocked it (project keys) was answered live by the PO and is now AC1–AC1d. Conditional items: security — creation is an `admin` act with the negative case as a criterion (AC2); data-shape impact identified (key column, unique constraint, immutability); architectural questions resolved (ADR-0004 governs how the resource is specified and generated); no UX. No exceptions applied.

## Definition of Done

- [ ] Meets [DoD](../../governance/DEFINITION_OF_DONE.md) — checked by `complete-ticket`; deviations require recorded PO/human approval.

---

## Work Log

### 2026-08-30 — Product Owner (claude-sm-9d4e)

- **Did:** Created by promoting IDEA-001 during backlog seeding.
- **Decided:** Sliced vertically (one resource, end to end through the spec) rather than by layer, so each ticket delivers observable behaviour.
- **Remaining:** Refinement to Ready. Project keys and creation authorisation are the two decisions to settle there.
- **Open questions / blockers:** none blocking creation; `PROJECT.md` Q7 shapes AC2's precision.
- **Branch / PR:** n/a
- **Test state:** n/a — not started.

### 2026-08-30 — Product Owner (claude-sm-9d4e)

- **Did:** Applied the maintainer's Q7 answer. Project creation is now an `admin` act (AC1/AC2), listing stays open to any authenticated caller, and the 403-vs-401 distinction is explicit. Added T-0009 as a dependency.
- **Decided:** Kept policy *definition* in T-0009 and only its *application* here, so authorisation is defined once centrally rather than per endpoint.
- **Remaining:** Refinement to Ready; project keys remain the open decision.
- **Open questions / blockers:** none.
- **Branch / PR:** n/a
- **Test state:** n/a — not started.

### 2026-08-31 — Product Owner decision, transcribed by claude-sm-9d4e

Asked during refinement whether projects get Jira-style keys and issues per-project numbers, the maintainer (human PO) answered:

> "Keys and per-project numbers — GOTI-123"

Recorded per [WoW §13](../../governance/WAY_OF_WORKING.md) before being acted on. This settles the decision refinement had flagged as needing an answer *before* implementation, and it settles it for [T-0005](T-0005-create-and-read-issues.md) too — issue identity derives from it.

### 2026-08-31 — Business Analyst (claude-sm-9d4e) — refinement

Perspectives applied: Product Owner, Business Analyst, Software Engineer, Architect, QA, Security. (No UX — no user-facing UI.)

- **PO answered the blocking question live**, so this ticket did not have to park. Keys are now In Scope with four criteria: valid format, uniqueness, immutability, and the creation-role restriction that was already there.
- **BA/QA:** the interesting criterion is **AC1d, immutability**. It is easy to implement a key as an ordinary column and only discover the problem when someone renames one and orphans every `GOTI-*` reference — including references living outside this system entirely, in commit messages and chat. Making it a criterion rather than a note is the difference between a decision and an accident.
- **ENG:** added the concurrency risk. A read-then-insert uniqueness check without a database constraint behind it lets two simultaneous creates both succeed; the constraint is the guarantee and the check is only the error message. This is the kind of thing that passes every test written against a single-threaded harness.
- **ARCH:** no ADR bar reached — the resource is specified and generated under [ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md), and roles are settled in `PROJECT.md` §5.
- **Sizing:** grew by four criteria but they are all one resource's validation rules; still comfortably within the guideline.

**DoR verdict: `ready`.**
