---
name: refine-ticket
description: Review a backlog ticket from Product, Engineering, QA, Architecture, and (as applicable) UX/Security perspectives; improve it; and honestly evaluate it against the Definition of Ready.
---

# Skill: refine-ticket

## Purpose

Drive a ticket toward genuinely Ready: remove ambiguity, discover missing scenarios and business rules, sharpen acceptance criteria, surface dependencies and architectural questions, and size the work — or conclude honestly that it is *not* ready and say why.

## When to Use

- A `backlog` ticket is nearing the top of the backlog.
- A `ready` ticket was invalidated by new information (re-refinement).
- Before sprint planning, for candidate tickets.

## Active Persona(s)

Business Analyst leads; the agent explicitly rotates through Product Owner, Software Engineer, QA, and Architect perspectives, adding UX and Security where the ticket touches UI or security-relevant surfaces. Note in the Work Log which perspectives were applied.

## Inputs

- Ticket ID to refine.

## Preconditions

- Ticket exists with `status: backlog` (or `ready` for re-refinement) and is not `committed`/`in-progress` — refining in-flight work is a scope change (WoW §7), not refinement.

## Context to Load

1. The ticket file
2. `governance/DEFINITION_OF_READY.md`
3. `PROJECT.md` (constraints, stack, open questions)
4. `product/PRODUCT_VISION.md` and `product/USER_PERSONAS.md`
5. `architecture/ARCHITECTURE.md` + ADRs plausibly touching the ticket's area (scan the [ADR index](../../architecture/adr/README.md))
6. `standards/TESTING.md`; `standards/SECURITY.md` if security-relevant
7. Tickets listed in `depends_on` (status and outcome)

## Procedure

1. **Product pass (PO):** is the outcome still valuable and vision-aligned? Is desired outcome unambiguous? Would the PO recognize "done" from the criteria alone?
2. **Analysis pass (BA):** hunt ambiguity and missing business rules. For each acceptance criterion ask: *could two reasonable implementers build different things and both claim compliance?* If yes, tighten it. Add concrete *Examples / Scenarios*, especially edge cases (empty, maximum, concurrent, unauthorized, malformed) and counter-examples ("explicitly NOT expected to…").
3. **Engineering pass (ENG):** is it implementable against the current codebase and stack? Are there hidden dependencies? Fill Technical Notes with pointers, clearly marked as suggestions where they aren't constraints.
4. **Architecture pass (ARCH):** does it raise a decision meeting the [ADR bar](../../architecture/adr/README.md)? If yes: either spawn `create-adr` (decision is clear enough), record it in the ticket as a blocking question, or propose a spike ticket. A ticket with an open architectural question this size is **not Ready**.
5. **QA pass (QA):** is every criterion independently verifiable? Fill Testing Notes: levels, non-obvious setup, what can't be automated and how it will be checked instead.
6. **UX / Security passes (if applicable):** interaction described well enough to build without inventing UX? Auth/personal-data/input concerns named, with criteria where needed?
7. **Size it.** If it exceeds the DoR size guideline, split along outcome seams: create new tickets via `create-ticket`, link them, and shrink or drop this one.
8. **Evaluate the DoR** item by item. Then set the result:
   - all items pass → `status: ready`, tick the DoR checkbox, note the evaluation in the Work Log;
   - gaps an agent can't close → keep `backlog`, record precisely what's missing and who can supply it (question for PO/human, awaited ADR, dependency). **Never mark Ready to keep work moving.**
9. Update `updated` in frontmatter and mirror any status change in `BACKLOG.md`. Add a Work Log entry (perspectives applied, changes made, DoR verdict).

## Validation

- Every acceptance criterion passes the "two implementers" test.
- Risks/Unknowns reflects what refinement actually found (an empty section after refinement is a claim).
- If marked `ready`: every universal DoR item verifiably holds and applicable conditional items are addressed.

## Outputs

Improved ticket; DoR verdict with reasons; possibly split tickets, an ADR (proposed), a spike proposal, or recorded questions.

## State Changes

May modify: the ticket file, `product/BACKLOG.md` (status/ordering rows), new tickets via `create-ticket`, ADRs via `create-adr`. MUST NOT change the ticket's fundamental intent — that is a PO decision escalated per WoW §13.

## Failure / Escalation

- Missing business rule no document answers → record the question in the ticket, keep `backlog`, escalate to human PO if it blocks upcoming planning.
- Ticket's premise conflicts with an Accepted ADR or `PROJECT.md` constraint → do not "fix" the ticket to dodge the conflict; record it and escalate (precedence conflict, WoW §3).

## Example

Refining `T-0031` (invoice date filtering): BA adds scenarios for empty ranges, future dates, and timezone boundaries; QA rewrites "filtering works" into "given invoices on Mar 1 and Apr 1, filtering Mar 1–31 returns only the March invoice (user's timezone)"; ENG notes the list endpoint already supports pagination parameters; ARCH confirms no ADR needed (existing query patterns); size fits. DoR passes → `ready`, Work Log records all five perspectives.
