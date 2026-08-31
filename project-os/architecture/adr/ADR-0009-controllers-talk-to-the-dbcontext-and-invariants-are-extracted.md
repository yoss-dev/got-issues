# ADR-0009: Controllers talk to the DbContext directly; only domain invariants are extracted

## Status

Proposed

**Not in force.** Raised by the maintainer on 2026-08-31, asking why `IssuesController` takes a `GotIssuesDbContext` directly rather than a service, and whether that is consistent with Clean Architecture and best practice. The answer was that nothing in this repository ever decided either way — which is the reason for this ADR. **Nothing changes until this is Accepted or Rejected.**

## Date

2026-08-31

## Context

Both controllers take `GotIssuesDbContext` as a constructor parameter and query it directly. So do `UserProjectionMiddleware` and `DatabaseHealthCheck`. There is no service layer, no repository interface, and no application/domain project separation.

**This is not drift.** [ARCHITECTURE.md](../ARCHITECTURE.md) describes the API service as *"Implements the generated controller interfaces: request handling, domain logic, persistence"* — one component doing all three, in writing. [ENGINEERING.md](../../standards/ENGINEERING.md) states exactly one structural rule for controllers: they implement generated interfaces, and hand-written routing attributes are a review rejection.

**It is also not a decision.** It is what the first controller happened to do, and every controller since has copied it. This project has repeatedly found that framework defaults nobody chose become facts nobody can defend — the resource server's five-minute clock-skew grace is one, and it became [T-0019](../../product/tickets/T-0019-token-clock-skew.md) purely because it was unrecorded. A layering convention is the same failure at a larger scale: it binds every ticket from here on, and right now a future engineer asking "why?" finds nothing.

Two facts shape the answer more than general principle:

- **The generated abstract controller is already a boundary.** Clean Architecture's outer ring exists to stop delivery concerns leaking inward. Here the delivery contract is *generated from `spec/openapi.yaml`* and cannot be hand-edited ([ADR-0004](ADR-0004-contract-first-openapi-code-generation.md)), so routes, status codes and DTOs are already unable to leak into hand-written code. The port exists; it is simply not shaped the way the pattern assumes.
- **The test strategy does not want mocks.** [TESTING.md](../../standards/TESTING.md) puts the weight on an integration tier driving real HTTP against real PostgreSQL. Every defect the last two tickets produced — a migration backfill default, a response media type, an `UPDATE … RETURNING` clause, a control character reaching the database — is invisible to a unit test over a mocked repository. A service layer whose main benefit is mockability would buy a kind of test this project has decided it does not rely on.

Against that: **`IssuesController` now contains genuine domain logic.** Allocating a per-project issue number is an invariant with a correctness argument — a single `UPDATE … RETURNING` inside a transaction, chosen over a sequence for reasons recorded in [T-0005](../../product/tickets/T-0005-create-and-read-issues.md). It is not request handling, it is written in raw SQL, and its column name is a string that nothing checks against the entity: rename `NextIssueNumber` on the model and the code still compiles, ships, and fails at runtime.

## Decision

**Controllers take `GotIssuesDbContext` directly and remain thin. Domain invariants are extracted into named types when they arise — and the issue-number allocator is one.**

Concretely:

1. **No service or repository layer by default.** A controller that reads, maps and returns may do so against `DbContext`. Adding a pass-through service that only forwards a query is not an improvement and should be rejected in review.
2. **An invariant gets a type.** Logic that must hold regardless of who calls it — allocation, uniqueness, state transitions with rules — moves out of the controller into a named type in `apps/GotIssues.Api/Domain/`. The test is not "is this more than five lines", it is **"would this still have to be true if a second caller did it?"**
3. **The issue-number allocator moves** under rule 2, in the next ticket that touches issue creation. It carries the raw SQL with it, and it takes the column-name fragility with it into one place where a test can pin it.
4. **This is revisited when a second entry point needs the same logic.** [T-0007](../../product/tickets/T-0007-list-and-filter-issues.md) needs issue lookup; [T-0006](../../product/tickets/T-0006-issue-lifecycle-fields.md) needs issue mutation. If sharing turns awkward, that is evidence, and this ADR is superseded rather than quietly ignored.

