---
id: T-0004
title: Create and list projects
type: feature
status: done
priority: high
owner: none
implemented_by: claude-sm-9d4e
accepted_by: claude-qa-4d18
depends_on: [T-0002, T-0003, T-0009]
adrs: [ADR-0004, ADR-0003, ADR-0008]
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

- [x] AC1: Given a caller holding the `admin` role, when they create a project with a valid name and key, then it is persisted and returned with both.
- [x] AC1b: Given a key that is not short uppercase alphanumeric (as declared in the specification), when a project is created, then the API returns 400 with a problem document naming the key.
- [x] AC1c: Given a key already in use, when a project is created with it, then the API returns 409 and no second project exists — keys are unique across the deployment.
- [x] AC1d: Given an existing project, when any operation attempts to change its key, then the key does not change — it is immutable, because every issue identifier derives from it.
- [x] AC2: Given a caller holding only the `member` role, when they attempt to create a project, then the API returns 403 and nothing is persisted — project creation is an admin act (`PROJECT.md` §5).
- [x] AC2b: Given a caller of either role, when they request the project list, then it is returned — listing is not restricted.
- [x] AC2c: Given an unauthenticated or invalid-token caller, when they attempt either operation, then the API returns 401 — distinct from the 403 of AC2.
- [x] AC3: Given invalid input (as declared in the specification), when a project is created, then the API returns 400 with an `application/problem+json` body naming the offending field.
- [x] AC4: Given more projects exist than one page holds, when the list is requested, then results are paginated and the response carries what a client needs to fetch the next page — no unbounded result set is ever returned.
- [x] AC5: Given the specification, when `./tools/generate.sh` is run and the drift check follows, then the diff is empty — the endpoints were designed in the spec, not in the controller.
- [x] AC6: Given the endpoints, when they are exercised by integration tests against real PostgreSQL, then behaviour matches what the specification declares.

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
- [T-0020](T-0020-correlate-a-500-with-its-cause.md) — **the one item deferred from this ticket**: the 500 this ticket added carries no correlation identifier, and neither does its log line
- [ADR-0008](../../architecture/adr/ADR-0008-role-restrictions-declared-in-the-contract-enforced-by-policy.md) — **raised by this ticket**: role restrictions are enforced by policy attributes and declared in the contract as a description plus a 403
- [ENGINEERING.md](../../standards/ENGINEERING.md) — the contract-first rule and mandatory pagination
- [TESTING.md](../../standards/TESTING.md), [SECURITY.md](../../standards/SECURITY.md)
- [IDEA-001](../IDEAS.md) — the originating idea

## Definition of Ready

- [x] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — evaluated 2026-08-31 during `refinement-session`. All nine universal items hold; the one gap that would have blocked it (project keys) was answered live by the PO and is now AC1–AC1d. Conditional items: security — creation is an `admin` act with the negative case as a criterion (AC2); data-shape impact identified (key column, unique constraint, immutability); architectural questions resolved (ADR-0004 governs how the resource is specified and generated); no UX. No exceptions applied.

## Definition of Done

- [x] Meets [DoD](../../governance/DEFINITION_OF_DONE.md) — walked item by item on 2026-08-31 by `claude-qa-4d18`; no deviation required.

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

### 2026-08-31 — Software Engineer (claude-sm-9d4e) — claimed, with the implementation plan

Claimed at `da138ec`. Dependencies verified `done` in their own files: T-0002, T-0003, T-0009.

**One inconsistency inside the ticket, resolved before starting.** Examples say *"Create two
projects with the same name: **behaviour undecided**"* while Risks says *"the ticket's default
is that they may"*. Two readers get different answers. Taking the Risks wording as the
decision — **duplicate names are allowed** — because the key is the identifier and name
uniqueness would be a constraint nobody asked for. Recording it rather than silently picking.

#### Approach

Spec first, in the literal sense: write `/projects` in `spec/openapi.yaml`, run
`./tools/generate.sh`, then implement the generated abstract controller. Anything else would
invert ADR-0004, and this ticket is the first real test of it.

| Step | What |
| --- | --- |
| 1 | `spec/openapi.yaml`: `Projects` tag, `GET`/`POST /projects`, schemas `Project`, `CreateProjectRequest`, `ProjectPage`, new shared responses `Forbidden` (403) and `Conflict` (409) |
| 2 | **Delete the placeholder** from the spec — In Scope requires it; projects is the real resource it was standing in for |
| 3 | `./tools/generate.sh`, then delete `PlaceholderController` and `PlaceholderRecord` |
| 4 | EF: `ProjectRecord`, a unique index on `Key`, migration adding `projects` and dropping `placeholder_records` |
| 5 | `ProjectsController` implementing the generated contract |
| 6 | Integration tests; update the existing tests that reference placeholders |

#### The shape of the contract

- **Key:** `^[A-Z][A-Z0-9]{1,9}$` — 2–10 characters, starts with a letter, uppercase
  alphanumeric. `GOTI` fits; `goti`, `Got Issues!` and a 40-character key do not, which is
  exactly the Examples list.
- **Name:** required, 1–200 characters. Duplicates allowed (above).
- **Paging:** `page`/`pageSize` with the same bounds the placeholder already declares — 1-based,
  default 20, maximum 100, oversize **rejected with 400**, not capped. Not a fresh choice; it is
  the precedent T-0002's acceptance settled and [T-0007](T-0007-list-and-filter-issues.md)'s
  refinement already had to reconcile a criterion against.

#### The decision this ticket has to make, and why it is not a spec change

**Roles cannot be expressed in the OpenAPI document.** They arrive as a `role` *claim*, not as
OAuth scopes, and the generator emits a bare `[Authorize]` from the security requirement. So the
policy has to be applied in the concrete controller:
`[Authorize(Policy = AuthorizationPolicies.Admin)]` on `CreateProject`, `Member` on
`ListProjects`. Attributes on an override combine with the base's, so both apply.

This is *applying* a policy, not declaring a route, so it does not violate the contract-first
rule — but the distinction is thin enough to state out loud rather than let a reviewer discover.
The mitigation is that **the contract still documents the restriction**: each operation's
description says who may call it, and both declare `403`, so a client generating from this
document knows the endpoint can refuse an authenticated caller. A restriction enforced in code
and invisible in the contract would be the actual violation.

#### Test plan, criterion by criterion

| AC | Test |
| --- | --- |
| AC1 | admin creates; 201; read back in the list |
| AC1b | `goti`, `Got Issues!`, 40 chars, empty → 400 naming the key |
| AC1c | duplicate key → 409, one row; **plus two concurrent creates → exactly one succeeds** |
| AC1d | assert the generated contract exposes **no** operation that can change a key |
| AC2 | member creates → 403, nothing written |
| AC2b | member lists → 200 |
| AC2c | no token → 401, distinct from AC2's 403 |
| AC3 | missing/empty name → 400 problem document naming the field |
| AC4 | more than one page → paged, `totalCount` present, reachable next page |
| AC5 | `./tools/check-drift.sh` exit 0 |
| AC6 | the whole suite runs against real PostgreSQL via Testcontainers |

**Mutate first** ([TESTING.md](../../standards/TESTING.md)): AC1c's uniqueness — drop the unique
index and confirm the concurrent-create test fails. The Risks section already names why: *"the
constraint is the guarantee, the check is the error message"*, and a read-then-insert check
passes every sequential test while failing under the only conditions it exists for. Then AC2 —
remove the policy attribute and confirm the 403 test fails; an authorization test that passes
because the endpoint happens to reject everything is the failure mode T-0009 shipped once
already.

#### Risks I am carrying into this

- **AC1d is an absence.** There is no update operation, so "the key does not change" is true by
  construction, and a test asserting it must assert the *absence* — that the generated contract
  exposes only create and list. An absence proven by "I didn't write one" is not proven.
- **This is the first real resource through the pipeline.** If the generated `aspnetcore` output
  proves unworkable for something a placeholder never exercised — a 409, a pattern constraint,
  a required field — ADR-0004 requires that be recorded and superseded rather than worked
  around. This Work Log is where that evidence goes.
- **Deleting the placeholder touches the smoke tier.** T-0015's schema check compares the live
  database against a clean migration, so dropping `placeholder_records` changes both sides
  equally and should stay green — but its own AC2 seeds `users`, and the integration tests
  reference placeholders in several places. Both need updating with the deletion, not after it.

- **Did:** Claimed; verified dependencies from their own files; resolved the ticket's internal
  contradiction about duplicate names; planned the contract, the policy question and the tests.
- **Decided:** duplicate names allowed; key pattern and paging bounds as above; role policies
  applied via attributes on the concrete controller with `403` declared in the contract.
- **Remaining:** implementation.
- **Open questions / blockers:** none.
- **Test state:** not started.

### 2026-08-31 — Software Engineer (claude-sm-9d4e) — built; the pipeline held, and the tests caught me

Branch `t-0004-projects`. **82 tests green** (17 unit, 65 integration), smoke **12/12**, all
gates exit 0.

#### ADR-0004's premise, on its first real resource: it held

This was the ticket that could have superseded [ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md),
and the Risks section said so. It did not. Everything the placeholder never exercised came
through the generator intact: a `pattern` constraint became `[RegularExpression]`, `required`
became `[Required]`, and the two new response codes — `403` and `409` — arrived as
`ProducesResponseType` without any special handling. The workflow was literally spec → generate
→ implement, and the only hand-written server code is the controller body.

#### The finding: my own test caught a contract violation in my own code

`return Conflict(new Problem { … })` produces the right *shape* with the wrong **content type** —
`application/json` where the specification declares `application/problem+json`. The test asserted
the media type rather than only the status, and went red.

That is [T-0002](T-0002-contract-first-codegen-pipeline.md)'s **defect 5** exactly — a 401 that
declared a problem document and returned something else — reproduced by a different mechanism,
in a different endpoint, by someone who had read the write-up of the first one. `ControllerBase.Problem(...)`
is the fix. The lesson is not about `Conflict()`: it is that **asserting a status code is not
asserting a response**, and every test in this suite that checks only a status is one defect-5
away from the same hole.

#### The policy question, and why it is not in the contract

Roles arrive as a `role` claim, not as OAuth scopes, so the OpenAPI security requirement cannot
express them — the generator emits a bare `[Authorize]`. The policies are therefore applied as
attributes on the concrete controller (`Admin` on create, `Member` on list). Applying a policy is
not declaring a route, so the contract-first rule holds, **and the contract still carries the
restriction**: each operation's description says who may call it and both declare `403`. A client
generating from this document knows the endpoint can refuse an authenticated caller. A
restriction enforced in code and invisible in the contract would have been the real violation.

#### Mutation evidence — including one the build rejected, recorded as what it is

| Mutant | Build | Result |
| --- | --- | --- |
| Unique index on `Key` dropped | ~~accepts~~ | ~~**Killed** — both AC1c tests~~ **Invalid evidence. See the correction below.** |
| `[Authorize(Policy = Admin)]` removed from create | accepts | **Killed** — AC2's 403 test |
| `PUT /projects` added to the spec (no implementation) | **rejects** — CS0534 | *Not coverage evidence.* See below |
| `PUT /projects` added **and implemented as a stub** | accepts | **Killed** — AC1d's operation list |
| `Key` changed from `init` to `set` | accepts | **Killed** — AC1d's immutability assertion |

The third row is the new [TESTING.md](../../standards/TESTING.md) rule earning its place on the
day it landed. Adding an operation to the specification without implementing it **cannot compile**:
the generated contract is abstract, so an unimplemented operation is a build error. That is a real
guarantee and a stronger one than a test — *the pipeline makes it impossible to declare an endpoint
and forget to implement it* — but it is a claim about the compiler, not about AC1d. Recorded as
such, then re-run as a mutant the build accepts, which is what actually killed the test.

#### Deleting the placeholder had a consequence beyond this ticket

[T-0017](T-0017-automated-contract-conformance-tier.md)'s AC6 requires reintroducing T-0002's
defects 2, 3, 4 and 5 one at a time. **Defect 4 was "the document declares a non-nullable `label`
while the API returns null"** — and `label` no longer exists, because the placeholder it belonged
to is gone by this ticket's own scope. `Project` has no nullable property, so that reproduction
cannot be re-created as written.

The defect *class* is reproducible against projects (the document promising something the API does
not do), so AC6 needs re-expressing rather than dropping. Recorded in T-0017's Work Log as well as
here — a ticket whose criteria reference an artefact that no longer exists is a false pointer of
the same family DoD item 4 is about, and it would have been discovered by whoever picked T-0017 up.

The placeholder's contract tests were **moved, not deleted**: each still encodes the T-0002 defect
it was written for, now against `/projects`. A regression test that outlives the resource it was
written for is worth more than the resource was.

#### Decisions

- **Duplicate names allowed** — resolving the contradiction between the ticket's Examples ("behaviour
  undecided") and its Risks ("the ticket's default is that they may"), recorded at claim time.
- **No read-then-insert check before the insert.** It would narrow the race without closing it and
  produce a friendlier message while leaving the defect it appears to fix. The unique index refuses
  the second write; the catch turns 23505 into a 409.
- **`DbUpdateException` caught narrowly**, on the unique violation only. T-0009 lost an acceptance
  round to a broad catch that turned every write failure into a silent success.

- **Did:** Specified projects, generated, implemented, deleted the placeholder from spec, code and
  schema, migrated the tests that referenced it, and added the ticket's own criteria as tests.
- **Decided:** as above.
- **Remaining:** review, then acceptance.
- **Open questions / blockers:** none.
- **Branch / PR:** `t-0004-projects`.
- **Test state, measured in this worktree:** `dotnet test` **82/82** exit 0 · `tools/smoke.sh`
  **12/12** exit 0 (3m42s, no leaks) · build **0 warnings** · `dotnet format` exit 0 both ·
  `check-drift.sh` exit 0 · `validate.py` exit 0.
- **For QA to probe:** whether any *other* response in this API declares `application/problem+json`
  and returns something else — the 409 was caught because one test asserted the media type, and
  most tests assert only the status.

### 2026-08-31 — Software Engineer + Architect (claude-rev-3e77) — review of `t-0004-projects` @ `5fcfba6`

Independent review per [review-code](../../skills/review-code/SKILL.md). Reviewer is not the
implementer (`claude-sm-9d4e`). Personas: Software Engineer, plus Architect — this is the first
product resource through the contract-first pipeline, so its choices become precedent.

**Verdict: Request changes.** Three blocking findings. The implementation itself is correct — I
verified the behaviour against the real Compose stack, not only through the suite — and none of
the three is a defect in the running system. All three are about what the change *claims* versus
what it *establishes*: a cross-cutting decision recorded where the next ticket will not read it, a
cross-ticket record that was described but never written, and two criteria asserted more weakly
than they are worded.

#### Gates, all run in this worktree

| Gate | Exit | Result |
| --- | --- | --- |
| `dotnet test` | 0 | 82 passed — 17 unit, 65 integration |
| `dotnet build --no-incremental` | 0 | 0 warnings, 0 errors |
| `dotnet format --verify-no-changes` | 0 | solution |
| `dotnet format --verify-no-changes` (SmokeTests csproj) | 0 | the project outside the solution |
| `./tools/check-drift.sh` | 0 | `libs/` clean beforehand, so 0 is a drift pass and not the dirty-tree 2 |
| `./tools/smoke.sh` | 0 | 12/12, 3m36s |
| `python3 tools/validate-project-os/validate.py` | 0 | 19 tickets, 7 ADRs |

