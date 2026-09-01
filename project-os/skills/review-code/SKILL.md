---
name: review-code
description: Independently review a ticket's change-set before merge - correctness, tests, scope fidelity, standards and ADR compliance - and record an approve/request-changes verdict.
---

# Skill: review-code

## Purpose

Give every merge to the trunk an independent engineering check: the change does what the ticket asks, to standard, without scope drift or undocumented decisions — before it becomes part of `main`.

## When to Use

A ticket's PR is open and awaiting review (the handover sequence in `standards/GIT.md`: Work Log → PR → **review** → merge → status commit). Review MUST be performed by a session other than the implementer's (identity per `standards/GIT.md`); self-review does not satisfy WoW §10.

## Active Persona(s)

Software Engineer; add the Architect perspective for changes touching system boundaries, data models, dependencies, or cross-cutting structure.

## Inputs

- The ticket ID and its branch/PR (recorded in the ticket's Work Log at handover).

## Preconditions

- Ticket is `in-progress` with an open PR noted in the Work Log; you did not implement this change.
- The branch is current with the trunk (or the diff against trunk is otherwise clean to read).

## Context to Load

1. The ticket — requirements sections first (Scope, Acceptance Criteria, Examples), then the Work Log
2. The full diff of the branch against the trunk
3. `standards/ENGINEERING.md`, `standards/TESTING.md`, `standards/GIT.md`; `standards/SECURITY.md` when the change touches auth, input handling, secrets, or dependencies
4. ADRs referenced by the ticket, plus any ADR covering the touched components (scan the [ADR index](../../architecture/adr/README.md))

## Procedure

1. **Scope fidelity first:** walk the diff against In Scope / Out of Scope. Anything outside scope, or any acceptance criterion with no corresponding change, is a finding — regardless of code quality.
2. **Correctness:** read the change for logic errors, unhandled edge cases (compare against the ticket's Examples/Scenarios), error handling, and concurrency/state hazards. Run the test suite yourself if the environment allows.
3. **Tests:** do the new tests genuinely encode the acceptance criteria, or pass vacuously? Is every fixed bug covered by a regression test? Is test code held to production standards (`standards/TESTING.md` binds the harness too)?
   - **Ask what input reaches the code under test.** An assertion is evidence about a path only if the input gets there; a green run does not tell you whether it did.
   - **Ask for a mutant when you doubt a coverage claim — and only then.** A challenge is a legitimate trigger and the answer is a mutant rather than an argument; but do not require re-mutation of a claim whose code has not changed shape, and do not ask for a mutant where a compiler, constraint or framework already enforces the property (`standards/TESTING.md`).
   - **When re-reviewing a fix for "this is satisfied by anything", enumerate what else satisfies the replacement.** A narrower predicate looks like progress while the deciding question goes unasked. In SPRINT-002 the implementer *and* the reviewer both walked into this on the same ticket, one round apart.
4. **Claims: pick the load-bearing ones in the diff and run them.** A comment, Work Log entry, ADR sentence, mutation record or commit message asserting that a mechanism guarantees something is a claim, and it is verified by executing the mechanism, not by reading it (`standards/TESTING.md`, *Run the claim, don't read it*). Load-bearing means a reader would act on it: it explains why a guard is safe, why a test is sufficient, or why an alternative was rejected.
   - **A false claim is a blocking finding**, at the same level as a defect in behaviour. Across SPRINT-003, eleven of eighteen blocking findings were claims that this check reaches; three were defects in shipped behaviour.
   - **Look hardest inside fixes and corrections.** Three of [T-0006](../../product/tickets/T-0006-issue-lifecycle-fields.md)'s ran in sequence, each inside the fix for the previous one.
   - **Check your own review text by the same rule.** One of those claims was written by a reviewer while approving the fix for this exact fault. If you assert what a tool does, you have run it.
   - **When the fix is a reworded claim, ask whether a mechanism delivers the guarantee a reader would act on.** If one does, correcting the description is the right fix; if none does, a corrected sentence describes an absence, and the finding is not closed.
5. **Standards & ADR compliance:** lint/static analysis clean; naming and structure per `ENGINEERING.md`; no undocumented architectural decision in the diff (a decision meeting the [ADR bar](../../architecture/adr/README.md) without an ADR is a blocking finding); commit messages per `GIT.md`.
6. **Record findings** where the implementer will see them (PR comments where a platform exists, otherwise the ticket Work Log), each one concrete: file, issue, why it matters. Distinguish **blocking** (violates criteria, standards, scope, or ADRs) from **suggestions** (take or leave, no re-review needed).
7. **Findings outside the ticket's scope** (pre-existing issues the diff merely reveals) become tickets via `create-ticket` — never review-time scope creep.
8. **Verdict, recorded in the Work Log** (persona + your id): **Approve** (merge may proceed) or **Request changes** (blocking findings listed; the implementer addresses them on the branch and re-requests review). Do not fix the code yourself — that would make you a co-implementer and void your independence for this ticket.

## Validation

- Every acceptance criterion was checked against the diff, not assumed; verdict + findings recorded in the Work Log with your identity; no blocking finding left only in chat or PR state.

## Outputs

An Approve / Request-changes verdict with recorded findings; possibly new tickets for out-of-scope discoveries.

## State Changes

May modify: the ticket's Work Log, PR review state, new tickets via `create-ticket`. MUST NOT modify: the implementation code, acceptance criteria, ticket status (the implementer merges and performs the handover status commit after approval).

## Failure / Escalation

- Implementer disputes a blocking finding → both positions in the Work Log; the relevant standard/ADR text arbitrates; unresolved → escalate per WoW §13.
- The change is unreviewable (giant diff, unrelated changes mixed in) → request changes on that basis alone; splitting is the fix, not a longer review.

## Example

Reviewing T-0031's PR: the diff matches scope, but AC2's invalid-range test asserts only the 422 status code, not the required error body — blocking finding recorded on the PR. A pre-existing N+1 query in the adjacent list endpoint (revealed, not caused, by the diff) becomes T-0039. Verdict: Request changes. The implementer adds the body assertion, re-requests; second pass: Approve, recorded in the Work Log as `ENG (claude-eng-9c1b)`.