## Options Considered

**1. Thin controllers over `DbContext`, invariants extracted — proposed.** Matches what the architecture document already says, keeps the layer count honest, and puts a type exactly where there is something to protect. *Costs:* no single seam to enforce cross-cutting rules through; a reviewer must judge rule 2 case by case, and "would a second caller need this to hold?" is a judgement rather than a lint.

**2. Full Clean Architecture — domain, application, infrastructure projects; repository interfaces; controllers depending only on abstractions.** Genuinely better when domain logic is substantial, when persistence may be swapped, or when the domain needs testing in isolation. **Rejected for this system, now:** the controllers are 132 and 126 lines including documentation; persistence will not be swapped ([ADR-0003](ADR-0003-initial-technology-stack.md) fixes PostgreSQL and [PROJECT.md](../../PROJECT.md) §4 rules out anything else); and the isolation it buys is isolation from the database, which is where this project's actual defects live. The honest summary is that it would add three projects and a mapping layer to protect against changes this system has decided not to make. It is the right answer for a different system, and possibly for this one later.

**3. A service layer without the rest of Clean Architecture** — `ProjectService`, `IssueService`, still using `DbContext` internally. **Rejected as the worst of both:** it adds a layer without a boundary, and in practice the methods would be `CreateIssueAsync` calling the same three lines the controller calls today. It is the shape that gets adopted because it looks like good practice, and it is the one that most reliably becomes pass-through. If the argument for it is testability, note that mocking it tests the mock.

**4. Leave it unrecorded.** Rejected on the specific grounds that this project keeps paying for unrecorded defaults. The maintainer had to ask the question, which is itself the evidence.

## Consequences

### Positive

- The convention is written down, so the next controller is a decision rather than a copy.
- The allocator lands somewhere a test can pin its column name — closing a real sharp edge rather than a theoretical one.
- No speculative layers: the project keeps the option of adopting option 2 later, when there is domain logic to justify it, and this ADR names the trigger.

### Negative

- **Rule 2 is a judgement call.** Two reasonable engineers will disagree about whether a given piece of logic is an invariant, and there is no mechanical check. The wording is chosen to make the argument short, not to remove it.
- **Nothing prevents a controller growing fat.** The rule is enforced by review, and this repository's own history says review catches things late rather than never.
- **If persistence ever must be swapped, this is the expensive choice**, and the cost would be spread across every controller rather than concentrated in a repository layer.

## Risks

- **The most likely failure is drift in the permissive direction:** an invariant that "isn't quite big enough yet" stays in a controller three tickets running. The mitigation is that rule 3 gives the pattern one concrete instance to imitate; a rule with no example tends not to be followed.
- **`apps/GotIssues.Api/Domain/` could become a junk drawer** if rule 2 is read as "anything non-trivial". It is deliberately narrower: logic that must hold for *every* caller.

## Follow-up Actions

- If Accepted: extract the issue-number allocator in the next ticket touching issue creation, and record it there. Add a line to [ENGINEERING.md](../../standards/ENGINEERING.md) via `evolve-governance` so review has something to point at.
- If Rejected: the alternative chosen should say what replaces it, because "keep it as it is" is this ADR's option 4 and is the one thing already ruled out.

## Related ADRs

- [ADR-0004](ADR-0004-contract-first-openapi-code-generation.md) — the generated contract that already provides the delivery boundary
- [ADR-0003](ADR-0003-initial-technology-stack.md) — fixes PostgreSQL, which removes the swap-the-database argument
- [ADR-0008](ADR-0008-role-restrictions-declared-in-the-contract-enforced-by-policy.md) — the other convention governing what belongs in a controller

## Related Tickets

Frontmatter links follow acceptance; a `Proposed` ADR should not read as governing tickets that have not been told about it.

- [T-0005](../../product/tickets/T-0005-create-and-read-issues.md) — where the allocator lives today
- [T-0006](../../product/tickets/T-0006-issue-lifecycle-fields.md) — the next ticket to touch issue creation, and the natural home for rule 3
- [T-0007](../../product/tickets/T-0007-list-and-filter-issues.md) — the second entry point that will test rule 4