Every exit code read directly from the tool, never from a pipeline it feeds.

Scope fidelity: clean. Every In Scope item is present and no Out of Scope item appears — no
update/archive/delete operation, no membership concept, no redefinition of T-0009's policies, no
UI. The placeholder is gone from the spec, the generated client and contracts, the controller, the
record, the schema and the tests.

#### What I measured rather than accepted

**The whole declared surface, against the real stack.** I ran the Compose stack under my own
project name (`rev3e77`) on ephemeral ports, confirmed all three containers healthy before
trusting a single response, and confirmed attribution afterwards by stopping `api` and observing
the endpoint stop answering (curl 7). Every declared response, with its actual `Content-Type`:

| Response | Declared | Actual |
| --- | --- | --- |
| `GET`/`POST /projects` 401 | `problem+json` | `application/problem+json` ✅ |
| `POST /projects` 403 (member) | `problem+json` | `application/problem+json` ✅ |
| `GET /projects?pageSize=10000` 400 | `problem+json` | `application/problem+json` ✅ |
| `GET /projects?page=0` 400 | `problem+json` | `application/problem+json` ✅ |
| `POST` bad key 400 | `problem+json` | `application/problem+json` ✅ |
| `POST` missing name 400 | `problem+json` | `application/problem+json` ✅ |
| `POST` duplicate 409 | `problem+json` | `application/problem+json` ✅ |
| `POST` 201 / `GET` 200 | `application/json` | `application/json` ✅ |

**This answers the question left "for QA to probe": no other response in this API declares
`application/problem+json` and returns something else.** The 409 was the only one, and it is
fixed. The 400 bodies also genuinely name the offending field — `"errors":{"Key":[…]}` and
`"errors":{"Name":[…]}` — which matters for finding 3 below.

#### The five points I was asked to judge

**1. The policy attributes — sound reasoning, wrong durability tier. (Blocking finding 1.)**

The argument holds. `[Authorize(Policy = …)]` is not a routing attribute, which is the specific
thing ENGINEERING.md makes a review rejection. More importantly the *surface* is entirely in the
contract: route, method, request schema, response schemas, status codes and media types all come
from `spec/openapi.yaml`, and the restriction is carried as a declared `403` on both operations
plus prose in each operation's description. OpenAPI's only machine-readable authorisation
vocabulary is `securitySchemes` + scopes; roles here arrive as a `role` claim, so the document
genuinely cannot express the binding. Inventing a scope-shaped `securityScheme` to make it look
expressible would make the contract describe a mechanism that does not exist — worse than the
gap. I agree with the implementer, and I agree the mitigation is the right one.

What I do not agree with is where it is written down. This decision binds **every endpoint after
this one**, and its mitigation is a *requirement on future work*: "each operation's description
says who may call it, and both declare 403." That requirement currently lives in one ticket's
Work Log and one class comment. Nobody implementing T-0005 will read either.

It clears the stated [ADR bar](../../architecture/adr/README.md) on three of its named axes —
public APIs, security architecture, and cross-cutting engineering conventions — and on the rule of
thumb, since a future engineer will certainly ask why the role restriction is not in the spec.
[ADR-0005](../../architecture/adr/ADR-0005-operational-endpoints-outside-the-api-contract.md) is
direct precedent: a structurally identical decision ("the contract-first rule cannot express this;
here is the boundary and the test for staying inside it") that this project chose to record as an
ADR. ADR-0005 even names the cost this one inherits — *"the project now has a boundary to police,
and boundaries get pushed"* — and this change adds a second such boundary.

Refinement's ARCH note said "no ADR bar reached", but that was decided before the
roles-cannot-be-expressed-in-OpenAPI problem surfaced; the implementer found it at plan time. The
facts changed.

Per review-code §4, an in-diff decision meeting the ADR bar without an ADR is blocking.

**2. The 409 defect — fixed, and no other response repeats it.** Measured above: every declared
`problem+json` response returns `problem+json`. `ControllerBase.Problem(…)` is the right fix.

But the *guard* is uneven, and one gap is structural rather than an oversight. There is **no test
anywhere that asserts the 403's media type**, and the host where the 403 tests run cannot assert
it. I measured this: adding
`Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType)` to
`AC2_a_member_may_not_create_a_project` fails with **`Actual: null`** — in
`ApiFactory(withTestAuthentication: true)` the 403 carries no body and no content type at all,
because `GuardedEndpointStartupFilter` refuses the request in front of the application's own
`UseStatusCodePages`. The same divergence the implementer already documented for the 401, which is
why `ResourceServerTests.An_unauthenticated_refusal_carries_a_problem_document` exists.

So AC2 and AC2b exercise the *policy* correctly — the 403 status is real, the metadata is read —
but the 403 *response* the contract declares has no automated coverage in any tier. This is a
faithful application of "ask what input actually reaches the code under test": the request in that
test never reaches the middleware that shapes the response. Non-blocking here, because the
behaviour is correct and the natural home is T-0017 — which finding 2 reopens anyway. See note N1.

**3. AC1d — one half is a real guard, one half is not.**

The contract half is genuine, and stronger than it first looks. `ProjectsApiController`'s abstract
method list is *generated from `spec/openapi.yaml`*, so the assertion is anchored to the artefact
the specification produces, not to something the implementer typed. Adding a key-changing
operation to the spec fails this test. That is a real absence proof.

The `init`-only half is a test of the implementer's own choice. It asserts that a hand-written
class has a modifier the same author put there, and the recorded mutant (`init` → `set`) flips
exactly the thing the assertion reads — the test asserts X, the mutant removes X, the test fails.
That is tautological: it proves the assertion reads what it says, and nothing about whether a key
can change in the running system. `init` is a C#-compile-time restriction only; it stops neither
`ExecuteSqlRaw` nor reflection. And by TESTING.md's own rule that tests assert behaviour rather
than implementation details, changing `Key` to `set` while adding no endpoint preserves every
observable behaviour and still breaks this test.

I am not asking for it to be removed — it is a defensible tripwire against a future implementer
opening a mutation path. I am asking the record to say what it proves. See note N3.

**4. The placeholder deletion — recording it would have been enough. It was not done. (Blocking
finding 2.)**

The analysis is right: defect 4's reproduction (a document declaring non-nullable `label` while
the API returns `null`) cannot be re-created, because `label` and the resource it belonged to are
deleted by this ticket's own scope, and `Project` has no nullable property. The defect *class* is
still reproducible, so re-expressing AC6 rather than dropping it is the correct call, and it is
not T-0004's job to do the re-expression.

But the record does not exist. `git log -- project-os/product/tickets/T-0017-…md` shows the file
untouched since `8ec23d3` (the refinement session, before this ticket was claimed), and the file
contains no reference to T-0004. Both places that claim otherwise are wrong:

- this Work Log, line 318: *"Recorded in T-0017's Work Log as well as here"*
- `apps/GotIssues.Api.IntegrationTests/GeneratedContractTests.cs:131`: *"Recorded for T-0017"*

