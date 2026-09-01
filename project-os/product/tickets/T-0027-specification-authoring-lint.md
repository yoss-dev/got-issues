---
id: T-0027
title: Lint the specification for authoring completeness, so a fix to one field reaches the next
type: technical
status: backlog
priority: normal
owner: none
implemented_by: none
accepted_by: none
depends_on: [T-0002]
adrs: [ADR-0004]
created: 2026-08-31
updated: 2026-08-31
---

# T-0027: Lint the specification for authoring completeness, so a fix to one field reaches the next

## Problem / Context

From [RETRO-SPRINT-003](../../delivery/retrospectives/RETRO-SPRINT-003.md), action 3 — finding D5:
**the same defect shipped twice, two tickets apart, because the fix was per-field.**

[T-0004](T-0004-create-and-list-projects.md)'s acceptance found that a project name containing
`U+0000` produced an undeclared HTTP 500 with a zero-length body — PostgreSQL rejects the character
(SQLSTATE 22021) and the input should never have reached it. The fix was correct and deliberately
addressed the class rather than the character: `name` gained a `pattern` in `spec/openapi.yaml`
excluding `U+0000`–`U+001F` and `U+007F`, so the rule lives in the contract and reaches generated
clients, per [ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md).

Two tickets later, [T-0006](T-0006-issue-lifecycle-fields.md) added `assignment.subject` — a new
string field — and its first review finding was titled *"T-0004's defect, recurring in a field I
added two tickets later"*. Same crash, same cause, new field.

**Nothing checks that a new string property in the specification declares a pattern.** The T-0004 fix
protected one field and had no way to protect the next. [T-0017](T-0017-automated-contract-conformance-tier.md)
validates *responses* against the declared schema and does not reach specification-authoring
completeness, so this has no existing home.

## Desired Outcome

Adding a string field to `spec/openapi.yaml` without deciding its permitted characters fails a gate,
so the decision is made once per field rather than rediscovered by an acceptance run.

## User / Business Value

No user-visible change directly, but the defect it prevents is user-visible: an undeclared 500 with
an empty body, from input a client had no way to know was invalid. Every future ticket adds string
fields — [T-0007](T-0007-list-and-filter-issues.md) adds filters, [T-0008](T-0008-comment-on-an-issue.md)
adds comment bodies — so the value grows with each one, and the cost of the alternative is a repeat
of the same acceptance failure.

## Scope

### In Scope

- A check over `spec/openapi.yaml` requiring every `string`-typed property in `components/schemas`
  to declare either a `pattern`, a `format` that constrains the character set (`uuid`, `date-time`,
  `email`), or an `enum`.
- **An explicit opt-out that is recorded in the specification itself** — a marker (for example
  `x-unconstrained-text: true` with a required reason) for fields that genuinely accept anything,
  so a deliberate decision is visible and a forgotten one is not.
- Wiring as a gate: extend `tools/check-drift.sh` or add a sibling script, and add it to
  [GIT.md](../../standards/GIT.md)'s merge-gate list — and to `tools/gates.sh` if
  [T-0026](T-0026-self-reporting-gate-runner.md) has landed by then.
- Bringing the existing specification to green, which means auditing every current string field and
  either constraining it or opting it out with a reason.

### Out of Scope

- Request or response *runtime* validation — the generated contract already enforces declared rules
  server-side, and [T-0017](T-0017-automated-contract-conformance-tier.md) owns response conformance.
- Choosing the right pattern for each field. The check requires that a decision exists; it does not
  second-guess which decision.
- Non-string constraints (numeric bounds, array limits). A wider lint is a later decision; this
  ticket closes the defect that actually recurred.
- Any change to the endpoints or their behaviour.

## Acceptance Criteria

- [ ] AC1: Given `spec/openapi.yaml` as it stands, when the check runs, then it exits 0 — every existing string property either carries a constraint or a recorded opt-out.
- [ ] AC2: Given a **newly added** string property with no `pattern`, `format`, `enum` or opt-out, when the check runs, then it exits non-zero and names the schema and property. Demonstrated by adding one, watching it fail, and removing it.
- [ ] AC3: Given a property marked with the opt-out, when the check runs, then it passes **only if** the marker carries a reason — an opt-out with no stated reason is a silent exemption and must fail.
- [ ] AC4: Given the check is run in a state where the specification is unreadable or absent, then it fails loudly rather than passing vacuously. A lint that finds nothing to complain about because it read nothing is the failure mode that matters.
- [ ] AC5: Given [GIT.md](../../standards/GIT.md), when it is read after this ticket, then the check is in the gate list and an agent following it runs the check without having to know it exists.
- [ ] AC6: Given the T-0004 defect is reintroduced — remove the `pattern` from `CreateProjectRequest.name` — then the check fails and names that property. This is the specific regression the ticket exists to prevent, so it is asserted directly rather than inferred from AC2.

