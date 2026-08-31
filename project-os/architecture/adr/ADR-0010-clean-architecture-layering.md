# ADR-0010: Adopt Clean Architecture layering — domain, application ports, infrastructure adapters

## Status

Accepted

**Decided by the maintainer on 2026-08-31**, superseding [ADR-0009](ADR-0009-controllers-talk-to-the-dbcontext-and-invariants-are-extracted.md) the same day it was accepted. ADR-0009 argued for thin controllers over `DbContext` with invariants extracted piecemeal; the maintainer's direction is the fuller pattern — repositories, ports, and the layer separation Clean Architecture prescribes — with the first implementation serving as the paradigm every later ticket copies.

The maintainer's assessment, recorded verbatim because it is the rationale:

> *"in my opinion the current implementation is an anti-pattern"*

That is right about the implementation, whatever one concludes about the general rule. `IssuesController.CreateIssue` opens a transaction, executes hand-written SQL implementing a correctness-critical invariant, re-reads the row it just locked, and maps a response — transaction management, domain logic, persistence and delivery in one method, with a column named in a string nothing checks against the entity. ADR-0009 noted that fragility and argued for keeping the arrangement regardless; the fragility was the stronger signal.

The direction is the maintainer's. The concrete shape below is mine, chosen under that direction; the two sub-decisions most worth overruling are marked **[open to reversal]**.

## Date

2026-08-31

## Context

Today `IssuesController` and `ProjectsController` take `GotIssuesDbContext` directly and query it inline. `UserProjectionMiddleware` and `DatabaseHealthCheck` do the same. There is no domain layer, no port, and no adapter: persistence, domain rules and HTTP handling occupy one project and, for the issue-number allocator, one method.

[ADR-0009](ADR-0009-controllers-talk-to-the-dbcontext-and-invariants-are-extracted.md) recorded that arrangement and proposed extracting only invariants. It was accepted, then superseded within the hour, and the reason is worth stating precisely rather than as a change of mind: **ADR-0009 optimised for the system as it is today — two resources, 258 lines of controller — and the maintainer is optimising for what it is becoming.** [T-0006](../../product/tickets/T-0006-issue-lifecycle-fields.md), [T-0007](../../product/tickets/T-0007-list-and-filter-issues.md) and [T-0008](../../product/tickets/T-0008-comment-on-an-issue.md) all add behaviour over the same entities, and the cost of introducing a boundary rises with every ticket that does not have one. ADR-0009's own rule 4 named the trigger as "when a second entry point needs the same logic"; the maintainer's judgement is that waiting for that trigger is more expensive than acting before it.

What does **not** change: [ADR-0004](ADR-0004-contract-first-openapi-code-generation.md) still owns the API surface. The generated abstract controllers remain the delivery boundary, are never hand-edited, and controllers still implement them without declaring routes.

## Decision

**Adopt Clean Architecture layering across the API service. Dependencies point inward only.**

```
Controllers (generated contract)  ->  Application (use cases, ports)  ->  Domain
                                            ^
Infrastructure (EF adapters) ---------------'   implements ports; depends inward
```

**1. Domain** — entities and the rules that must hold regardless of caller. The issue-number invariant lives here. No EF attributes, no `DbContext`, no ASP.NET types.

**2. Application** — one type per use case (`CreateIssue`, `GetIssue`, `CreateProject`, `ListProjects`), depending on **ports**: interfaces this layer owns and infrastructure implements (`IProjectRepository`, `IIssueRepository`, `IUnitOfWork`). Returns a result the controller maps to a response; it never returns `IActionResult`.

**3. Infrastructure** — EF Core adapters implementing those ports, owning `GotIssuesDbContext`, the migrations, and the raw SQL the allocator needs.

**4. Controllers** — implement the generated contract, call one use case, map its result to a status code. No queries, no `DbContext`.

**Separate projects, not folders [open to reversal].** `GotIssues.Domain`, `GotIssues.Application`, `GotIssues.Infrastructure`, with `GotIssues.Api` referencing Application and Infrastructure. Folders inside one assembly document the intent; project references *enforce* it — the compiler refuses a domain type that reaches for `DbContext`. Given the maintainer's instruction that this becomes the paradigm, an enforced boundary is worth more than a documented one. The cost is four projects and a slower solution load for a system this size.

**Domain entities are the EF entities, for now [open to reversal].** Clean Architecture usually separates persistence models from domain models. This decision keeps one set, configured by `IEntityTypeConfiguration` in Infrastructure so the domain types carry no attributes. Two sets would double the mapping for no benefit this system can currently name — but this is the sub-decision most likely to be wrong later, and separating them afterwards is a mechanical if tedious change.

**Not adopted:** CQRS, MediatR, a separate read model, or domain events. Each is commonly bundled with this pattern and none is implied by it; adopting them here would add indirection this system has no evidence it needs. A later ADR may revisit any of them.