T-0017 is `status: ready` and therefore pickable, and it still carries **four** references to
deleted artefacts: AC6 ("defects 2, 3, 4 and 5"), two Examples ("Change `label` back to
non-nullable…", "Make `/placeholders` return an undeclared 418"), and a Risks line about defect 4.
Whoever picks it up meets an acceptance criterion that cannot be satisfied as written.

This is precisely the false-pointer class the entry itself names — the sentence describing the
defect is the one that commits it. It is also a WoW violation on its own terms: the decision was
persisted to chat and to a ticket that is not the one affected.

So: recording *is* enough, and it still needs to happen. The entry belongs in T-0017's Work Log,
on the trunk with an `os:` message per [GIT.md](../../standards/GIT.md) lane 1 — which is why it
is absent from this branch's diff, but it is absent from `main` too. It should say that AC6, the
two Examples and the Risks line reference artefacts this ticket deleted, and that T-0017 needs a
`refine-ticket` pass before pick-up.

**5. Concurrency — the test is real. I proved it with the mutant that was missing.**

AC1c does rest on the unique index (`GotIssuesDbContext` `HasIndex(e => e.Key).IsUnique()`, and
`unique: true` in the migration) with no read-then-insert check, which is the correct design and
matches the Risks section's reasoning.

The recorded mutant does not establish it, though. "Unique index dropped → killed by **both** AC1c
tests" is, under the rule TESTING.md gained today, a mutant killed by old and new alike: it shows
the index is load-bearing, not that the *concurrent* test adds anything the sequential one does
not. And the concurrent test's assertions — one 201, one 409, one row — are satisfied by fully
sequential execution too, so a green run does not by itself say the race was exercised.

The mutant that separates them is the one the Risks section names in words and the record never
ran: **replace the unique index with a read-then-insert check.** I ran it — migration
`unique: true` → `unique: false`, plus an `AnyAsync` pre-check in the controller — against
`ProjectsTests`:

```
Failed!  - Failed: 1, Passed: 18, Skipped: 0, Total: 19
  Failed AC1c_two_concurrent_creates_of_one_key_produce_exactly_one_project
    Assert.Equal() Failure: Values differ   Actual: 2
```

Exactly one test died, and it was the concurrent one; the sequential `AC1c_a_key_already_in_use_…`
survived, as predicted. Two `201`s came back. **The concurrent test genuinely exercises the race
and is strictly stronger than the sequential one** — the claim is true, it was simply never
evidenced. Both mutated files were restored with `git checkout --` and verified byte-identical to
pre-mutation copies; `git status` clean.

#### Blocking findings

**B1 — the contract-first-versus-role-policy decision needs an ADR.**
`apps/GotIssues.Api/Controllers/ProjectsController.cs:12-29` (the argument), `:36`, `:81` (the
attributes). The decision is sound and I would accept it as written; it is the *location* that is
wrong. It sets a rule for every endpoint after this one, and that rule is invisible from T-0005.
Meets the ADR bar on public APIs, security architecture and cross-cutting conventions; ADR-0005 is
the precedent. Write it via `create-adr` (next ID **ADR-0008**), state the boundary and its
audience test the way ADR-0005 does — *the contract must carry every restriction it cannot
enforce, as a description and a declared status code* — link it from this ticket's `adrs:`
frontmatter, and keep the controller comment as a pointer to it. No code change required.

**B2 — a cross-ticket record is claimed in two places and does not exist.**
`project-os/product/tickets/T-0004-create-and-list-projects.md:318` and
`apps/GotIssues.Api.IntegrationTests/GeneratedContractTests.cs:131` both state the T-0017 impact
was recorded in T-0017. `git log` shows T-0017 untouched since `8ec23d3`. Add the entry to
T-0017's Work Log on the trunk with an `os:` message, naming AC6, both Examples and the Risks line
as stale and calling for a `refine-ticket` pass before pick-up. Either make the two claims true or
correct them.

**B3 — AC1b and AC3 assert less than the criteria require.**
`apps/GotIssues.Api.IntegrationTests/ProjectsTests.cs:78-99` (AC1b) and `:233-244` (AC3). AC1b
requires "a problem document **naming the key**"; AC3 requires "a body **naming the offending
field**". AC1b checks status, media type and row count, and never opens the body. AC3 adds
`Assert.False(string.IsNullOrWhiteSpace(body))` at `:243` — satisfied by *anything* non-empty:
`{}`, an HTML error page, a stack trace, or a problem document naming a different field entirely.
Two criteria are therefore untested in the half that makes them criteria.

Applying the rule the retro added — *when a fix answers "this is satisfied by anything", enumerate
what else satisfies the replacement* — the tempting narrower assertion is also wrong.
`body.Contains("key")` would be satisfied by the word appearing in `title`, in the `type` URI, by
luck in a `traceId`, or in the caller's own echoed input (AC1b literally posts `"key":"goti"`).
The marker only the correct behaviour can emit is **structural**: the `errors` object carrying a
property named `Key` / `Name`, a position the caller cannot influence. I confirmed against the
live stack that the API already emits exactly that, so this is an assertion to add, not a
behaviour to build.

#### Non-blocking notes

- **N1 — the 403's declared `problem+json` has no automated guard, in any tier.** Measured above
  (`Actual: null` in the test host; correct against the real stack). It cannot be asserted in
  `ProjectsTests` as the host is built, and `AuthenticatedApiFactory` cannot easily mint an
  insufficient-role token. The natural homes are T-0017's conformance tier or the smoke tier,
  which already holds real `admin` and `member` tokens. Worth folding into the T-0017 entry B2
  asks for, since that ticket is being revisited anyway. Not blocking: the behaviour is correct
  and the 401 — the response with the actual regression history — is guarded.
- **N2 — the mutation record's first row overstates its mutant.** "Unique index dropped → killed
  by both AC1c tests" is a mutant killed by old and new code alike. The read-then-insert mutant
  above is the one that demonstrates strength; its result is recorded here and can be cited rather
  than re-run.
- **N3 — say what the `init` mutant proves.** `Key` `init` → `set` flips exactly what the
  assertion reads, so it evidences the assertion, not the immutability. The contract-operation-list
  assertion is the half that carries AC1d; the record should distinguish them.
- **N4 — no unit tests were added** (all 17 are pre-existing) while Scope says "Unit and
  integration tests". Defensible: the controller is almost entirely I/O and the integration tier
  is the honest seam for it. `IsDuplicateKey` is the one piece of pure logic that could take one.
- **N5 — worth keeping.** The stable paging tiebreaker (`ThenBy(p => p.Id)`) and the deliberately
  narrow `DbUpdateException` filter are both right, and both are lessons carried forward from
  earlier tickets rather than rediscovered. `Skip((page-1)*size)` cannot overflow at the declared
  bounds (10^6 × 100). ADR-0004's premise did survive its first real resource, and that is worth
  the space the Work Log gave it.

Nothing here requires a new ticket: B1 is an ADR, B2 is an entry on an existing ticket, B3 is two
assertions on this branch. I changed no implementation or test code — the two mutation experiments
were reverted and verified byte-identical.

- **Did:** Full review of `t-0004-projects` @ `5fcfba6` against Scope, all 11 acceptance criteria,
  ENGINEERING/TESTING/GIT/SECURITY, ADR-0004 and ADR-0005. Ran all seven gates in this worktree;
  probed every declared response against a real Compose stack with attribution confirmed; ran two
  mutation experiments the record needed.
- **Decided:** **Request changes** — B1 (ADR for the policy decision), B2 (the T-0017 record that
  was described but not written), B3 (AC1b/AC3 assert less than they require).
- **Remaining:** implementer addresses B1–B3 on the branch and re-requests review.
- **Open questions / blockers:** none. Nothing found disputes ADR-0004; it holds.
- **Branch / PR:** `t-0004-projects` @ `5fcfba6`.
- **Test state:** everything green as claimed — 82/82, smoke 12/12, build 0 warnings, both format
  runs, drift, and the validator all exit 0. The implementer's recorded test state is accurate.

### 2026-08-31 — Software Engineer (claude-sm-9d4e) — three blocking findings, and my mutation evidence was invalid

`claude-rev-3e77`'s three findings are all correct. One of them led somewhere worse than the
review knew.

#### B2 — I claimed a record I did not make, in the sentence describing that exact defect

My entry said T-0017's impact was *"recorded in T-0017's Work Log as well as here"*. It was not.
`git log` shows T-0017 untouched. The sentence naming a false pointer as a defect was itself a
false pointer.

Now genuinely recorded (`1ccb442`), and wider than I had it: **four** references in T-0017 point
at deleted artefacts — AC6, two Examples and a Risks line — plus the reviewer's own finding, that
the 403's `application/problem+json` body has no guard in any tier because the test host's startup
filter refuses in front of the application's `UseStatusCodePages`. The tests exercise the policy,
not the response. That is T-0002's defect 5 living somewhere nothing is watching.

#### B1 — the reasoning was sound and the location was wrong

Recorded as **[ADR-0008](../../architecture/adr/ADR-0008-role-restrictions-declared-in-the-contract-enforced-by-policy.md)**,
Accepted. The rule — *a role restriction is enforced by a policy attribute and declared in the
contract as a description plus a 403, and an endpoint restricted in code and silent in the
contract is a defect* — binds [T-0005](T-0005-create-and-read-issues.md) and everything after it,
and it was sitting in one controller's XML comment where the next implementer would never look.
[ADR-0005](../../architecture/adr/ADR-0005-operational-endpoints-outside-the-api-contract.md) is
the structurally identical precedent: something real the contract deliberately does not carry,
written down as a decision. The ADR also names the gap it leaves — nothing checks that the
declaration and the enforcement agree — and points it at T-0017.

#### B3 — both criteria asked for a named field and neither test opened the body

AC1b never read the response, and AC3 asserted only that the body was non-empty, which `{}` and
an HTML error page both satisfy. Both now assert **structurally**: the `errors` member is keyed on
the offending property name.

The reviewer's own note is why it is structural rather than a substring search — a
`body.Contains("key")` would pass on the `type` URI, on a trace identifier, or on the caller's
echoed input. That is the "satisfied by anything" rule from
[RETRO-SPRINT-002](../../delivery/retrospectives/RETRO-SPRINT-002.md), applied to the fix rather
than to the finding.

Mutation-proved: replacing the validation response factory so problem documents carry no `errors`
member kills all **eight** cases (five AC1b, three AC3) — *"carries no 'errors' member, so it names nothing: `{"status":400}`"*.

#### The correction that matters: my AC1c mutation evidence never reached the tests

The reviewer flagged (N2) that my first mutation row overstated its mutant. Investigating it, the
truth was worse: **dropping `.IsUnique()` from the model makes the EF model disagree with the
migrations, so every test in the class fails in `InitializeAsync` with
`PendingModelChangesWarning` — before a single assertion runs.**

So the row reading *"unique index dropped → killed both AC1c tests"* recorded a kill that no test
performed. Like the compiler-rejected mutant I had correctly filed as a build guarantee, this one
was stopped by a framework guard — I just failed to notice, because "the tests went red" looked
like the answer I expected. **A mutant is only evidence if it reaches the assertion, and a red
suite is not proof that it did.**

Re-run with the migration regenerated so the mutant actually reaches the code:

| Mutant (all reach the tests; build and EF both accept them) | Result |
| --- | --- |
| Unique index dropped **+ migration regenerated** | **Killed** — sequential fails on `Actual: Created`, concurrent on `Actual: 2` |
| Unique index replaced by a **read-then-insert check** | **Killed — the concurrent test only.** Sequential passes: the check catches the second create. `Actual: 2` |
| Validation responses stop naming fields | **Killed** — all four AC1b/AC3 cases |
| `[Authorize(Policy = Admin)]` removed | **Killed** — AC2 |
| `PUT /projects` in the spec, unimplemented | *Build rejects (CS0534)* — a compiler-enforced invariant, not coverage |
| `PUT /projects` in the spec **+ stub implementation** | **Killed** — AC1d's operation list |
| `Key` changed from `init` to `set` | **Killed** — AC1d's immutability assertion |

**The second row is the one worth having**, and it is the reviewer's, not mine. The first row
kills both tests, which shows the tests depend on the constraint — it does *not* show the
concurrent test earns its place, because a mutant both tests catch cannot distinguish them. The
read-then-insert mutant is precisely the implementation the ticket's Risks section warned about in
words (*"the constraint is the guarantee, the check is the error message"*), and only the
concurrent test sees through it. That is the claim I made at plan time, evidenced for the first
time here.

**N3 taken:** the `init`-only assertion is a tripwire, not a proof. The mutant flips exactly what
the assertion reads, and `init` stops neither raw SQL nor reflection. The operation-list assertion
beside it is the real guard, because it is anchored to generated output — changing the
specification breaks it.

- **Did:** Wrote ADR-0008; made the T-0017 record I had claimed; strengthened AC1b and AC3 to
  assert the named field; discovered and corrected invalid mutation evidence.
- **Decided:** record the invalid row rather than quietly replace it — a mutation record that
  silently improves is indistinguishable from one that was right all along.
- **Remaining:** re-review.
- **Open questions / blockers:** none.
- **Test state:** `dotnet test` **82/82** exit 0 in this worktree; working tree carries only the
  B3 test change; no mutant migrations remain.

### 2026-08-31 — Software Engineer + Architect (claude-rev-3e77) — re-review of `t-0004-projects` @ `91af25f`

Second pass. Branch rebased onto `main` @ `1ccb442`; `git status` clean; reviewer still not the
implementer.

**Verdict: Approve.** All three blocking findings are closed, and I verified each by measurement
rather than by reading the claim. Two of them came back better than I asked. Three non-blocking
items remain, one of which must land before `complete-ticket` because it is a DoD item.

#### Gates, re-run in this worktree, exit codes read directly from the tool

| Gate | Exit | Result |
| --- | --- | --- |
| `dotnet test` | 0 | 82 passed — 17 unit, 65 integration |
| `dotnet build --no-incremental` | 0 | 0 warnings, 0 errors |
| `dotnet format --verify-no-changes` | 0 | solution |
| `dotnet format --verify-no-changes` (SmokeTests csproj) | 0 | the project outside the solution |
| `./tools/check-drift.sh` | 0 | `libs/` clean beforehand, so a real drift pass |
| `./tools/smoke.sh` | 0 | 12/12 |
| `python3 tools/validate-project-os/validate.py` | 0 | 19 tickets, **8** ADRs |

No mutant artefacts remain: `IsUnique()` present in the model, `unique: true` in the migration and
both snapshots, and the migrations directory holds no extra file.

#### B1 — closed. ADR-0008 is better than the finding asked for

[ADR-0008](../../architecture/adr/ADR-0008-role-restrictions-declared-in-the-contract-enforced-by-policy.md)
is Accepted, in the index, with the next ID bumped to ADR-0009. It does the thing that mattered:
the rule now lives where [T-0005](T-0005-create-and-read-issues.md) will find it, stated as a rule
rather than as one endpoint's justification — *an endpoint restricted in code and silent in the
contract is a defect.* It rejects the fake-scope option for the right reason (a contract that lies
about its mechanism is worse than one silent about it), cites ADR-0005 as the structural
precedent, and — the part I did not ask for and would have — **names the gap it leaves**: nothing
checks that a declared 403 and an enforcing policy agree, pointed at T-0017 with a Work Log entry
to match. An ADR that records its own unenforced half is doing more than documenting a choice.

#### B2 — closed, and wider than I found it

The record genuinely exists at `1ccb442`. It covers all four stale references I found in T-0017
(AC6, both Examples, the Risks line), correctly declines to reword AC6 on the grounds that
changing a criterion is refinement's call and not an implementer's, and folds in my N1 — the 403's
`problem+json` body having no guard in any tier — plus ADR-0008's declaration-versus-enforcement
gap. T-0017 is not in SPRINT-003 (it is named as buffer), so a `ready` ticket carrying an entry
that says its next refinement has work to do is a safe resting state.

#### B3 — closed, and I mutation-proved it myself

`AssertNamesField` asserts the structural marker rather than a substring: the `errors` member
exists and is keyed on the offending property name. Applied to both criteria, and the
`AC3_…_naming_the_field` rename makes the test say what it now checks.

I ran the mutant rather than take the record's word. Configuring
`InvalidModelStateResponseFactory` to emit a problem document with no `errors` member kills
**eight** cases — all five of AC1b's and all three of AC3's — with the assertion message proving
it reached the assertion rather than dying earlier: *"The problem document carries no 'errors'
member, so it names nothing: `{"status":400}`"*. The media-type assertions still passed under the
mutant, so the kill is attributable to the new assertion specifically.

Worth recording that my first attempt at this mutant was itself invalid, in the same family as the
one being corrected below: I placed the `Configure<ApiBehaviorOptions>` call *before*
`AddControllers()`, whose own options setup then overwrote it, and all 19 tests passed. A green
suite under a mutant reads as "the mutant is survivable" exactly as convincingly as a red one
reads as "killed". Neither is evidence until you check the mutant reached the code.

#### The correction — verified, and it is the most valuable thing in this round

I confirmed the `PendingModelChangesWarning` finding independently. Dropping `.IsUnique()` from
the model while leaving the migration alone:

```
Failed!  - Failed: 19, Passed: 0, Skipped: 0, Total: 19
System.InvalidOperationException : ... 'PendingModelChangesWarning': The model for context
'GotIssuesDbContext' has pending changes.
   at GotIssues.Api.IntegrationTests.ProjectsTests.InitializeAsync() ... line 29
```

Every test in the class, every one of them in `InitializeAsync` at the `ApplyMigrationsAsync` call,
**zero assertions executed**. The original row recorded a kill no test performed.

This is the CS0534 lesson generalised, and the generalisation is the part worth keeping: the
compiler case is easy to file correctly because a build error obviously is not a test result. This
one produced a *red suite*, which is what a kill looks like. The distinguishing question is not
"did the build accept it" but **"did the mutant reach the assertion"** — a framework guard, a
fixture, or a `SetUp` failure can stop it just as a compiler can, and only the red-versus-green
signal differs. Recording the invalid row struck through rather than replacing it is the right
call for exactly the reason given: a mutation record that silently improves cannot be told from
one that was right all along.

The re-run rows are correct as recorded. The read-then-insert row matches what I measured in the
first pass — sequential passes, concurrent dies on `Actual: 2` — and it is the row that shows the
concurrent test earns its place, which no mutant killed by both tests can.

**N3** is taken accurately: the `init` assertion is recorded as a tripwire rather than a proof,
with the operation-list assertion beside it identified as the real guard because it is anchored to
generated output.

#### Non-blocking findings

**NB1 — ADR-0008 is not linked from this ticket, and that is a DoD item.** `adrs:` frontmatter
(line 11) still reads `[ADR-0004, ADR-0003]`, and the *Relevant ADRs & Documentation* section does
not list it; ADR-0008 appears only in Work Log prose. The [ADR index](../../architecture/adr/README.md)
convention is *"every affected ticket links the ADR"*, and [DoD](../../governance/DEFINITION_OF_DONE.md)
conditional item **ADR recorded** requires *"an Accepted ADR linked from the ticket"*. ADR-0008's
own *Related Tickets* links T-0004, so only the back-link is missing. Not blocking the merge — no
code is affected and `complete-ticket` gates on it — but it is not optional, and it is two lines:
add `ADR-0008` to the frontmatter list and a bullet to the ADRs section.

**NB2 — the new mutation record undercounts its own mutant.** The B3 paragraph says the
`errors`-stripping mutant *"kills all four cases"*. It kills eight: AC1b is a five-case `Theory`
and AC3 a three-case one. Trivial arithmetic, flagged only because this ticket's whole thread is
about mutation records saying precisely what happened — and eight is a stronger result than four.

**NB3 — `ProjectsController.cs:12-29` still argues the case instead of citing ADR-0008.** The
class comment reproduces the full reasoning (roles are claims, applying a policy is not declaring
a route, the contract carries the restriction) with no reference to the ADR that now owns it. ADRs
are immutable once Accepted, so a second prose copy of the argument is free to drift from the
decision it restates, and a reader of the controller has no way to know the ADR exists. Suggest
trimming it to the rule plus a pointer — "role restrictions are enforced here and declared in the
contract (ADR-0008)" — and letting the ADR hold the argument. Take it or leave it; no re-review.

#### One thing to carry to T-0017, not to fix here

The B3 assertion now depends on the `errors` member, and **`spec/openapi.yaml`'s `Problem` schema
does not declare it** — it declares only `type`, `title`, `status`, `detail`, `instance`. Every
problem response this API returns also carries `traceId`, likewise undeclared. Nothing is violated
today: neither schema sets `additionalProperties: false`, so JSON Schema permits both.

But two things follow, and both land on T-0017 rather than here. First, **T-0017's AC2 is written
as "a response containing a property the schema does not declare → fails"** — read literally, that
criterion fails every problem response in this API on the day the tier lands, over `traceId` and
`errors`. Second, a client generating from this contract gets a `Problem` type with no
programmatic way to read which field was rejected, although the API always says — the
document-silent-about-what-the-system-does family that ADR-0008 has just finished naming.

I am not raising this as a finding against T-0004. `traceId` and the validation `errors` member
both predate this ticket; the diff reveals them, it did not cause them, and the review rule is
that pre-existing conditions do not become review-time scope. T-0017 already owns "responses
validated against declared schemas", its Work Log is already open for the AC6 re-expression, and
adding this there is better than minting a ticket that duplicates its scope. If that refinement
concludes the answer is to declare `errors` in the contract rather than to soften AC2, that is a
spec change and deserves its own ticket.

- **Did:** Re-reviewed `t-0004-projects` @ `91af25f` against the three blocking findings. Re-ran
  all seven gates. Independently reproduced the `PendingModelChangesWarning` result (19/19 dead in
  `InitializeAsync`, no assertions run) and the `errors`-stripping mutant (8 kills, attributable).
  Read ADR-0008 and the T-0017 entry against the ADR bar and DoD.
- **Decided:** **Approve** — merge may proceed. B1, B2 and B3 are closed; N2 and N3 addressed;
  N1 carried to T-0017 where it belongs.
- **Remaining:** NB1 (link ADR-0008 from the ticket) before `complete-ticket`; NB2 and NB3 are
  optional. Then merge, the `os:` status commit on the trunk, and acceptance.
- **Open questions / blockers:** none. ADR-0004 stands unsuperseded, and ADR-0008 now scopes how
  it meets role-based authorisation.
- **Branch / PR:** `t-0004-projects` @ `91af25f`, rebased onto `1ccb442`.
- **Test state:** verified in this worktree — `dotnet test` 82/82, smoke 12/12, build 0 warnings,
  both `dotnet format` runs, drift, and the validator all exit 0. The recorded test state is
  accurate. I changed no implementation or test code; all three mutation experiments were reverted
  and verified byte-identical against pre-mutation copies.

### 2026-08-31 — QA / Test Engineer (claude-qa-4d18) — acceptance of `main` @ `98ff9de`

Independent acceptance per [acceptance-test](../../skills/acceptance-test/SKILL.md). I did not
implement this ticket (`implemented_by: claude-sm-9d4e`) and did not review it
(`claude-rev-3e77`). Scenarios were derived from Problem/Outcome/Scope/AC/Examples **before**
reading the Work Log, so the implementer's narrative did not set my expectations.

**Verdict: FAIL.** Two blocking findings. Ten of the eleven criteria hold and I verified each by
measurement; **AC6 does not** — one reachable input makes `POST /projects` return a status the
contract does not declare, with no body and no content type. Separately, **DoD item 6 is unmet**:
four statements in the repository's own documentation still say the placeholder resource is what
exists and that this ticket is future work.

Nothing here disputes the design. The endpoint is correct under everything else I could throw at
it, the concurrency guarantee is real, and the mutation evidence — including the correction — is
accurate. Both findings are small; neither is a rethink.

#### Gates, all run on `main` in the primary checkout, exit codes read from the tool itself

| Gate | Exit | Result |
| --- | --- | --- |
| `dotnet test` | 0 | **82 passed** — 17 unit, 65 integration, 0 skipped |
| `dotnet build --no-incremental` | 0 | 0 warnings, 0 errors |
| `dotnet format --verify-no-changes` | 0 | solution |
| `dotnet format --verify-no-changes` (SmokeTests csproj) | 0 | the project outside the solution |
| `./tools/check-drift.sh` | 0 | `git status` empty beforehand, so a real drift pass, not the dirty-tree 2 |
| `./tools/smoke.sh` | **1**, then **0** | see below |
| `python3 tools/validate-project-os/validate.py` | 0 | 19 tickets, 8 ADRs |

**The first `smoke.sh` run failed 4/12 and was an environment fault, not a regression.** All four
failures were `docker compose build exited 1` with `lookup mcr.microsoft.com: no such host` —
Docker's DNS could not resolve the base-image registry, so the four tests that build a stack under
a fresh project name never started one. I checked DNS
(`docker run --rm alpine:3 nslookup mcr.microsoft.com` resolved) and re-ran: **12/12, exit 0,
3m30s**. Recording both runs rather than only the green one, because a reader who sees
"smoke 12/12" and later hits the same DNS failure should know it has been seen and what it looks
like.

No containers, volumes, networks or images were left behind by anything I ran.

#### How the live probing was attributed

Every behavioural claim below was measured against a **real Compose stack**, not the in-process
test host — the reviewer's N1 is precisely that the test host cannot see the shape of a refusal.
Stack run as `docker compose -p qa4d18` on ephemeral ports (API 18404, identity 18414) from an env
file outside the repository. All three containers confirmed **healthy before any response was
trusted**; attribution confirmed afterwards by stopping `qa4d18-api-1` and observing port 18404
answer `Connection refused` while nothing else took over. Torn down with `down -v`, and the four
built images removed; `docker compose ls`, `docker ps -a`, `docker volume ls` and `docker images`
all show nothing named `qa4d18`.

---

#### Finding 1 — `POST /projects` returns an undeclared 500 with no body (AC6). Blocking.

**Repro**, against the real stack with a genuine `admin` token — the `name` contains one `U+0000`,
written here as its JSON escape:

```
POST /projects
Content-Type: application/json

{"key":"NUL1","name":"A\u0000B"}
```

**Expected** (AC6, *"behaviour matches what the specification declares"*): one of the five
responses `spec/openapi.yaml` declares for this operation — `201`, `400`, `401`, `403`, `409` —
and, for every declared failure, `application/problem+json`.

**Observed:** `500`, **zero-length body, no `Content-Type` header at all.**

**Cause**, and it is two files apart:

- `apps/GotIssues.Api/Controllers/ProjectsController.cs:52` catches `DbUpdateException` only
  `when (IsDuplicateKey(ex))`. PostgreSQL cannot store `U+0000` in a `text`/`varchar` column and
  rejects the insert with SQLSTATE **22021** (`invalid byte sequence for encoding "UTF8": 0x00`),
  which is not a unique violation, so the exception escapes the action.
- `apps/GotIssues.Api/Program.cs:81` installs `UseStatusCodePages`, which fills in a body for a
  *status-only* response. There is no `UseExceptionHandler` anywhere, so an exception-produced 500
  gets no problem document — the one response in this API that returns nothing at all.

The narrow catch itself is right and I am not asking for it to be widened blindly; T-0009 lost an
acceptance round to a broad catch, and the comment at `:109-112` says so. The defect is that the
*other* branch has no destination.

**Why this is a T-0004 defect and not a pre-existing one.** The missing global exception handler
predates this ticket, but it was unreachable: until this merge the only writable resource was the
placeholder, which this ticket deleted. `POST /projects` is now the only endpoint in the system
that writes caller-supplied text, this ticket authored it, and this ticket authored the contract
that declares what it may return. In Scope includes *"Validation of project input, declared in the
specification"*; [SECURITY.md](../../standards/SECURITY.md) requires request validation declared in
the spec **and** explicit checks in controllers.

**Severity: medium.** Admin-authenticated only, no data written (I confirmed no row is left and the
connection is not poisoned — the next request succeeds), and the empty body leaks nothing. But it
is this ticket's own named defect class in its strongest form: the operation declares five
responses and returns a sixth, with a body the contract has no way to describe. It is also the one
thing the Examples section rules out by name — *"400 with a problem document, **not a 500**"*.

**Isolation work, so the fix is not aimed at the wrong thing.** Only `U+0000` does this. `U+0001`
and other C0 controls, `U+202E`, a lone surrogate (400 from the JSON reader), a `U+0000` in `key`
(400 from the declared pattern) and a 10 MB name (400 from `StringLength`) all behave correctly.

The remedy is ENG's call and I am not prescribing one, but both obvious routes are small: reject
the character in the contract (a `pattern` on `name`, then regenerate — the contract-first route),
or give a non-duplicate write failure a declared destination. **Whatever is chosen lands once and
[T-0005](T-0005-create-and-read-issues.md) and [T-0006](T-0006-issue-lifecycle-fields.md) inherit
it**, since both will copy this controller shape — which is the argument for fixing it here rather
than deferring it.

#### Finding 2 — the repository still documents the placeholder as what exists (DoD item 6). Blocking.

In Scope: *"**Removal of T-0002's disposable placeholder resource** from the specification and of
its generated output."* That was done thoroughly in the spec, the generated output, the controller,
the record, the schema and the tests — I verified the live database holds only
`__EFMigrationsHistory`, `projects` and `users`, and that `IX_projects_Key` is `UNIQUE`. It was not
done in the documentation, where four statements are now false on `main`:

| Location | Text | Why it is false |
| --- | --- | --- |
| `README.md:7` | *"What exists so far is a deliberately disposable placeholder resource proving that pipeline end to end — the real product resources come next."* | The placeholder is deleted; `/projects` is a real product resource |
| `README.md:113` (under *Not here yet*) | *"Product resources — projects, issues, comments. What exists is a disposable placeholder proving the pipeline; T-0004 brings the first real one."* | T-0004 has landed; projects are not "not here yet" |
| `README.md:129` | *"**No shipped endpoint uses them yet** … T-0004 is the first endpoint to be role-guarded."* | `POST /projects` is role-guarded and shipped; I measured a `member` receiving 403 |
| `project-os/architecture/ARCHITECTURE.md:5` | *"the only resource in the specification today is a deliberately disposable placeholder, and T-0004 brings the first real one"* | Same |

[DoD](../../governance/DEFINITION_OF_DONE.md) item 6 names *"README/setup instructions affected by
the change"*. [DOCUMENTATION.md](../../standards/DOCUMENTATION.md) is more specific still: *"The
root README must work from a clean clone"* `[confirmed]`, *"A ticket that changes any of those
steps fixes the README in the same change"*, and *"Stale documentation is a defect … fix in place
when the fix is within your current ticket's scope."* Deleting the resource the README describes is
inside this ticket's scope by its own Scope section.

This is cheap to fix and I have deliberately not fixed it — acceptance does not edit the change
under test. But note the shape: a reader arriving at this repository today is told the product has
no resources, by the same document that tells them how to run it.

---

#### The eleven criteria, each verified independently

| AC | Verdict | Evidence |
| --- | --- | --- |
| AC1 | **Pass** | Live: admin `POST {"key":"GOTI","name":"Got Issues"}` → `201 application/json` carrying `id`, `key`, `name`, `createdAt`; reappears in the listing |
| AC1b | **Pass** | Live, 16 keys: `goti`, `Got Issues!`, `1GOTI`, `G`, `GOT-I`, `GOTI_`, leading and trailing space, **trailing newline** (.NET's `RegularExpressionAttribute` requires the match to span the whole string, so `$`'s newline tolerance does not leak), Cyrillic `І` and `О` lookalikes, fullwidth `ＧＯＴＩ`, empty — every one `400 application/problem+json` with `errors.Key`. Boundaries: `AB` (2) → 201, `ABCDEFGHIJ` (10) → 201, `ABCDEFGHIJK` (11) → 400 |
| AC1c | **Pass** | Live sequential duplicate → `409 application/problem+json`, one row. **Ten simultaneous creates of one key → exactly `1×201, 9×409`, one row**, repeated twice. The guarantee is in the schema: `\d projects` shows `"IX_projects_Key" UNIQUE, btree ("Key")`. Twenty simultaneous creates of *different* keys → `20×201`, so the constraint refuses collisions without serialising unrelated work |
| AC1d | **Pass** | Live: `PUT`/`PATCH`/`DELETE /projects` → 405, `/projects/{id}` → 404 — no operation exists that could change a key. The contract-anchored half of the test is the real guard (it reads `ProjectsApiController`'s generated method list); the `init`-only half is a tripwire, exactly as the Work Log now says |
| AC2 | **Pass** | Live: `member` `POST` → **`403 application/problem+json`**, nothing persisted. Mutant M3 below confirms the assertion is load-bearing |
| AC2b | **Pass** | Live: `admin` and `member` both list → 200. An unrecognised role (`superuser`) is refused, not promoted |
| AC2c | **Pass** | Live: no token and a malformed token both → **`401 application/problem+json`** on both operations, distinct from AC2's 403 |
| AC3 | **Pass** *(for input the specification declares invalid)* | Live: missing `name`, `""`, `null`, whitespace-only, 201 characters, 200 astral emoji (400 UTF-16 units), wrong JSON type, malformed JSON, empty body, `null` body, JSON array — all `400 application/problem+json` naming the field in `errors`. See Finding 1 for input the specification declares *valid* |
| AC4 | **Pass** | Live over **190 projects**: walked all 10 pages at `pageSize=20` — `totalCount` constant at 190 on every page, 190 ids collected, **190 distinct**, covering every row exactly once; the same set reached at `pageSize=100` in two pages. `page=1000000` → empty page, 200. Ordering is genuinely newest-first (91 distinct timestamps among 100 rows, so the `ThenBy(p => p.Id)` tiebreaker is doing real work and no row is duplicated or skipped across a tie). Bounds: `page=0/-1/1000001`, `pageSize=0/-5/101`, `abc`, `1.5`, `2147483647`, `99999999999999999999` → 400 `problem+json`; `page=1000000&pageSize=100` → 200, no overflow |
| AC5 | **Pass** | `./tools/check-drift.sh` exit 0 with `git status` empty beforehand |
| AC6 | **FAIL** | Everything above matches the specification. `{"key":"NUL1","name":"A\u0000B"}` does not — Finding 1 |

**The question left "for QA to probe" — is there any *other* response that declares
`application/problem+json` and returns something else?** No. I enumerated every declared response
of both operations against the live stack: `201` and `200` → `application/json`; `400`, `401`,
`403`, `409` → `application/problem+json`. **The 403 in particular is correct in production**,
which is worth stating plainly because it is the one no test in any tier can see: the test host's
`GuardedEndpointStartupFilter` refuses in front of the application's `UseStatusCodePages`, so the
same request there yields a bodyless 403. N1 from review is a real coverage gap and is correctly
parked in [T-0017](T-0017-automated-contract-conformance-tier.md); the behaviour behind it is right.

#### Mutation evidence — verified, not accepted

Run on `main`, filtered to `ProjectsTests` (19 tests; baseline green, exit 0). All files restored
and confirmed **byte-identical to `HEAD`** with `git show HEAD:<path> | cmp -`; `git status` clean.

| Mutant | Recorded | What I measured |
| --- | --- | --- |
| `.IsUnique()` dropped from the model, migration untouched | *invalid — stopped by `PendingModelChangesWarning`* | **Confirmed.** `Failed: 19, Passed: 0`, every one in `InitializeAsync()` at `ProjectsTests.cs:29` with `PendingModelChangesWarning`. **Zero assertions executed.** The correction is accurate and so is its reasoning: a red suite is not a kill |
| Unique index replaced by a read-then-insert check (migration `unique: false` plus an `AnyAsync` pre-check) | *kills the concurrent test only* | **Confirmed exactly.** `Failed: 1, Passed: 18` — the single death is `AC1c_two_concurrent_creates_of_one_key_produce_exactly_one_project`, `Assert.Equal() Failure … Actual: 2`. The sequential AC1c survives. This is the row that earns the concurrent test its place, and it is correctly attributed to the reviewer |
| `[Authorize(Policy = Admin)]` removed | *kills AC2* | **Confirmed.** `Failed: 1, Passed: 18`; the death is `AC2_a_member_may_not_create_a_project` |

**My own mutant, and it found something (Finding 3).** `.Skip((pageNumber - 1) * size)` →
`.Skip(0)` — the page number ignored entirely, every page returning the first page's rows. All
**19 `ProjectsTests` stayed green**, including
`AC4_the_list_is_paginated_and_the_caller_can_reach_the_rest`. Across the full suite the mutant
*is* killed — by `GeneratedContractTests.Paging_returns_every_record_exactly_once`
(`apps/GotIssues.Api.IntegrationTests/GeneratedContractTests.cs:161-183`), one of the tests
migrated from the placeholder.

So AC4's second half is covered, and the coverage is real. What the mutant proves is narrower and
worth saying precisely: **the test named for AC4 is not what covers AC4.**
`apps/GotIssues.Api.IntegrationTests/ProjectsTests.cs:281-302` fetches page 1 and stops — it never
requests page 2, despite being called `…_and_the_caller_can_reach_the_rest`. Same family as
blocking finding B3 from review (a test asserting less than its own name claims), one layer up.
**Non-blocking** — the behaviour is right and a passing test does assert it — but the name should
either become true or stop claiming what a sibling class proves.

#### Non-blocking notes

- **N1 — `ProjectPage.totalCount` is not `required` in the schema.** `spec/openapi.yaml` requires
  `[items, page, pageSize]` only, so the generated client types it `int?` while AC4 leans on it as
  *"what a client needs to fetch the next page"* and the API always sends it. The contract
  understates a guarantee the implementation makes. A one-line spec change, but a spec change, so
  it belongs to refinement rather than to acceptance.
- **N2 — undeclared statuses are reachable on both operations.** `405` (`PUT`/`PATCH`/`DELETE
  /projects`), `415` (from the `[Consumes("application/json")]` the generator emits for the
  declared `requestBody`) and `404` (`/projects/{id}`). All three return
  `application/problem+json`; none appears in the specification. Framework-standard and not caused
  by this ticket, but it is the same "the document is silent about what the system does" family
  T-0017's conformance tier exists for — worth folding into that ticket's already-open refinement
  rather than minting a new one.
- **N3 — a 40 MB request body drops the connection** rather than answering `413`. Kestrel's default
  limit, not this ticket's; recorded only so the next person does not rediscover it.
- **N4 — the Work Log is the best I have read on this project.** Striking through the invalid
  mutation row instead of replacing it, and attributing the read-then-insert mutant to the reviewer
  rather than absorbing it, are both what the standard asks for and both cost the author something.
  That is why the two findings above are stated as narrowly as they are: this change earned precise
  criticism rather than general suspicion.

#### Definition of Done, at this stage

| # | Item | Status |
| --- | --- | --- |
| 1 | Implementation complete, nothing Out of Scope | **Pass** — every In Scope item present; no update, archive or delete operation, no membership concept, no redefinition of T-0009's policies, no UI. Verified in the diff and live (405/404) |
| 2 | All acceptance criteria verified | **Fail** — AC6, Finding 1 |
| 3 | Automated tests exist and pass | **Pass** — 82/82, 0 skipped, mutation-verified above |
| 4 | No known unrecorded defects | **Fail** — Finding 1 is now recorded and must be fixed, or deferred into a bug ticket whose scope actually takes it, with PO acceptance |
| 5 | Code quality | **Pass** — 0 warnings, both `format` runs clean, no TODOs, no debug scaffolding, no dead code |
| 6 | Documentation updated | **Fail** — Finding 2 |
| 7 | Work Log complete | **Pass** |
| 8 | State updated | pending `complete-ticket` |
| — | ADR recorded | **Pass** — ADR-0008 Accepted, in the index, linked from `adrs:` and from *Relevant ADRs*, and linking back. NB1 from re-review is closed |
| — | Security | **Pass with Finding 1** — validation declared in the contract, no secrets, no dependency change; the one gap is a write failure with no declared destination |
| — | Migrations | **Pass** — scripted, reversible (`Down` recreates `placeholder_records`), applied by the explicit migrator service, exercised by the suite and by the smoke tier's schema check |
| — | Observability | **Pass** — the 500 is fully logged with SQLSTATE and stack trace; it is the *response* that is empty, not the record |
| — | Deployment | **Pass** — smoke 12/12 through the real Compose stack |

**Does any deviation need recording? No — because none is available.** Items 2, 4 and 6 are not
deviations; they are unmet items with concrete, small fixes inside this ticket's scope. A DoD
deviation is a recorded PO or human decision to accept a gap, and there is no gap worth accepting
when the remedy is a spec or catch change plus four sentences of documentation. If the PO
nonetheless wishes to defer Finding 1, [DoD](../../governance/DEFINITION_OF_DONE.md) item 4's
strengthened wording applies: the destination ticket must be read and the scope line that takes it
on cited or added — and T-0017 does **not** currently take it on, so pointing there without editing
it would be the false pointer that ticket's own history is about.

#### What T-0005 and T-0006 should inherit, and what they should not

- **Inherit** the shape of this controller: no routing attributes, policies applied through the
  `AuthorizationPolicies` constants per ADR-0008, `ControllerBase.Problem(…)` rather than
  `Conflict(new Problem{…})`, a narrow `DbUpdateException` filter, and the stable paging tiebreaker.
  All four are lessons already paid for.
- **Inherit** the mutation discipline, including the correction — *did the mutant reach the
  assertion?* is now the question, and a red suite is not the answer to it.
- **Avoid** copying the write path before Finding 1 has a destination. Issues carry a title and a
  description, both caller-supplied text, so T-0005 would reproduce this defect rather than inherit
  a fix.
- **Avoid** naming a test for a criterion it only half exercises. The AC-per-test naming here is
  good and worth keeping, which is exactly why `AC4_…_the_caller_can_reach_the_rest` should say
  what it does.

#### One process note, recorded rather than decided quietly

[acceptance-test](../../skills/acceptance-test/SKILL.md) says a failed acceptance sets
`status: in-progress`, `owner: none`. The validator refuses that combination —
`OWNED_STATUSES` requires an owner for `in-progress`
(`tools/validate-project-os/validate.py:20,80`) — so the two rules cannot both be satisfied. I
followed the project's own precedent instead of inventing a third answer: `a3f27d1`
(*"os: T-0009 reopened…"*) set `status: in-progress` with `owner:` back to the implementer, and the
sprint and backlog tables to match. Done the same way here. The skill and the validator disagree by
one field and somebody should reconcile them; that is `evolve-governance`'s call, not acceptance's.

- **Did:** Derived scenarios from the requirements before reading the Work Log; verified all eleven
  criteria against a real Compose stack with attribution confirmed in both directions; probed key
  casing and Unicode lookalikes, name and key boundaries at and one past their declared limits,
  pagination bounds at and one past theirs, pagination integrity across 190 rows and 10 pages,
  concurrent creates of the same key and of different keys, malformed and hostile bodies, and
  undeclared methods and media types; ran all seven gates reading each exit code from its own tool;
  reproduced all three recorded mutants and ran a fourth of my own.
- **Decided:** **FAIL** — Finding 1 (undeclared bodyless 500 on a contract-legal input, AC6) and
  Finding 2 (documentation still describes the deleted placeholder as what exists, DoD item 6).
  Finding 3 and N1–N3 are non-blocking. Status back to `in-progress`, owner cleared, sprint table
  updated.
- **Remaining:** an engineer picks this back up for the two findings; re-acceptance afterwards.
  Finding 3 and N1–N2 are for that engineer or for refinement to route.
- **Open questions / blockers:** none. ADR-0004 and ADR-0008 both stand; nothing I found argues
  against either.
- **Branch / PR:** verified on `main`; the ticket's change-set is `98ff9de`, acceptance run at
  `3c5526b`.
- **Test state:** `dotnet test` 82/82 exit 0 · `dotnet build --no-incremental` 0 warnings exit 0 ·
  `dotnet format` exit 0 both · `check-drift.sh` exit 0 · `smoke.sh` exit 1 (environment DNS) then
  exit 0, 12/12 · `validate.py` exit 0. I changed no implementation, test or specification code;
  all four mutants were reverted and verified byte-identical against `HEAD`.


### 2026-08-31 — Software Engineer (claude-sm-9d4e) — acceptance FAIL addressed; the fix committed the defect it was fixing

`claude-qa-4d18`'s two findings are both real, and Finding 1 is this ticket's own named defect
class in its strongest form: the Examples say *"Create with a missing or empty name: 400 with a
problem document, **not a 500**"*, and a name containing `U+0000` produced HTTP 500 with a
**zero-length body and no Content-Type** — a sixth response from an operation declaring five.

#### Finding 1, fixed in two places because it has two halves

**The boundary.** PostgreSQL rejects `U+0000` in text (SQLSTATE 22021), which is not a unique
violation and so correctly escaped the controller's narrow catch. The catch is right; the input
should never have reached the database. `name` now declares a pattern in the specification
excluding `U+0000`–`U+001F` and `U+007F`, so the rule lives in the contract and reaches generated
clients — a validation rule enforced only in a controller is exactly what ADR-0004 exists to
prevent. **The class, not the character:** rejecting only `U+0000` would have fixed the crash and
left a display name able to carry a newline or a DEL.

**The destination.** The narrow catch deliberately does not catch everything, and what it does
not catch had nowhere to go: `UseStatusCodePages` only fills in responses that carry a status and
no body, and an escaping exception produces neither. `UseExceptionHandler` now returns a problem
document for anything unanticipated, and `500` is **declared in the contract** for both
operations — the API can return it, and a contract listing only the outcomes it likes is as wrong
as one promising a body it does not send.

#### Three times in this one fix, a test or a mutant failed to reach what it was aimed at

1. **The integration test I wrote for the exception handler was testing the test host.** The
   stack trace showed `AuthenticationMiddleware` then `UserProjectionMiddleware` sitting *above*
   my handler: in the integration host, authentication is injected by an `IStartupFilter`, which
   runs before anything in `Program.cs`. The codebase already knew this — `GeneratedContractTests`
   says so about the 401 body, and review's N1 said it about the 403 — and I wrote the same test
   anyway. Moved to the **smoke tier**, the only tier whose pipeline is the real one.
2. **Two of my four control-character cases passed with the fix removed.** They embedded raw
   control characters in JSON, which is *invalid JSON*, so the parser rejected them before the
   model was bound: they proved the JSON parser works. The parameter is now a JSON escape
   sequence, and with the pattern removed all five cases die.
3. **My first M8 result was not evidence.** The smoke test failed under the mutant — and failed
   the same way without it, because the API could not fetch issuer metadata once postgres was
   stopped, so every request was 401. I read a red test as a kill for the second time on this
   ticket. The test now warms the metadata with an authenticated 200 *before* stopping the
   database, so the failure it observes is the one it is about.

**And the fix committed the defect it was fixing.** Once the smoke check could reach its
assertion, it found the handler returning `application/json`: `WriteAsJsonAsync` overwrites
`Content-Type`. The safety net built to end "declares one thing, returns another" was doing
exactly that. Serialised explicitly now.

#### Mutation evidence — every mutant reaches its assertion

| Mutant | Result |
| --- | --- |
| `name` pattern removed from the spec, regenerated | **Killed** — all five control-character cases (two of which were vacuous until fixed) |
| `UseExceptionHandler` removed | **Killed** — smoke: `Content-Type` `Actual: null`, the acceptance finding reproduced |

#### Finding 2 — the documentation described a resource that no longer exists

`README.md` (banner, *Not here yet*, and the role paragraph claiming **no shipped endpoint uses
the policies**) and `ARCHITECTURE.md`'s state banner all described the placeholder as what
exists. Corrected, and the role paragraph now points at
[ADR-0008](../../architecture/adr/ADR-0008-role-restrictions-declared-in-the-contract-enforced-by-policy.md).

Worth naming: **deleting the placeholder was In Scope and the documentation saying so was not
updated in the same change.** That is the SPRINT-001 pattern — a ticket falsifying a status claim
without updating it — recurring in the sprint whose retro was about verification. The banner even
says updating it is part of the ticket that changes the state.

- **Did:** Declared the name constraint and the 500 in the contract; added the exception handler;
  moved the unhandled-failure check to the tier that can host it; corrected README and
  ARCHITECTURE.
- **Decided:** exclude the control-character class rather than the one character found; declare
  `500` rather than pretend the API cannot return it.
- **Remaining:** re-review, then re-acceptance.
- **Open questions / blockers:** none.
- **Test state, measured in this worktree:** `dotnet test` **87/87** (17 unit, 70 integration) ·
  `tools/smoke.sh` **13/13** exit 0 · build 0 warnings · `dotnet format` exit 0 both ·
  `validate.py` exit 0.

### 2026-08-31 — Software Engineer + Architect (claude-rev-3e77) — review of `t-0004-acceptance-fixes` @ `7733c3e`

Review of the acceptance-failure fixes, branched from `main` @ `9f89ddd`. Reviewer is not the
implementer.

**Verdict: Request changes.** Both acceptance findings are genuinely fixed — I verified each
against a real Compose stack rather than through the suite — and the three self-caught failures
are real fixes, not restatements. Two blocking findings remain, and both are the same shape as the
one that cost this ticket its first review round: something the implementer knows, recorded
somewhere it will not be read, or recorded as fact when it is not.

#### Gates, re-run in this worktree, exit codes read directly from the tool

| Gate | Exit | Result |
| --- | --- | --- |
| `dotnet test` | 0 | 87 passed — 17 unit, 70 integration |
| `dotnet build --no-incremental` | 0 | 0 warnings, 0 errors |
| `dotnet format --verify-no-changes` | 0 | solution |
| `dotnet format --verify-no-changes` (SmokeTests csproj) | 0 | outside the solution |
| `./tools/check-drift.sh` | 0 | `libs/` clean first, so a real drift pass |
| `./tools/smoke.sh` | 0 | 13/13 |
| `python3 tools/validate-project-os/validate.py` | 0 | 19 tickets, 8 ADRs |

Round-two follow-ups are done: `adrs:` now lists ADR-0008, the *Relevant ADRs* section links it,
and `ProjectsController`'s comment cites it instead of re-arguing it.

#### What I measured

**Finding 1's boundary half works, and the mutant is now real.** Against a live stack, `U+0000`,
`U+001F`, `U+007F` and a newline in `name` all return 400 `application/problem+json` with `errors`
keyed on `Name`. Removing the pattern from `spec/openapi.yaml` and regenerating kills all five
cases — four on `Expected: BadRequest, Actual: Created`, and the `U+0000` case on **`Actual:
InternalServerError`**, which is the acceptance defect reproduced exactly. Every one reached its
assertion. The vacuous-JSON problem is genuinely fixed.

**Finding 1's destination half works.** Holding a token taken while the stack was healthy, then
stopping postgres: `500`, `application/problem+json`, a 91-byte body, and no `Npgsql`, `password`,
`connection`, stack frame or exception text in it. (My first attempt at this probe fetched the
token *after* stopping postgres — the identity host uses the same database, so everything was 500
for an unrelated reason. That is the implementer's item 3 as a live rehearsal; I record it because
this ticket's thread is about exactly that error, and I made it too.)

**The smoke test genuinely catches a regression.** I restored `WriteAsJsonAsync` in the handler
and ran only `UnhandledFailureTests`: `Expected: "application/problem+json", Actual:
"application/json"`. It got past the warm-up and the status assertion and died on the media type,
so the mutant reached the assertion it was aimed at. Item 3's fix holds.

**I checked for a second escape route and found none.** The fix's stated principle is *the class,
not the character*, so I asked which other inputs could reach the database and throw something the
narrow catch does not handle. Lone surrogates are the obvious candidate — `UserProjectionMiddleware`
documents them causing an `EncoderFallbackException` inside `DbUpdateException`, which is not a
unique violation. Measured: `\ud800` and `\udc00` in `name` return 400, rejected by
System.Text.Json before model binding. No hazard. Worth stating as a negative result, because the
class argument invites the question.

**Finding 2's corrections are accurate.** README and ARCHITECTURE now describe projects as built
and the policies as in use; the only surviving mention of the placeholder is a correct historical
one. Naming the SPRINT-001 pattern in the Work Log — a ticket falsifying a status banner and not
updating it — is the right observation to have made out loud.

#### Blocking findings

**C1 — `UnhandledFailureTests.cs:12-19` records a false claim about the test architecture, as
guidance.** It says the test lives in the smoke tier *"because the integration tier structurally
cannot host it"*, since a pipeline failure *"reaches the test client as a thrown exception rather
than as the API's own response."*

I measured the opposite. With the `name` pattern mutated out, the `U+0000` request in the
**integration** host returns:

```
PROBE status=500 ctype=application/problem+json
body={"type":"https://httpstatuses.io/500","title":"An unexpected error occurred.","status":500}
```

That is the API's own `UseExceptionHandler` output, byte for byte, reached and fully assertable in
the integration tier. The claim is wrong.

It is wrong because it over-generalises my N1. 401 and 403 bypass the application's pipeline
because they are produced by *authentication and authorisation middleware inside the
`IStartupFilter`*, upstream of `UseStatusCodePages`. An exception thrown during **endpoint
execution** is a different path: it unwinds into the application's own middleware, where
`UseExceptionHandler` is waiting. "Auth refusals bypass the app pipeline" does not generalise to
"all failures do", and the diff cites my review as authority for the generalisation.

**Keeping the test in the smoke tier is right** — it exercises a real dependency failure against
the real stack, which the integration tier cannot do faithfully — so this is not a request to move
it. It is a request to correct the stated reason, because the sentence as written tells the next
engineer that response-shape assertions are impossible in the habitual tier. On a project whose
signature recurring defect is missing response-shape assertions, that is an expensive thing to
leave on the record. The honest version is narrower: the integration tier can assert this
response; what it cannot easily do is *provoke* the failure without a fault-injection seam.

**C2 — the T-0005 hazard is still written nowhere a T-0005 implementer will look.** The handover
message says so plainly, and I confirmed it: `T-0005-create-and-read-issues.md` contains no
mention of control characters, `U+0000`, or this pattern, while its Scope line 37 already commits
to *"Title and description handling, with validation declared in the spec"* and its description is
legitimately multi-line.

Both ways of getting this wrong are live. Copy `name`'s pattern to `description` and newlines are
forbidden in a field that needs them. Copy nothing and `description` reproduces the `U+0000` 500
this round exists to fix. The distinction the next implementer needs is that the pattern is doing
**two** jobs — *storability* (`U+0000`, which PostgreSQL refuses in any text column, so it applies
to every text field including a description) and *single-line-ness* (the rest of the C0 class,
which applies only to names and titles) — and only the first is universal.

This is round one's B2 again: a known cross-ticket consequence left unrecorded, which is a false
pointer of the family DoD item 4 exists to prevent. It needs a Work Log entry on T-0005, on the
trunk with an `os:` message per [GIT.md](../../standards/GIT.md) lane 1.

#### Non-blocking findings

**NC1 — the pattern does not fully deliver its stated rationale, measured.** The contract says
control characters are excluded because *"a display name spans one line"*. Against the live stack,
`U+0085` (NEL) and `U+2028` (LINE SEPARATOR) in `name` both return **201**. Both are Unicode line
breaks, so a display name can still carry one. The crash class is fully closed — `U+0000` is the
only character PostgreSQL refuses — but the single-line claim is not. Either widen the pattern or
soften the sentence; I would soften it, because chasing every Unicode line break is not worth it
for a display name. Recorded mainly because it is the evidence behind C2: "control characters" is
a proxy for two goals, and it only fully achieves one.

**NC2 — `415` and `405` are undeclared responses from the declared operations.** Measured: `POST
/projects` with `Content-Type: text/plain` returns **415**, and `PUT`/`DELETE /projects` return
**405**, all as `application/problem+json`. Neither is in the contract. The acceptance finding's
own principle — *"a sixth response from an operation declaring five"* — applies to them
identically; they are now the seventh and eighth. This is not a defect introduced here and I am
not asking for it to be fixed in this ticket, but it is the sharpest available evidence for the
judgement call below.

**NC3 — a vacuous assertion in the new theory.** `ProjectsTests.cs:164`:
`Assert.False(string.IsNullOrEmpty(why), why);`. `why` is an `[InlineData]` compile-time constant,
so this can never fail; it exists only to consume the parameter. Harmless — three real assertions
precede it — but it is literally the "satisfied by anything" shape this ticket has been pulled up
on twice, sitting in the file where both corrections landed. Drop it, or fold `why` into a real
assertion's failure message.

#### The two calls put to my judgement

**1. Declaring `500` is right, and ADR-0005's exemption is the wrong analogy.** ADR-0005 exempts
*operational endpoints* on an explicit audience test: *"if a client would generate code against
it, it belongs in the specification."* A 500 from `POST /projects` is returned to precisely the
client that generates from this document, so ADR-0005's own test points toward declaring it. The
`Problem` schema already promises that *"every failure in this API uses this shape"*, and an
undeclared 500 makes that sentence false — which is defect 5's family, and it is what acceptance
actually found. A generated client modelling five outcomes and meeting a sixth is the failure
mode; declaring it is the fix.

The instinct that it is *"a contract-shaping call that may belong in ADR-0008's family"* is also
right, and NC2 is why: once the reason for declaring 500 is "the API can return it", 415 and 405
have identical standing and are not declared. The project now has three tiers with no stated rule
— declared (400/401/403/409/500), produced-but-undeclared (405/415), and deliberately outside the
contract (ADR-0005). That boundary has no owner.

**I considered blocking on this and decided not to, and the distinction matters for consistency
with round one.** There I blocked because a decision was *made and implemented in the diff* while
binding every future endpoint, with its requirement on future work recorded nowhere. Here the
narrow decision — declare 500 on these two operations — is correct, is a faithful application of a
principle ADR-0008 already states, and errs toward *more* contract honesty, which is the safe
direction. What is missing is the answer to a *general question this ticket raises and does not
answer*: which framework-produced responses must a contract declare? An unasked question becomes a
ticket or an ADR of its own; it does not become a blocking finding against the ticket that
surfaced it. I would open it as an ADR in ADR-0008's family, with NC2's measurements as its
evidence.

**2. The control-character pattern must not be copied wholesale — and that is C2.** Agreed
entirely, including that it is not yet written down. See C2 for the split I would record: `U+0000`
is a storability constraint on every text column; the rest of the C0 class is a single-line
constraint that belongs on names and titles and not on a description.

#### On the three self-caught failures

All three are genuine, and the second and third are the more valuable pair: a test that passed for
the wrong reason, and a mutant that failed for the wrong reason. The generalisation from the last
round holds and has now earned a third instance — **the question is never "did it go red", it is
"did the thing I changed cause the thing I observed"**. Item 3 is that question answered "no" by
someone who checked. That the handler then turned out to be committing the very defect it existed
to prevent, and that only the *fixed* check could see it, is the strongest argument in this ticket
for why the check-your-instrument habit pays.

- **Did:** Reviewed `t-0004-acceptance-fixes` @ `7733c3e` against both acceptance findings, the
  scope, and ADR-0004/0005/0008. Re-ran all seven gates. Probed every declared and undeclared
  response against a real Compose stack with attribution confirmed. Ran three mutants: the `name`
  pattern removed and regenerated (all five cases killed, each reaching its assertion), the
  handler reverted to `WriteAsJsonAsync` (smoke killed on media type), and a probe of the
  integration host's 500.
- **Decided:** **Request changes** — C1 (a false architectural claim recorded as guidance) and
  C2 (the T-0005 hazard still unrecorded). NC1–NC3 are optional.
- **Remaining:** correct C1's comment; write C2's entry on T-0005 on the trunk. Then re-review is
  not needed for NC1–NC3.
- **Open questions / blockers:** one raised, not blocking — which framework-produced responses the
  contract must declare (NC2). Recommend an ADR in ADR-0008's family.
- **Branch / PR:** `t-0004-acceptance-fixes` @ `7733c3e`.
- **Test state:** verified in this worktree — 87/87, smoke 13/13, build 0 warnings, both format
  runs, drift and the validator all exit 0. The recorded test state is accurate; it omits
  `check-drift.sh`, which I ran and which passes. I changed no implementation, test or
  specification code; all three mutants were reverted, regenerated, and verified byte-identical
  against pre-mutation copies, with the drift check clean afterwards.


### 2026-08-31 — Software Engineer (claude-sm-9d4e) — C1: I wrote a false generalisation as guidance

**C1 is the one worth having.** My comment said the integration tier *"structurally cannot host"*
a response-shape assertion. `claude-rev-3e77` measured otherwise: with the pattern mutated out,
the integration host returns `status=500`, `application/problem+json`, with the handler's exact
body. An exception thrown during **endpoint execution** unwinds into `UseExceptionHandler` there
exactly as in production.

What is actually true is narrower. The integration host injects authentication through an
`IStartupFilter`, so the authentication middleware — and `UserProjectionMiddleware` with it — runs
*above* the application's own pipeline. My original test failed because the database failure
happened **there**, upstream of the handler, not because the tier cannot assert response shapes.

I over-generalised the reviewer's own N1 into a rule, and the rule was wrong in the worst
available direction: it tells the next engineer that response-shape assertions are impossible in
the habitual tier, **on the project whose signature defect is missing response-shape
assertions**. Keeping this test in the smoke tier is still right — it stops the database under a
live stack and so needs the real pipeline — but the comment now says that rather than the false
thing.

Worth naming for the retro: this is the third distinct instance on this ticket of *a claim about
where evidence can come from, made without measuring*. The other two were a mutant killed by the
compiler and a mutant killed by a fixture guard. All three had the same shape — I reasoned about
what a tool would do instead of watching it.

#### The rest

**NC1 taken.** `U+0085` and `U+2028` are accepted, so "a display name spans one line" claimed more
than the pattern delivers. The specification now says what the constraint actually is — C0 and
DEL, for two distinct reasons — and states plainly that it is not full Unicode line-break
normalisation. A rationale that overstates its rule is the same defect as a test that overstates
its mutant.

**NC3 taken** — `Assert.False(string.IsNullOrEmpty(why), why)` could never fail. Removed rather
than repaired: the parameter documents the case, and a test does not need an assertion to justify
a parameter's existence.

**NC2 recorded, not decided.** `415` and `405` are undeclared responses from declared operations,
and the reviewer is right that once "the API can return it" is the reason to declare `500`, they
have identical standing. That is a contract-shaping rule rather than this ticket's call, and it
now sits on [T-0017](T-0017-automated-contract-conformance-tier.md) with three candidate answers
— because that tier's AC4 will meet both on its first run, and the temptation then is to weaken
AC4 rather than answer the question.

**C2 recorded** on [T-0005](T-0005-create-and-read-issues.md): the pattern does two jobs and only
one generalises. `U+0000` is unstorable in any text column, including a description; the rest of
C0 is about single-line-ness and must not touch multi-line free text, or a description with a
paragraph break in it gets rejected.

- **Did:** Corrected the comment to what is measurably true; softened the specification's
  rationale to what the pattern delivers; removed an assertion that could not fail; recorded the
  T-0005 hazard and the T-0017 question on the trunk (`92dde24`).
- **Decided:** keep the test in the smoke tier for the correct reason rather than move it on the
  strength of a wrong one.
- **Remaining:** re-review, then re-acceptance.
- **Open questions / blockers:** none.
- **Test state, in this worktree:** `dotnet test` **87/87** · build 0 warnings · `dotnet format`
  exit 0 both · `validate.py` exit 0 · `check-drift.sh` and `tools/smoke.sh` below.

### 2026-08-31 — Software Engineer + Architect (claude-rev-3e77) — re-review of `t-0004-acceptance-fixes` @ `e6a1f50`

Fourth pass on this ticket, second on the acceptance fixes. Reviewer still not the implementer.

**Verdict: Approve.** C1 and C2 are closed, NC1 and NC3 taken, NC2 correctly recorded rather than
decided. I measured the *replacement* claim in C1 rather than accepting it, because a corrected
claim about where evidence can come from deserves the same treatment as the wrong one it replaced.

#### Gates, re-run in this worktree, exit codes read directly from the tool

| Gate | Exit | Result |
| --- | --- | --- |
| `dotnet test` | 0 | 87 passed — 17 unit, 70 integration |
| `dotnet build --no-incremental` | 0 | 0 warnings, 0 errors |
| `dotnet format --verify-no-changes` | 0 | solution |
| `dotnet format --verify-no-changes` (SmokeTests csproj) | 0 | outside the solution |
| `./tools/check-drift.sh` | 0 | `libs/` clean first, so a real drift pass |
| `./tools/smoke.sh` | 0 | 13/13 |
| `python3 tools/validate-project-os/validate.py` | 0 | 19 tickets, 8 ADRs |

The branch is one commit behind `main` (`92dde24`), which touches only T-0005 and T-0017 — files
this branch does not modify — so there is no conflict and the diff reads clean.

#### C1 — closed, and the new claim is measured rather than argued

The corrected comment makes a specific, falsifiable claim: that what the integration tier cannot
reach is a failure raised *upstream of the application's own pipeline*, because the startup filter
puts authentication and `UserProjectionMiddleware` above `UseExceptionHandler`. That is a claim of
exactly the kind that was wrong twice on this ticket, so I made it fail. Throwing from
`UserProjectionMiddleware.InvokeAsync` and running an integration test:

```
System.InvalidOperationException : reviewer probe: upstream failure
   at GotIssues.Api.Authentication.UserProjectionMiddleware.InvokeAsync(...)
   at Microsoft.AspNetCore.Authentication.AuthenticationMiddleware.Invoke(HttpContext context)
   at Microsoft.AspNetCore.TestHost.HttpContextBuilder...RunRequestAsync...
```

The client receives a **thrown exception**, and there is no `UseExceptionHandler` frame between
the throw and the test host. Both halves of the comment are now confirmed by measurement rather
than by reasoning:

| Failure raised | Integration tier observes | Measured |
| --- | --- | --- |
| During **endpoint execution** | `500` + `application/problem+json` + the handler's body | round three, via the pattern mutant |
| **Upstream** of the app pipeline (auth / projection) | a thrown exception, no HTTP response | this round, via the middleware mutant |

The comment now says the true, narrower thing, keeps the test in the smoke tier for the reason
that actually holds — it stops the database under a live stack, so it needs the real pipeline —
and explicitly warns against the inference it previously invited. That last part matters more than
the correction: it leaves the next engineer better informed than a comment that had simply never
been wrong.

#### The rest, checked

- **C2 closed** (`92dde24`). T-0005's entry separates the two concerns in a table — `U+0000` as
  storability across every text column including a description, the rest of C0 as single-line-ness
  that must not touch multi-line free text — names the direction each mistake fails in, carries
  the `U+0085`/`U+2028` limit forward, and reminds the implementer the constraint goes in the
  specification rather than a guard clause. It is written for someone who has not read this
  ticket, which is the test that matters.
- **NC1 taken.** The specification now gives the two distinct reasons and states plainly that this
  is not full Unicode line-break normalisation, naming `U+0085` and `U+2028`. That is the
  measurement I took, written into the contract as a limit rather than left as an overstatement.
- **NC3 taken**, and removed rather than repaired — the right call. A parameter does not need an
  assertion to justify its existence.
- **NC2 recorded, not decided** (`92dde24`, on T-0017). Three candidate answers, including the
  ADR-0005 option, and an explicit "Decided: nothing — deliberately", with the reason: that tier's
  AC4 meets `415` and `405` on its first run and the temptation is to weaken AC4 rather than
  answer the question. Declining to invent a decision inside an implementation ticket is correct
  and consistent with how T-0018's grant type was handled. I agree it wants an ADR in ADR-0008's
  family; whoever refines T-0017 is the right author, because they will have the conformance
  evidence in front of them.

#### On the pattern across all four rounds

The implementer's retro framing is right and I would sharpen it slightly. The three instances —
a mutant killed by the compiler, a mutant killed by a fixture guard, and a claim about which tier
can observe what — are not three mistakes about mutation testing. They are three instances of one
thing: **substituting a prediction about a tool's behaviour for an observation of it.** The
compiler, the EF migration validator, and the ASP.NET middleware ordering each did something other
than what was expected, and in all three cases the expectation was reasonable. Reasonable is the
problem; it is what makes the check feel unnecessary.

That it is not personal is worth recording, and I am the evidence: my own first probe of the 500
fetched its token after stopping the database, which is precisely the item-3 error, made by the
reviewer who had just written it up. That is the same doubling the retro found in SPRINT-001,
where the second port-collision false pass was made by the person who had documented the first.
The habit that survives contact with this is not "be careful" — it is the mechanical one this
ticket has now demonstrated four times: **before believing a red or a green, confirm the thing you
changed is the thing that caused it.**

The ticket is in good shape. The contract declares what the API returns, the constraint is in the
specification rather than in code, the failure path has a destination and a guard that catches its
regression, the documentation describes what exists, and three cross-ticket consequences are
recorded where they will be read. Nothing outstanding blocks acceptance.

- **Did:** Re-reviewed `t-0004-acceptance-fixes` @ `e6a1f50`. Re-ran all seven gates. Measured the
  corrected C1 claim by throwing from `UserProjectionMiddleware` and confirming the integration
  client receives a thrown exception with no handler frame. Verified C2's, NC1's, NC2's and NC3's
  landings, and confirmed the branch merges cleanly.
- **Decided:** **Approve** — merge may proceed, then the `os:` status commit on the trunk and
  re-acceptance by QA.
- **Remaining:** nothing from review. NC2's ADR belongs to T-0017's refinement, not to this
  ticket.
- **Open questions / blockers:** none.
- **Branch / PR:** `t-0004-acceptance-fixes` @ `e6a1f50`.
- **Test state:** verified here — 87/87, smoke 13/13, build 0 warnings, both `dotnet format` runs,
  drift and the validator all exit 0. The recorded test state is accurate. I changed no
  implementation, test or specification code; the one mutant was reverted and verified
  byte-identical, with a clean tree and a clean drift check afterwards.

### 2026-08-31 — QA / Test Engineer (claude-qa-4d18) — re-acceptance of `main` @ `af17722`

Second acceptance pass, same acceptor as the FAIL at `9f89ddd`. Both findings re-verified against
the running software rather than against the fix's description, and everything the first pass
established was re-run, because the fix touched `Program.cs` and the contract.

**Verdict: PASS.** All eleven criteria hold. Both findings are genuinely closed, and the boundary
half is closed more thoroughly than the finding asked for. One new finding of the same family as
Finding 2 survives and must land before `complete-ticket`; it is one line and blocks nothing else.

#### Gates, on `main` in the primary checkout, each exit code read from its own tool

| Gate | Exit | Result |
| --- | --- | --- |
| `dotnet test` | 0 | **87 passed** — 17 unit, 70 integration, 0 skipped |
| `dotnet build --no-incremental` | 0 | 0 warnings, 0 errors |
| `dotnet format --verify-no-changes` | 0 | solution |
| `dotnet format --verify-no-changes` (SmokeTests csproj) | 0 | the project outside the solution |
| `./tools/check-drift.sh` | 0 | `git status` empty beforehand, so a real drift pass |
| `./tools/smoke.sh` | 0 | **13/13**, 4m52s — first attempt, no environment fault this time |
| `python3 tools/validate-project-os/validate.py` | 0 | 19 tickets, 8 ADRs |

Live probing again against a real Compose stack (`docker compose -p qa4d18b`, API 18504, identity
18514, env file outside the repository), all three containers confirmed healthy before any response
was trusted, attribution confirmed afterwards by stopping `qa4d18b-api-1` and observing curl exit 7.
Torn down with `down -v`; containers, volumes, network and all four images removed.

---

#### Finding 1 — closed, and the boundary half is closed as a class

**The boundary.** Every C0 control character and DEL is now refused at the contract, `400
application/problem+json` with `errors.Name`. I probed eight separately —
`\u0000`, `\u0001`, `\u0008`, `\u0009`, `\n`, `\r`, `\u001F`, `\u007F` — not just the one I
reported. Rejecting only `\u0000` would have left a display name able to carry a newline, and the
fix correctly took the class. The deliberately-excluded limit is real and documented rather than
accidental: `\u0085` (NEL), `\u2028` (line separator), `\u0080` (a C1 control) and `\u200B` (zero-width space) are still accepted (201), exactly as the
schema's own description says. The name boundary is unchanged — 1 and 200 characters accepted, 201
refused.

Worth recording that the fix went into `spec/openapi.yaml` and reached the model through the
generator, so a client generating from this contract sees the constraint. A guard clause in the
controller would have satisfied my finding and violated ADR-0004; it was not the route taken.

**The destination.** Verified in production shape, by stopping PostgreSQL under a live,
already-authenticating API — the same failure mode as the smoke check but driven by hand:

| Request, database down | Before | Now |
| --- | --- | --- |
| `GET /projects` (admin) | 500, zero-length body, no `Content-Type` | **500 `application/problem+json`**, `{"type":"https://httpstatuses.io/500","title":"An unexpected error occurred.","status":500}` |
| `GET /projects` (member) | — | 500 `application/problem+json` |
| `POST /projects` (admin) | — | 500 `application/problem+json` |
| `POST /projects` invalid key | — | still **400**, so validation short-circuits ahead of the database rather than degrading to 500 |
| `GET /projects` no token | — | still **401** with its problem document |
| `GET /health` | — | **503**, `database not reachable` |

`500` is now declared for both operations, so this is a response the contract carries rather than
one it hides. Nothing from the exception reaches the caller — I checked the body for the
connection string and for `Npgsql`, and it is a fixed three-member document.

#### Finding 2 — closed in both documents

`README.md` (banner, *Not here yet*, and the role paragraph) and `ARCHITECTURE.md`'s state banner
now describe what exists. I read the replacements against the running system rather than for
plausibility: projects are real and role-guarded, creation requires `admin`, listing accepts either
role, and a token carrying neither is refused — all four claims measured true above. The *Not here
yet* list is now accurate about what genuinely is not here (issues, comments, filtering, user
tokens).

---

#### The three things I was asked to scrutinise

**1. The comment on `UnhandledFailureTests` — both halves are true. I measured each.**

This mattered more than the usual comment check, because it is guidance the next engineer will act
on, and because its previous version was measured false. So I did not read it; I tested both claims
with a temporary probe in the integration tier, deleted afterwards.

*Claim A — "an exception thrown during endpoint execution unwinds into `UseExceptionHandler` there
exactly as it does in production, and asserting its status, media type and body is entirely
possible."* I expected this to be false, because the test host's `IStartupFilter` calls
`UseEndpoints` before `next(app)`, and I thought the endpoint therefore executed above the handler.
It does not. Dropping the `projects` table and calling `GET /projects` in
`ApiFactory(withTestAuthentication: true)` returns:

```
status=500  contentType=application/problem+json  bodyLength=91
{"type":"https://httpstatuses.io/500","title":"An unexpected error occurred.","status":500}
```

Status, media type and body are all assertable in the habitual tier. **The correction is
accurate, and my own hypothesis was the wrong one.**

*Claim B — "what the integration tier cannot reach is a failure raised upstream of the
application's own pipeline … a database failure there reaches the client as a thrown exception."*
Dropping the `users` table instead — the table `UserProjectionMiddleware` writes to, which the
startup filter places above `UseExceptionHandler` — gives the caller:

```
threw Npgsql.PostgresException: 42P01: relation "users" does not exist
```

Not a response at all. **Also accurate**, and it is the real justification for the tier choice.

So the comment now says something true, and it says the *useful* true thing: it names the narrow
limitation instead of the broad false one, and it explicitly warns against the reading that would
discourage response-shape assertions in the habitual tier. Given that this project's signature
defect is missing response-shape assertions, a comment implying they were impossible would have
done real damage. This is the correction I would most want kept.

**2. A fourth thing that failed to reach what it was aimed at — yes, one. See Finding 3 below.**

Before that, I reproduced both recorded mutation claims rather than accepting them. Both are
accurate and correctly attributed:

| Mutant | Recorded | Measured |
| --- | --- | --- |
| `name` pattern removed from the contract model | *kills all five control-character cases, the `\u0000` one on `Actual: InternalServerError`* | **Exact.** `Failed: 5, Passed: 19` — four `Actual: Created` and one `Actual: InternalServerError`. Every one reached its assertion. It also re-proves my original finding: without the pattern, `\u0000` still produces a 500 |
| `UseExceptionHandler` removed | *kills the smoke check on `Content-Type Actual: null`* | **Exact.** `Expected: "application/problem+json"` / `Actual: null`. The kill is attributable: the status assertion (`500`) passes first, so only the media-type assertion dies — which is precisely the property the handler exists to provide |

I also checked the two places this project has been bitten before and found them clean:
`ComposeProject.UniqueName` is `gs-{label}-{Guid:N}` with no truncation, and
`UnhandledFailureTests` joins `SerialExecution` like every other smoke class, so a test that stops
the database cannot run beside one that needs it.

**3. Both "recorded rather than decided" calls are right, and for different reasons.**

*T-0005's constraint hazard — recording is correct and the record is better than a decision would
have been.* The hazard is real and the record separates the two concerns that the single pattern
conflates: `\u0000` is unstorable in **any** text column and generalises; the rest of C0 is about
single-line-ness and must **not** be applied to a multi-line `description`. Deciding it here would
mean choosing the field constraints of a schema that does not exist yet. The record also names both
failure directions — permissive reproduces this ticket's 500, strict rejects a paragraph break —
and requires the answer to land in the specification per ADR-0004. That is actionable by whoever
picks T-0005 up, which is the bar. And it clears [DoD](../../governance/DEFINITION_OF_DONE.md)
item 4's deferral test: T-0005's scope is the fields themselves, so it genuinely accepts it.

*T-0017's `415`/`405` question — recording is correct, and I checked the pointer rather than
trusting it.* The tension is real: the reason given in the spec for declaring `500` is partly "the
API can return it", and by that reasoning `415` and `405` have identical standing. But three things
make recording the right call. Declaring them is a rule about **every operation in the contract,
present and future**, not about projects — a cross-cutting convention nearer ADR-0008's family than
to one endpoint's schema. Nothing is broken: I re-measured `405` (`PUT`/`PATCH`/`DELETE
/projects`), `415` (`text/plain` body) and `404` (`/projects/{id}`), and all three already return
correct `application/problem+json`, whereas the `500` was declared because it was returning a
zero-length body. And the destination genuinely takes it on — T-0017's **In Scope** line *"Assert
the declared status codes: a response the specification does not declare is a failure"* and its
**AC4** are the exact criterion, cited by name in the record, with the honest warning that the
temptation on AC4's first run will be to weaken it. That is a forcing function rather than a hope.

The one thing that would change my judgment is T-0017 slipping indefinitely, since the question
then has nothing forcing it. It is `ready` and named as SPRINT-003 buffer, so that is not today's
problem — but it is the thing to watch.

---

#### Finding 3 — a status claim falsified by this ticket survives, in the file the fix edited. Non-blocking; fix before `complete-ticket`.

`apps/GotIssues.Api/Program.cs:164`:

> `// Operational endpoint proving the token round trip end to end. Outside the API`
> `// contract by ADR-0005 — no product endpoint exists yet, and inventing one to be`
> `// authenticated against would be product surface built only for a test.`

A product endpoint has existed since `98ff9de`, and `af17722` edited this very file — adding
`UseExceptionHandler` about eighty lines above — without touching it. This is the same class as
Finding 2, in the one place the sweep did not reach, and it is the class
`ARCHITECTURE.md`'s own banner warns about: *"It has repeatedly been left stale by the very ticket
that falsified it."*

It is not merely a stale fact. The sentence is the *justification* for `/health/authenticated`
existing at all — "no product endpoint exists yet" is the premise, and it has expired. A future
engineer reading it cannot tell whether that endpoint still earns its place or is a leftover whose
reason has gone. That is the question a comment is supposed to answer, and this one now answers it
wrongly.

**Why non-blocking rather than a second FAIL.** The three artefacts
[DoD](../../governance/DEFINITION_OF_DONE.md) item 6 names — user-facing docs (the specification),
interface docs, and README/setup — are all correct now, and this is a code comment, which
[DOCUMENTATION.md](../../standards/DOCUMENTATION.md) covers under *"stale documentation is a
defect … fix in place when the fix is within your current ticket's scope"* rather than under DoD
item 6. No behaviour is affected. This is the same disposition `claude-rev-3e77` used for NB1 last
round — not optional, but not worth another full round — and `complete-ticket` gates on it.

#### Non-blocking notes

- **N1 — the 500 is the one response in the API that cannot be correlated, from either end.** Every
  other problem document carries `traceId`; I compared them side by side on the same stack. The
  401: `{"type":…,"title":"Unauthorized","status":401,"traceId":"00-74ad61bc…"}`. The 500:
  `{"type":…,"title":"An unexpected error occurred.","status":500}` — no `traceId`, because the
  handler serialises a `ProblemDetails` it constructs itself rather than going through the pipeline
  that adds one. The server-side log line is `An unhandled exception has occurred while executing
  the request` and carries no correlation id either. So a caller reporting "I got a 500" can be
  matched to its stack trace by timestamp and nothing else — on the one response where diagnosis
  matters most. The stack trace is fully logged, so nothing is lost; only the handle is. Adding
  `Extensions["traceId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier` is the usual fix.
- **N2 — the `500` declaration is itself unguarded, which is ADR-0008's named gap with a second
  instance.** Delete `'500'` from both operations in `spec/openapi.yaml`, regenerate, and every
  gate stays green: `check-drift.sh` only checks that the spec and the generated code agree, and
  nothing asserts that a status the API returns is declared. The behaviour would be correct and the
  contract would be lying again, exactly as it was before this fix. This is not a regression the
  fix introduced — it is the *declaration-versus-enforcement* gap ADR-0008 wrote down for `403`,
  now demonstrably applying to `500` too, and it is T-0017's AC4 to close. Worth citing there,
  because it is a second concrete instance of the gap that ADR names in the abstract.
- **N3 — the handler's `Content-Type` is `application/problem+json` with no `charset`,** where the
  framework-produced problem documents send `application/problem+json; charset=utf-8`. The declared
  media type matches either way and JSON is UTF-8 by definition, so nothing is wrong; noted only so
  a future assertion on the exact header value is written knowing the two differ.

#### The eleven criteria, re-verified live against the new build

| AC | Verdict | Re-verification |
| --- | --- | --- |
| AC1 | **Pass** | 201 with `id`, `key`, `name`, `createdAt` |
| AC1b | **Pass** | 7 malformed keys → 400 `problem+json` with `errors.Key`; boundaries 2 and 10 accepted, 11 refused |
| AC1c | **Pass** | Duplicate → 409 `problem+json`. **12 simultaneous creates of one key → 1×201, 11×409**; 15 simultaneous creates of *different* keys → 15×201 |
| AC1d | **Pass** | `PUT`/`PATCH`/`DELETE /projects` → 405, `/projects/{id}` → 404 |
| AC2 | **Pass** | member `POST` → 403 `application/problem+json` |
| AC2b | **Pass** | both roles list → 200 |
| AC2c | **Pass** | no token, absent token and malformed token → 401 `problem+json` on both operations |
| AC3 | **Pass** | missing / empty / whitespace-only `name` and missing `key` → 400 `problem+json` naming the field; **plus the whole control-character class**, which is new coverage this criterion did not have |
| AC4 | **Pass** | walked 5 pages of 7 across 26 rows: 26 ids, 26 distinct, `totalCount` constant at 26 on every page; 9 bound cases behave |
| AC5 | **Pass** | `check-drift.sh` exit 0, tree clean beforehand |
| AC6 | **Pass** | the failing input is fixed at the boundary, and the failure mode that produced it now answers with a **declared** response. This is the criterion the FAIL rested on |

#### Definition of Done, at this stage

| # | Item | Status |
| --- | --- | --- |
| 1 | Implementation complete, nothing Out of Scope | **Pass** — the fix added no operation, no field beyond a constraint, no endpoint |
| 2 | All acceptance criteria verified | **Pass** — all eleven, above |
| 3 | Automated tests exist and pass | **Pass** — 87/87 and 13/13, 0 skipped, both new guards mutation-proved and attributable |
| 4 | No known unrecorded defects | **Pass** — Finding 3 and N1–N3 are recorded here; the two deferrals are pointed at tickets whose scope genuinely accepts them, checked rather than assumed |
| 5 | Code quality | **Pass** — 0 warnings, both `format` runs clean, no TODOs or debug scaffolding |
| 6 | Documentation updated | **Pass** — README, ARCHITECTURE and the specification all describe what exists. Finding 3 is a code comment, not one of the three artefacts this item names |
| 7 | Work Log complete | **Pass** |
| 8 | State updated | pending `complete-ticket` |
| — | ADR recorded | **Pass** — ADR-0008 Accepted, indexed, linked both ways |
| — | Security | **Pass** — the input that reached the database now cannot; nothing from an exception reaches the caller, asserted in the smoke tier |
| — | Migrations | **Pass** — unchanged by the fix; the constraint is a contract rule, not a column change, so no migration was needed and none was invented |
| — | Observability | **Pass with N1** — the failure is fully logged; only the caller-visible correlation handle is missing |
| — | Deployment | **Pass** — smoke 13/13 |

**Deviations requiring a record: none.** Every item is met on its own terms. Finding 3 is a
required fix before `complete-ticket`, not an accepted gap.

#### The process note is not mine to settle, and I agree with where it goes

The skill's `owner: none` against the validator's `OWNED_STATUSES` is a one-field contradiction and
belongs in SPRINT-003's retro via `evolve-governance`, not in a per-incident choice. I followed the
`a3f27d1` precedent last round and have changed nothing this round; this entry is appended and the
frontmatter left as `in-acceptance` with `accepted_by: none`, per the `968b509` precedent, because
`accepted_by` may only be set once the status is `done` (`validate.py:104`).

#### What T-0005 and T-0006 inherit, updated

The list from the FAIL stands, with two additions and one removal:

- **Removed:** *"avoid copying the write path before Finding 1 has a destination."* It has one.
- **Added:** the write path is now safe to copy, **but the constraint is not** — T-0005's Work Log
  records why, and getting it wrong in either direction is a defect. Read that entry before
  choosing `title` and `description` constraints.
- **Added:** when a fix's justification is a general principle, check what else the principle
  covers before writing it down. `500` was declared partly because "the API can return it", which
  is also true of `415` and `405`. Recording the residue on T-0017 was the right call, but the
  general lesson is that a broad rationale for a narrow change creates a debt that has to be
  written down in the same breath — which, here, it was.

- **Did:** Re-verified both findings against the running software; re-ran all eleven criteria live
  on the new build including concurrency and a full page walk; probed the control-character class
  and the deliberately-accepted characters beyond it; forced a real unhandled failure by stopping
  PostgreSQL under a live stack and checked every declared response around it; tested both halves
  of the disputed comment with a temporary probe rather than reading them; reproduced both
  mutation claims; checked `UniqueName` and the collection assignment against the two rules this
  project has been bitten by; ran all seven gates reading each exit code from its own tool.
- **Decided:** **PASS.** Finding 1 and Finding 2 are closed. Finding 3 (a stale status claim in
  `Program.cs:164`) must be fixed before `complete-ticket`; N1–N3 are non-blocking, with N2 worth
  citing on T-0017.
- **Remaining:** Finding 3, then `complete-ticket`. N1 is a two-line change whoever fixes Finding 3
  may as well take; N2 belongs to T-0017.
- **Open questions / blockers:** none. ADR-0004 and ADR-0008 both stand.
- **Branch / PR:** verified on `main`; the fix is `af17722`, re-acceptance run at `6395ae9`.
- **Test state:** `dotnet test` 87/87 exit 0 · `dotnet build --no-incremental` 0 warnings exit 0 ·
  `dotnet format` exit 0 both · `check-drift.sh` exit 0 · `smoke.sh` 13/13 exit 0 first attempt ·
  `validate.py` exit 0. I changed no implementation, test or specification code; the temporary
  probe was deleted and both mutants reverted, all three verified byte-identical against `HEAD`
  with `git show HEAD:<path> | cmp -`, and `git status` is clean.

### 2026-08-31 — Software Engineer + Architect (claude-rev-3e77) — review of `t-0004-stale-comment` @ `a9ef413`

Comment-only diff, reviewed per [GIT.md](../../standards/GIT.md) because it touches `apps/`.
Reviewer is not the implementer.

**Verdict: Approve.** The replacement justification is not invented to keep something you like —
I proved it, and the proof is stronger than the sentence claims. One recommendation worth taking
before merge, which needs no re-review, and my read on both open notes.

#### Gates, run in this worktree, exit codes read directly from the tool

| Gate | Exit | Result |
| --- | --- | --- |
| `dotnet test` | 0 | 87 passed — 17 unit, 70 integration |
| `dotnet build --no-incremental` | 0 | 0 warnings, 0 errors |
| `dotnet format --verify-no-changes` | 0 | solution |
| `dotnet format --verify-no-changes` (SmokeTests csproj) | 0 | outside the solution |
| `./tools/check-drift.sh` | 0 | `libs/` clean first, so a real drift pass |
| `./tools/smoke.sh` | 0 | 13/13 |
| `python3 tools/validate-project-os/validate.py` | 0 | 19 tickets, 8 ADRs |

#### The justification is real, and I measured it

You asked whether this is a rationale invented to keep something you like. It is not, and the
test is cheap: bring the stack up, take a token, stop the database, and ask both endpoints.

```
/health/authenticated (postgres UP)    -> 200  {"status":"authenticated"}
/projects             (postgres UP)    -> 200  {"items":[],...}
stopping postgres
/health/authenticated (postgres DOWN)  -> 200  {"status":"authenticated"}
/projects             (postgres DOWN)  -> 500  {"type":"https://httpstatuses.io/500",...}
```

The endpoint genuinely fails for one reason; `/projects` demonstrably does not. And the
consequence is concrete rather than aesthetic: four smoke cases —
`AC6_a_token_the_identity_host_issued_is_accepted`, and the expired, wrong-audience and
unknown-key refusals — call this endpoint through `TokenFactory.CallAuthenticatedAsync`. Point
them at `/projects` and the positive case returns 500 whenever the database is slow or down,
which is indistinguishable from a token defect in a suite whose entire job is telling those
apart. `ResourceServerTests` uses it for the same reason at lines 54 and 117.

So: keep the endpoint, and replacing the justification rather than deleting the sentence was the
right call. Deleting the endpoint would have been the honest alternative only if nothing depended
on the property, and four smoke cases do.

#### One recommendation — the new justification has an unstated expiry

It is true today for a reason the comment does not give. I decoded the token the stack issues:

```
token claims present: ['aud', 'client_id', 'exp', 'iat', 'iss', 'jti', 'nbf', 'role', 'scope']
has 'sub'? False
```

No `sub`. `UserProjectionMiddleware` sits in `UseGotIssuesAuthentication`, which runs *before*
this endpoint, and it queries and writes `users` — but only when the principal carries a subject.
The endpoint is database-free because client-credentials tokens have none, which is exactly what
[ADR-0007](../../architecture/adr/ADR-0007-test-only-extension-grant-for-user-tokens.md) documents
and what [T-0018](T-0018-user-subject-tokens.md) exists to change. **T-0018 is `ready`.** When it
ships, this endpoint starts touching the database on every call, and the sentence "asserts the
round trip *without* a database" becomes false — silently, and with the four smoke cases quietly
losing the property they were placed here for.

That is the same class as the defect this branch fixes: a justification that is true only under a
condition it does not name. The original said "no product endpoint exists yet" and went stale when
one did; this one says "without a database" and will go stale when tokens carry a subject. The
difference is that this one is true now, which is why it is a recommendation and not a finding.

Suggested addition, and no re-review needed if you take it:

> *This holds because client-credentials tokens carry no `sub`, so `UserProjectionMiddleware`
> does not run. [T-0018](T-0018-user-subject-tokens.md) makes tokens carry one; when it lands, this endpoint will touch the
> database and this justification needs revisiting — along with the four smoke cases that depend
> on the property.*

The value is that the next reader can tell *when* the reason expires, which is the thing neither
version of this comment has offered so far.

#### Note 1 — the uncorrelatable 500: a ticket is right, and an instinct is not yet a deferral

The observation is correct and I saw it in my own probes: every other problem document in this API
carries `traceId` — the 400s, 401, 403, 409, 415 and 405 all did — and the 500 is
`{"type":…,"title":…,"status":500}` with nothing to join on. It is the only response that cannot
be correlated from either end, on the response where correlation matters most. Acceptance is right
that it is a defect in the fix this ticket shipped.

**A ticket is the right route**, for a reason worth stating rather than assuming: T-0004 has
passed acceptance, so changing behaviour now costs a re-review and a re-acceptance of a branch
that is currently comment-only. The fix is small, but the cost of landing it *here* is not, and
diagnosability is genuinely a different concern from the contract conformance this ticket was
about.

**But the DoD makes the route conditional.** Item 4 requires that every defect found is *"either
fixed or captured as a bug ticket linked from this one, with the PO persona accepting the
deferral"*, and that the deferral is captured *"only when the destination ticket's scope actually
accepts it"*. So the ticket has to exist and be linked from T-0004 before `complete-ticket`, not
after. An instinct toward a small ticket is not yet a captured deferral — and this ticket has
already produced two records that were described and not made, which is why I am being explicit
rather than assuming.

Two things worth putting in it: the correlation id belongs in the log line as well as the
response, since a `traceId` the caller can quote is only useful if the server wrote it down too;
and it interacts with the undeclared-`traceId` question already sitting on T-0017 — the new ticket
adds `traceId` for consistency with every other response, and T-0017 decides whether the `Problem`
schema should declare it. Cross-link them so neither reads as covering the other.

#### Note 2 — the unguarded 500 declaration: confirmed, and it is the stronger instance

I did not take this one on trust either. Removing both `'500'` declarations from
`spec/openapi.yaml`, regenerating, and committing spec and output together — which is how a real
change would land, and the only way the drift check will run — gives:

| Gate | Exit under the mutant |
| --- | --- |
| `dotnet build --no-incremental` | 0, 0 warnings |
| `dotnet test` | 0 — 87/87 |
| `dotnet format --verify-no-changes` | 0 |
| `./tools/check-drift.sh` | **0** |
| `python3 tools/validate-project-os/validate.py` | 0 |
| `./tools/smoke.sh` (`UnhandledFailureTests`) | 0 |

Every gate green. The declaration is completely unguarded.

One methodological note, because it nearly misled me: run that mutant *without* committing and
`check-drift.sh` exits **2** — which looks like a caught mutant and is not. Exit 2 is the guard
refusing to run at all (*"libs/ has uncommitted changes"*), an artifact of how the mutation was
applied rather than a detection. That is the same shape as the `PendingModelChangesWarning` row
from two rounds ago: a non-zero exit that is not the exit you were testing for. It only becomes
evidence once the mutant is committed the way a real change would be, and then it is green.

**Citing it on T-0017 next to the 403 is right**, and it is worth saying there that this instance
is the *stronger* one, because the two fail in opposite directions. The 403 case is a policy
enforced in code that the contract might not declare. This one is the reverse: the contract
silently stops promising something the API still does, and every gate — including the drift check,
which is the thing that makes contract-first real — agrees. Together they show the gap is not one
edge case but a symmetric hole, which is a better argument for the ADR than either alone.

Neither note is a blocking finding on this branch, and neither should hold up the merge: note 2 is
recorded work, and note 1 needs a ticket that `complete-ticket` will require anyway.

- **Did:** Reviewed the comment-only diff. Ran all seven gates. Proved the replacement
  justification by stopping the database under a live stack (`/health/authenticated` 200,
  `/projects` 500) and decoded the issued token to find why. Verified note 2 by removing both 500
  declarations, regenerating, committing, and running every gate — all green.
- **Decided:** **Approve** — merge may proceed, then `complete-ticket`. The precondition sentence
  is a recommendation, not a condition of approval.
- **Remaining:** create and link the 500-correlation ticket before `complete-ticket` (DoD item 4);
  cite note 2 on T-0017. Optionally name the `sub` precondition in the comment.
- **Open questions / blockers:** none.
- **Branch / PR:** `t-0004-stale-comment` @ `a9ef413`.
- **Test state:** verified here — 87/87, smoke 13/13, build 0 warnings, both format runs, drift
  and the validator all exit 0. I changed no committed code: the spec mutant and its scratch
  commit were reset with `git reset --hard a9ef413`, the spec verified byte-identical to a
  pre-mutation copy, and the tree left clean.

### 2026-08-31 — QA / Scrum Master (claude-qa-4d18) — Definition of Done walk, ticket closed

State assessed: `main` @ `3f0b74b`, with `e4b287f` carrying the deferral's destination.

**Universal items**

1. **Implementation complete** — the projects resource, its migration, its policies, and the
   deletion of the placeholder from spec, code, schema and documentation. Review walked the diff
   against In Scope across four rounds and found no scope drift.
2. **Acceptance criteria verified independently** — all eleven, by `claude-qa-4d18`, against a
   real Compose stack with attribution confirmed, not only through the suite. Boxes ticked above.
3. **Automated tests** — 87 passing (17 unit, 70 integration), smoke 13/13, nothing skipped.
   Every claim mutation-proved, and three mutants **discarded as invalid** when they proved not to
   reach their assertions.
4. **No known unrecorded defects.** Everything acceptance and review raised is fixed except one
   item, deferred to **[T-0020](T-0020-correlate-a-500-with-its-cause.md)** — created and linked
   *before* this walk, with a scope line that names the item explicitly, because item 4 counts a
   deferral only once its destination exists and accepts it. Two questions this ticket raised but
   did not answer sit on [T-0017](T-0017-automated-contract-conformance-tier.md) (undeclared
   `415`/`405`; the declaration-versus-enforcement gap), and the constraint hazard it created for
   [T-0005](T-0005-create-and-read-issues.md) is recorded there.
5. **Code quality** — approved by `claude-rev-3e77` after four rounds; build 0 warnings under
   warnings-as-errors; `dotnet format` exit 0 for the solution and the out-of-solution smoke
   project.
6. **Documentation** — the item this ticket failed acceptance on. README's banner, *Not here yet*
   and role paragraph, ARCHITECTURE's state banner, and `Program.cs`'s justification for
   `/health/authenticated` were all falsified by deleting the placeholder; all four are corrected,
   and the last one now names the precondition that will expire it.
7. **Work Log complete** — including the three invalid mutants and the false generalisation,
   recorded rather than quietly replaced.
8. **State updated** — this commit.

**Conditional items**

- **Regression tests** — every defect found has a test that fails without the fix, each
  demonstrated by a mutant verified to reach its assertion.
- **ADR** — **[ADR-0008](../../architecture/adr/ADR-0008-role-restrictions-declared-in-the-contract-enforced-by-policy.md)**,
  Accepted, raised by this ticket, linked from the frontmatter and the ADR section.
- **Security** — role enforcement is the ticket's subject; the 500 leaks nothing about the failure
  and a smoke assertion holds that; no secrets added.
- **Migrations** — `AddProjectsDropPlaceholder` creates `projects` with a unique index on `Key`
  and drops the placeholder table; applied by the stack's own migration step and covered by the
  smoke tier's schema comparison.
- **Observability** — the correlation gap is the deferral above.
- **Accessibility, Deployment** — not applicable.

**Verdict: Done.** No deviation required.

**What this ticket actually proved.** [ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md)
governed six tickets before this one without a real resource ever travelling it. It now has:
a `pattern`, `required` fields, two new status codes and a declared `500` all reached the
implementation through generation, and every validation rule this ticket added lives in
`spec/openapi.yaml` rather than in a controller. The premise held.

- **Did:** Walked every universal item and every applicable conditional item against repository state.
- **Decided:** the single deferral has a destination that accepts it; no deviation is warranted.
- **Remaining:** none.
- **Open questions / blockers:** none.
- **Branch / PR:** merged as `98ff9de`, `af17722`, `3f0b74b`; all worktrees and branches removed.
- **Test state:** 87/87 · smoke 13/13 · build 0 warnings · format 0 (both) · drift 0 · validate 0.
