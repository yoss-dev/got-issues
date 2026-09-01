---
name: acceptance-test
description: Independently verify a ticket in in-acceptance against its acceptance criteria and the applicable DoD, by exercising the software — never by trusting the implementer's claims.
---

# Skill: acceptance-test

## Purpose

Give the team an honest, independent answer: does this implementation actually deliver the ticket's outcome? Catch defects, scope drift, and quality-gate misses before Done.

## When to Use

A ticket is `in-acceptance`. Acceptance MUST be performed by a session other than the one that implemented (WoW §2/§9) — a fresh agent, or at minimum a fresh session with no implementation context beyond the repository.

## Active Persona(s)

QA / Test Engineer, with the Product Owner perspective for the "does this satisfy the *intent*?" judgment.

## Inputs

- Ticket ID in `in-acceptance`.

## Preconditions

- Ticket `status: in-acceptance` with a handover Work Log entry; the change-set is identifiable (commits referencing the ticket).
- You did not implement this ticket in this session: your identity (per `standards/GIT.md`) differs from the ticket's `implemented_by`.

## Context to Load

1. The ticket — **read the requirements sections (Problem, Outcome, Scope, Acceptance Criteria, Examples) BEFORE the Work Log**, and derive your own expectations first, so the implementer's narrative doesn't anchor you
2. `governance/DEFINITION_OF_DONE.md`
3. `standards/TESTING.md`; other standards as the change's area demands
4. ADRs referenced by the ticket
5. The diff/commits for the ticket

## Procedure

1. **Derive acceptance scenarios from the requirements alone:** per criterion, the concrete checks that would prove it, plus the Examples/edge cases from refinement, plus scenarios the ticket implies but doesn't spell out (empty states, invalid input, permissions, regressions in adjacent behavior).
2. **Execute:** run the full relevant automated suite yourself (exact commands from `standards/TESTING.md` / project docs) — do not accept a pasted green run; exercise the software directly where feasible (run the app, call the API, walk the UI); inspect the new tests: do they genuinely encode the criteria, or pass vacuously? A test that can't fail is a finding.
3. **Explore adversarially** around the change: boundaries, misuse, interaction with existing features. Time-box this; depth proportional to risk.
   - **Put the running system into a state it was not built in.** This is where this project's most serious defects have actually been found, and none of them would have been caught by any test in the suite, because every test starts from a state the code was designed for. Specifically worth doing: input nobody anticipated (a control character, a value at and just past a declared bound); **a database that already holds rows** — revert to the previous schema, seed it, run the real migrator, and see what the migration did to what was already there; and a dependency removed underneath a live service.
   - **Run the load-bearing claims rather than reading them.** The ticket, its Work Log and the diff's comments assert that mechanisms guarantee things — *"the constraint enforces this"*, *"the guard fails the build"*, *"this closes the class"*. Execute the mechanism and observe the outcome (`standards/TESTING.md`, *Run the claim, don't read it*). In SPRINT-003 eleven of eighteen blocking findings were claims this check reaches, against three in shipped behaviour. Acceptance found claims that review had read past: on [T-0006](../../product/tickets/T-0006-issue-lifecycle-fields.md), after two review passes had approved it, acceptance measured that a guard documented as failing the build in fact left `dotnet build` at exit 0 and failed only `dotnet test`, and that a test's stated predicate — "every enum property in the model that carries a database default" — did not match the one it implemented.
   - Two of SPRINT-003's worst defects came from exactly these — an undeclared 500 with an empty body, and a migration that would have made every existing project's first issue unreadable (`standards/TESTING.md`, *Exercise the system in a state it was not built in*).
4. **Check DoD items observable at this stage:** scope fidelity (In Scope done, Out of Scope untouched — diff-check), regression test present for bugs, ADR present if the Work Log shows an architectural decision, docs updated, no debug leftovers.
5. **Classify every failure:** *implementation defect* (behavior violates a criterion/standard) vs. *requirement ambiguity* (criterion legitimately readable two ways — an implementation matching a reasonable reading is not a defect; the *criterion* is). Record each: what was done, expected (with the criterion quoted), observed, severity.
6. **Verdict, in the Work Log (as QA persona):**
   - **Pass:** every criterion verified with evidence (test names, commands, observed behavior) → proceed to `complete-ticket`.
   - **Fail:** defects recorded → `status: in-progress`, `owner: none`, sprint table updated; the implementer (any engineer session) picks it back up.
   - **Ambiguity found:** route the question to the PO persona/human via WoW §13. **You MUST NOT rewrite acceptance criteria to make the implementation pass** — criteria changes are recorded PO decisions, and post-implementation changes deserve extra suspicion.

## Validation

- Every criterion has an explicit verdict + evidence; every failure is reproducible from its description; verdict recorded by QA persona in the Work Log.

## Outputs

Acceptance verdict with evidence; on failure, a precise defect list; on ambiguity, an escalated question.

## State Changes

May modify: the ticket (Work Log, status on fail), `delivery/CURRENT_SPRINT.md` (status), bug tickets via `create-ticket` for out-of-scope defects discovered in *adjacent existing* behavior. MUST NOT modify: implementation code (defects go back to ENG — fixing them yourself destroys independence), acceptance criteria, DoD.

## Failure / Escalation

- Cannot execute the verification (environment broken, credentials missing) → `blocked` with the blocker recorded; never "accept by inspection" as a workaround.
- Implementer disputes a defect → both positions in the Work Log; PO persona (or human) arbitrates against the criterion text.

## Example

Accepting T-0031: QA derives 7 checks from 3 criteria + refinement examples. Suite green when run fresh; but exploratory check "filter with `from` after `to`" returns 500, and AC2 says invalid ranges return 422 with an error body. Verdict: FAIL — one implementation defect (repro: `GET /invoices?from=2026-04-01&to=2026-03-01`; expected 422 per AC2; observed 500). Status back to `in-progress`, owner cleared, sprint table updated.
