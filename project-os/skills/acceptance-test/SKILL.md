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