**The first implementation is the paradigm.** [T-0022](../../product/tickets/T-0022-adopt-clean-architecture-layering.md) migrates projects and issues, and its output is what every later ticket copies. It therefore has a documentation obligation the refactor itself does not: the pattern is written into [ARCHITECTURE.md](../ARCHITECTURE.md) and [ENGINEERING.md](../../standards/ENGINEERING.md), so review has something to point at rather than an example to infer from.

## Options Considered

**1. Full layering with separate projects — chosen.** Enforced boundaries, the pattern the maintainer asked for, and a compiler that refuses violations. *Costs:* four projects; a use case per operation even where it forwards; and a one-off migration of working, tested code — the riskiest kind of change, because it must preserve behaviour exactly while touching everything.

**2. Layering by folder inside `GotIssues.Api`.** Same structure, no project references, no enforcement. Cheaper and reversible, and it would have been my choice under ADR-0009's reasoning. Rejected because the instruction is that this becomes the paradigm: an unenforced convention is one distracted afternoon from being violated, and this repository's own history is of conventions drifting until a reviewer catches them.

**3. ADR-0009's position — extract invariants only.** Superseded. Its argument was that layers added before there is variation to absorb become pass-through, which remains true and is the honest cost of this decision.

**4. Full Clean Architecture *plus* CQRS/MediatR.** Rejected: not implied by the pattern, and it would make the paradigm harder to copy correctly. The point of a paradigm is that the next person can follow it without a second framework.

## Consequences

### Positive

- Domain rules — the issue-number allocation especially — become testable without HTTP or a database, and unreachable from code that should not touch them.
- The boundary is compiler-enforced, so "we should have used the repository" cannot be discovered in review three tickets later.
- A single reference implementation exists for T-0006 through T-0008 to copy, which is worth more than the layering itself.

### Negative

- **This is a refactor of working, accepted code**, and refactors of tested code are where behaviour quietly changes. Every existing test must pass untouched — that is the only real safety net, and it is why the ticket forbids changing tests to fit the new shape.
- **Pass-through is now the expected state for simple operations.** `ListProjects` will be a use case that calls a repository that calls `DbContext`. ADR-0009 called that a smell; under this decision it is the price of uniformity, and reviewers must not treat each instance as a finding.
- Four projects, more files, and a longer path from "read the controller" to "understand what happens".
- The raw SQL allocator moves rather than disappears; its unchecked column name becomes Infrastructure's problem, which is the right place but not a solution.

## Risks

- **Behaviour change during migration is the real risk**, not design. Mitigation: the existing suite (102 tests) and the smoke tier are the acceptance gate, and they must pass **unmodified**. A test that needs changing to accommodate the refactor is evidence the refactor changed behaviour.
- **Sequencing.** T-0006 is committed in SPRINT-003 and touches issue creation. Doing it before the refactor means writing code in the old shape and migrating it twice; doing the refactor first delays the MVP. That is a maintainer decision, recorded on [T-0022](../../product/tickets/T-0022-adopt-clean-architecture-layering.md) rather than assumed here.
- **The paradigm could be copied wrongly** if the first implementation is inconsistent. Hence the documentation obligation.

## Follow-up Actions

- [T-0022](../../product/tickets/T-0022-adopt-clean-architecture-layering.md) implements this and produces the reference implementation.
- [ARCHITECTURE.md](../ARCHITECTURE.md) and [ENGINEERING.md](../../standards/ENGINEERING.md) are updated by that ticket via `evolve-governance`; ARCHITECTURE.md currently describes the API service as doing "request handling, domain logic, persistence", which this decision falsifies.
- [T-0006](../../product/tickets/T-0006-issue-lifecycle-fields.md) carried ADR-0009's rule 3 (extract the allocator). That obligation moves here; T-0006's reference is corrected.

## Related ADRs

- **Supersedes [ADR-0009](ADR-0009-controllers-talk-to-the-dbcontext-and-invariants-are-extracted.md)** — read it for the argument this decision overrides, including the cost it names
- [ADR-0004](ADR-0004-contract-first-openapi-code-generation.md) — unchanged: the generated contract remains the delivery boundary
- [ADR-0008](ADR-0008-role-restrictions-declared-in-the-contract-enforced-by-policy.md) — unchanged: policy attributes stay on controllers, which is where authorisation belongs
- [ADR-0003](ADR-0003-initial-technology-stack.md) — PostgreSQL and EF Core remain; this changes where they are referenced from, not what they are

## Related Tickets

- [T-0022](../../product/tickets/T-0022-adopt-clean-architecture-layering.md) — implements this decision and is the paradigm
- [T-0006](../../product/tickets/T-0006-issue-lifecycle-fields.md), [T-0007](../../product/tickets/T-0007-list-and-filter-issues.md), [T-0008](../../product/tickets/T-0008-comment-on-an-issue.md) — the tickets that will copy it
- [T-0005](../../product/tickets/T-0005-create-and-read-issues.md) — where the allocator lives today
