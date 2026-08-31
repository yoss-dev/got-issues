---
id: T-0020
title: Make a 500 correlatable with the log line that explains it
type: technical
status: backlog
priority: normal
owner: none
implemented_by: none
accepted_by: none
depends_on: [T-0004]
adrs: []
created: 2026-08-31
updated: 2026-08-31
---

# T-0020: Make a 500 correlatable with the log line that explains it

## Problem / Context

Found by independent acceptance of [T-0004](T-0004-create-and-list-projects.md)
(`claude-qa-4d18`, 2026-08-31) and confirmed by review (`claude-rev-3e77`).

T-0004 added an exception handler so an unanticipated failure returns a problem document
instead of an empty body. It fixed the response and left a gap behind it:

- **Every other problem document in this API carries `traceId`** — ASP.NET Core's validation
  and status-code responses include it automatically.
- **The 500 does not**, because the handler serialises a `ProblemDetails` it constructs itself,
  and nothing adds the trace identifier.
- **The log line carries no correlation identifier either.**

So a caller reporting a 500 can be matched to the stack trace that explains it **by timestamp and
nothing else** — on the one response where the caller cannot see what went wrong and the operator
is the only one who can. Every response that *is* diagnosable from the outside carries the
identifier; the one that is not, does not.

This is not a contract defect: the `Problem` schema does not declare `traceId`, so nothing is
promised and unfulfilled. It is an operability defect, and it appears precisely where operability
matters most.

## Desired Outcome

An operator handed a 500's response body can find the log line that explains it, without guessing
from a timestamp.

## User / Business Value

The failure this affects is the one nobody can diagnose from the outside. For an internal tool
whose whole premise is running your own infrastructure, "the API returned 500 at about half past
two" is the difference between a five-minute answer and an afternoon of log grepping.

## Scope

### In Scope

- The 500 problem document carries a correlation identifier.
- The log line written when the exception is handled carries the **same** identifier.
- A test proving the two match — not that each exists separately.
- A decision on whether `traceId` belongs in the `Problem` schema, since the API already returns
  it on other failures and the contract is silent about it. **This scope line explicitly accepts
  that question**; it overlaps [T-0017](T-0017-automated-contract-conformance-tier.md)'s
  `errors`/`traceId` item and the two must not both assume the other settles it.

### Out of Scope

- Distributed tracing, an exporter, or a correlation identifier threaded through outbound calls —
  there are no outbound calls.
- Structured logging conventions generally.
- Changing what the 500 body says beyond the identifier: it must keep leaking nothing about the
  failure ([SECURITY.md](../../standards/SECURITY.md), and T-0004's tests assert it).

## Acceptance Criteria

- [ ] AC1: Given a request that fails unexpectedly, when the response is inspected, then its problem document carries a correlation identifier.
- [ ] AC2: Given that same request, when the API's log output is inspected, then a line carries the **same** identifier — asserted by matching the two values, not by asserting each is non-empty.
- [ ] AC3: Given the 500's body, when it is inspected, then it still carries nothing about the failure's cause — no exception type, message, connection string or user input ([SECURITY.md](../../standards/SECURITY.md); T-0004's smoke check already asserts this and must stay green).
- [ ] AC4: Given the `Problem` schema, when the decision in Scope is taken, then it is recorded in the ticket and reflected in `spec/openapi.yaml` if the answer is that `traceId` is declared.

## Examples / Scenarios

- Database stopped under a live API: the 500's identifier appears in a log line naming the Npgsql failure.
- Two failures in quick succession: two identifiers, each matching its own log line — the case a timestamp cannot separate and this ticket exists for.
- A validation failure (400): unchanged; it already carries `traceId`.

## Dependencies

**T-0004** — the exception handler this concerns.

## Risks / Unknowns

- **`traceId` is already emitted on other responses and undeclared in the contract.** Whatever is
  decided here must agree with what [T-0017](T-0017-automated-contract-conformance-tier.md)
  decides about undeclared members, or the two produce contradictory rules. Neither ticket may
  assume the other has settled it.
- **The obvious implementation is a matching pair of assertions that never compare.** A test
  asserting the body has an identifier and the log has an identifier passes when they differ,
  which is the whole defect. AC2 is worded to forbid that.
- The trace identifier ASP.NET Core uses is per-request and not stable across a retry; that is
  fine for this purpose and worth stating so nobody builds more than is needed.

## Testing Notes

The smoke tier is where T-0004's 500 is exercised (`UnhandledFailureTests`), because it needs a
failure raised under a live stack. Log output there means container logs rather than an in-process
capture, which is the non-obvious part of this ticket.

## Relevant ADRs & Documentation

- [T-0004](T-0004-create-and-list-projects.md) — where the handler was added and the gap found
- [T-0017](T-0017-automated-contract-conformance-tier.md) — the overlapping `traceId` question
- [SECURITY.md](../../standards/SECURITY.md) — what the body must never carry

## Definition of Ready

- [ ] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — not yet refined.

## Definition of Done

- [ ] Meets [DoD](../../governance/DEFINITION_OF_DONE.md) — checked by `complete-ticket`; deviations require recorded PO/human approval.

---

## Work Log

### 2026-08-31 — Product Owner (claude-sm-9d4e)

- **Did:** Created from T-0004's acceptance and review notes, as the destination [DoD](../../governance/DEFINITION_OF_DONE.md) item 4 requires for that ticket's deferral — created *before* `complete-ticket`, not after, because a deferral counts only once its destination exists and accepts it.
- **Decided:** scope explicitly accepts the `traceId`-in-the-contract question rather than leaving it between this ticket and T-0017, where each would assume the other had it.
- **Remaining:** refinement.
- **Open questions / blockers:** none.
- **Test state:** n/a — not started.