## Examples / Scenarios

- **Must catch:** `assignment: { subject: { type: string } }` added with no pattern — exactly [T-0006](T-0006-issue-lifecycle-fields.md) B1.
- **Must catch:** a comment body added by [T-0008](T-0008-comment-on-an-issue.md) with no constraint — the next instance waiting to happen.
- **Must pass:** `Issue.key`, which declares `pattern: '^[A-Z][A-Z0-9]{1,9}-[1-9][0-9]{0,8}$'`.
- **Must pass with a reason, and fail without one:** a free-text description field that deliberately allows newlines — a real case; `Issue.description` already uses a permissive pattern rather than none, which is the shape this ticket generalises.

## Technical Notes

The specification is hand-authored YAML ([ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md)),
so a small Python check using the same YAML parsing as the existing tooling fits the repository better
than a general-purpose OpenAPI linter with a rule set to configure and suppress. The existing
`tools/validate-project-os/validate.py` is the closest precedent for style, but note it validates
`project-os/` process state and this check reads `spec/` — a sibling script is likely the cleaner
home, and the same question was left open on [T-0025](T-0025-documentation-truth-sweep.md).

Watch the schema shapes actually in use: properties appear under `components/schemas/*/properties`,
inside `oneOf` branches (`UpdateIssueRequest.assignment`), and inside `items`. A check that only walks
the top level would pass the very field whose absence caused T-0006 B1.

## Dependencies

**[T-0002](T-0002-contract-first-codegen-pipeline.md)** — done; it established the specification and
the drift gate this would sit beside.

## Risks / Unknowns

- **The opt-out is the whole risk.** If it is easy and reasonless, every field gets it and the check
  becomes decoration. AC3 exists for this and should be treated as the hard criterion.
- **Bringing the current spec to green may surface real decisions** — a field nobody has thought about
  is the point of the exercise, but it means this ticket can uncover work rather than only prevent it.
  Report rather than absorb.
- **Overlap with [T-0025](T-0025-documentation-truth-sweep.md) and [T-0026](T-0026-self-reporting-gate-runner.md)**:
  three tickets now propose adding a check. If they are done together, they should share a home and a
  convention rather than each inventing one.

## Testing Notes

AC2 and AC6 are mutations: reintroduce the defect, watch the check catch it, revert. AC6 is the more
important of the two because it names the historical defect rather than a synthetic one — a check
that passes AC2 on a made-up field and fails AC6 has not closed the recurrence.

## Relevant ADRs & Documentation

- [ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md) — why validation rules belong in the contract rather than a controller
- [RETRO-SPRINT-003](../../delivery/retrospectives/RETRO-SPRINT-003.md) — finding D5, the evidence
- [T-0004](T-0004-create-and-list-projects.md) — the original defect and its per-field fix
- [T-0006](T-0006-issue-lifecycle-fields.md) — the recurrence, two tickets later
- [T-0017](T-0017-automated-contract-conformance-tier.md) — the adjacent tier, and why it does not cover this

## Definition of Ready

- [ ] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — not yet refined. The opt-out design is what refinement should close.

## Definition of Done

- [ ] Meets [DoD](../../governance/DEFINITION_OF_DONE.md) — checked by `complete-ticket`.

---

## Work Log

### 2026-08-31 — Created from RETRO-SPRINT-003 action 3 (claude-sm-9d4e)

- **Did:** Created to hold the retro's third action, from finding D5 — the identical `U+0000` defect shipping in [T-0004](T-0004-create-and-list-projects.md) and again in [T-0006](T-0006-issue-lifecycle-fields.md) two tickets later.
- **Decided:** framed as *authoring* completeness rather than validation, to keep it clearly distinct from [T-0017](T-0017-automated-contract-conformance-tier.md), whose scope covers responses and explicitly not this.
- **Remaining:** refinement, where the opt-out marker is the question that decides whether the check is real.
- **Open questions / blockers:** none blocking. Note that this is the third proposed check ([T-0025](T-0025-documentation-truth-sweep.md), [T-0026](T-0026-self-reporting-gate-runner.md)); whoever schedules them should decide whether they share a home.
- **Test state:** n/a — not started.
