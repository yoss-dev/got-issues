# RETRO-SPRINT-003

## Sprint summary

- **Goal:** *"A person can track work: create a project, create issues in it, and move an issue through its life."* — **achieved.** All three committed tickets are `done`, and the MVP works end to end on `main` at [`c14527b`](../sprints/SPRINT-003.md): create a project, create issues inside it, read one by key, change its type, status, priority and assignee.
- **Committed:** 3 tickets ([T-0004](../../product/tickets/T-0004-create-and-list-projects.md), [T-0005](../../product/tickets/T-0005-create-and-read-issues.md), [T-0006](../../product/tickets/T-0006-issue-lifecycle-fields.md)); **Done:** 3; **returned to backlog:** 0; **discovered work items:** 5 ([T-0021](../../product/tickets/T-0021-prove-migrations-against-populated-databases.md), [T-0022](../../product/tickets/T-0022-adopt-clean-architecture-layering.md), [T-0023](../../product/tickets/T-0023-integration-tests-retain-a-connection-per-test-database.md), [T-0024](../../product/tickets/T-0024-spurious-validation-error-on-every-body-taking-endpoint.md), [T-0025](../../product/tickets/T-0025-documentation-truth-sweep.md)).
- **Blocked episodes:** none. **Escalations:** one, answered same-day by the maintainer (sequencing T-0006 before the [ADR-0010](../../architecture/adr/ADR-0010-clean-architecture-layering.md) refactor).
- **Previous retro's actions:** all three **done**, applied 2026-08-31 in one commit, and — unusually — all three are *observable in this sprint's work*:
  - **(1) "a mutant only counts if the build accepts it"** — visibly in force. Every mutation record this sprint states build acceptance explicitly; [T-0006](../../product/tickets/T-0006-issue-lifecycle-fields.md)'s reviewer recorded *"Build-accepted, right assertion, right cause"* before believing a mutant.
  - **(2) "the standards bind the test infrastructure too"** — in force, and **violated three times by the session enforcing it** (see *Defect & rework analysis*, finding D3).
  - **(3) "a narrower assertion is not the fix for 'satisfied by anything'"** — in force and load-bearing: [T-0006](../../product/tickets/T-0006-issue-lifecycle-fields.md) C2 is exactly this rule firing, catching a regression test that passed against the defect it was written for.
  - **The fourth observation RETRO-SPRINT-002 deliberately deferred** — whether [`acceptance-test`](../../skills/acceptance-test/SKILL.md) should require what its practitioners already do — is answered below (finding P1), with this sprint's evidence.

## What worked

**Acceptance is the strongest gate in this process, and it is not close.** Every one of the three tickets passed review and then failed acceptance ([T-0004](../../product/tickets/T-0004-create-and-list-projects.md) F1, [T-0005](../../product/tickets/T-0005-create-and-read-issues.md) F1, [T-0006](../../product/tickets/T-0006-issue-lifecycle-fields.md) F1) — a 3-for-3 record of finding something two or three review passes had missed. The acceptors' distinguishing technique was not diligence but *state*: 505- and 504-row migrations through the real compose migrator, pre-existing rows PATCHed after a schema change, `pg_stat_activity` sampled 100 times mid-run. This is [TESTING.md](../../standards/TESTING.md)'s *"exercise the system in a state it was not built in"*, which RETRO-SPRINT-002 added, and it has now earned its place three more times.

**Independent sessions disagreed with each other usefully, in both directions.** Reviewers overturned implementer claims ([T-0005](../../product/tickets/T-0005-create-and-read-issues.md) B3, [T-0006](../../product/tickets/T-0006-issue-lifecycle-fields.md) B3 and C1); an acceptor overturned an implementer's *diagnosis* while passing the fix ([T-0005](../../product/tickets/T-0005-create-and-read-issues.md) F4 — a leak, not pool contention, measured rather than argued); and a reviewer overturned **its own** earlier approval by finding its own false sentence ([T-0006](../../product/tickets/T-0006-issue-lifecycle-fields.md) G1 attribution). A process where the checker also catches itself is working.

**Correcting in place rather than editing away.** Every false claim this sprint was struck through and annotated, not deleted — [T-0005](../../product/tickets/T-0005-create-and-read-issues.md)'s parallelism diagnosis, [T-0006](../../product/tickets/T-0006-issue-lifecycle-fields.md)'s `HasSentinel` comment and its build-time claim, the misattributed line at T-0006:1762. The Work Logs are consequently usable as evidence for *this document*, which is the whole argument for the practice.

**The contract caught what code review would not have.** Three [T-0006](../../product/tickets/T-0006-issue-lifecycle-fields.md) problems were found by writing the specification before the implementation (Work Log, *"three contract problems the tests found first"*), and [ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md)'s drift gate caught this sprint's worst incident (finding D4).

## What caused friction

**Rework dominated the sprint.** Each ticket ran review → fixes → re-review → acceptance → fixes → re-acceptance, and [T-0006](../../product/tickets/T-0006-issue-lifecycle-fields.md) ran **four review passes and two acceptance rounds** across 2,622 lines of Work Log. Three tickets took eleven review passes between them. The goal was still met, and the quality is real — but the cost is concentrated in one place, and it is the same place every time (see D1).

**The MVP was scoped mid-sprint, and it worked.** The maintainer asked for an MVP "as soon as possible" after planning; the sprint was re-cut to T-0004 + T-0005 + T-0006 as a strict chain. Recording this as friction only because a strict chain removes all slack: nothing could proceed while T-0006 was in its second acceptance round. It was the right call and it did cost the option to parallelise.

**A ticket's own documentation obligation is the least reliable part of the process.** Three consecutive acceptance failures were the same DoD item 6 on the same three lines, and the third was **forecast in writing** in [`eb1432a`](../sprints/SPRINT-003.md) before it happened (D2).

## Defect & rework analysis

**D1 — the recurring defect is a false claim, not false code. This is the sprint's central finding.**

Classifying every blocking finding across the three tickets by what was actually wrong:

| Kind | Count | Examples |
| --- | --- | --- |
| **Shipped behaviour was wrong** | 4 | T-0004 F1 (`U+0000` → undeclared 500); T-0005 B1 (migration numbers the first issue `GOTI-0`); T-0006 B1 (the same `U+0000` defect in a new field); T-0006 F3 (empty `subject` accepted) |
| **A claim, comment, record or document was false** | 11 | T-0004 B2, C1; T-0005 B3, F3, F4; T-0006 B3, F2, C1, C3, G1, G2 |
| **A test did not reach what it was aimed at** | 4 | T-0004 B3; T-0005 F1-adjacent; T-0006 F4, C2 |

**Two-thirds of blocking findings were defects in what the repository *says*, not what it does.** And the shape recurs *inside its own fixes*: [T-0006](../../product/tickets/T-0006-issue-lifecycle-fields.md) ran F2 → C1 → G1/G2, each one a false claim inside the fix for the previous false claim. [T-0004](../../product/tickets/T-0004-create-and-list-projects.md) named the pattern in its own Work Log — *"a claim about where evidence can come from, made without measuring… I reasoned about what a tool would do instead of watching it"* — and explicitly flagged it for this retro.

The reviewer's caution at [T-0006](../../product/tickets/T-0006-issue-lifecycle-fields.md) was that three instances on one claim-dense ticket is a weak sample for a rule. **Tested against the other two tickets, it holds:** T-0004 recorded three instances of its own, T-0005 four. It is a sprint-wide pattern, not a T-0006 artefact.

Two further facts sharpen what the action should be:

- **Mutation testing produced none of these findings.** Not one of the 19 blocking findings came from mutating. The two mutants run during T-0006 were *prompted by* someone checking a claim, then used to confirm it. This is consistent with the narrowed mutation mandate approved 2026-08-31, and is the first evidence about it.
- **The technique that did find them has no name.** Every finding above came from one of two activities: *exercising the system in a state it was not built in* (named in [TESTING.md](../../standards/TESTING.md), demonstrably working) and *taking a sentence that asserts a property and running the mechanism it names* (named nowhere).

**D2 — documentation asserting that built things do not exist: four instances, three fixes, still not closed.**

[T-0004](../../product/tickets/T-0004-create-and-list-projects.md), [T-0005](../../product/tickets/T-0005-create-and-read-issues.md) and [T-0006](../../product/tickets/T-0006-issue-lifecycle-fields.md) each failed acceptance on DoD item 6, on the same three lines of `README.md` and `ARCHITECTURE.md`. T-0006 applied the subtractive fix — delete the enumerations, point at [`BACKLOG.md`](../../product/BACKLOG.md) — and its acceptor then found a **fourth** file, `spec/README.md:9`, still claiming the specification does not exist, falsified by [T-0002](../../product/tickets/T-0002-contract-first-codegen-pipeline.md) long before.

That is the finding, and it disqualifies the fix that was applied: **a sweep repairs the files someone remembers to open, and the defect is precisely the file nobody opens.** Three sweeps have now been tried. [T-0025](../../product/tickets/T-0025-documentation-truth-sweep.md) exists and is scoped as sweep *plus* mechanism, which is candidate (c) from `eb1432a` — recorded in the sprint file as *not* mutually exclusive with the subtractive fix already applied.

**D3 — a written rule was violated three times in one session by the agent enforcing it.**

[TESTING.md:72](../../standards/TESTING.md) says: *"read the exit status of the tool you are checking, not of a pipeline it feeds. `dotnet format … | grep …` reports grep's status."* The rule is precise and names the exact failure. During T-0006's completion, `claude-sm-9d4e` reported gate results from `… | grep`, `… | tail` and `${PIPESTATUS[0]}`-after-an-`echo` **three separate times**, twice recording empty exit codes as if they were evidence.

**The action this points to is not another sentence.** The sentence exists, is well-written, was added by a prior retro, and was read by the agent that broke it. Guidance has been tried; the gap is mechanical.

**D4 — the session broke `main`, and the gate caught it.**

An `os:` commit intended to carry 32 lines of Work Log also deleted all 62 files of `libs/GotIssues.Client` (9,496 lines), because `git add -A` ran while `tools/check-drift.sh` was regenerating `libs/` in a backgrounded gate run ([T-0006](../../product/tickets/T-0006-issue-lifecycle-fields.md) Work Log, `4e261d9`, repaired by `b3242a4`). It never reached `origin`; the drift gate reported it.

Two separable causes, and the second is the systemic one:

1. `git add -A` staged a tree nobody had looked at — the commit message and the commit disagreed by four orders of magnitude.
2. **The gates are not read-only, and nothing says so.** `check-drift.sh` deletes and regenerates the working tree by design, so concurrent repository work corrupts *both* the commit and the gate's verdict. That run's `drift exit=1` may have been an artefact rather than a finding — **a gate result taken while its subject was moving is not evidence in either direction**, which is the same class as D3: a measurement whose conditions were never checked.

**D5 — the same defect shipped twice, two tickets apart, because the fix was per-field.**

[T-0004](../../product/tickets/T-0004-create-and-list-projects.md) F1 fixed `U+0000` → 500 by adding a control-character `pattern` to `name` in the specification. [T-0006](../../product/tickets/T-0006-issue-lifecycle-fields.md) B1 is the identical defect in `assignment.subject`, a string field added two tickets later — the reviewer's own title is *"T-0004's defect, recurring in a field I added two tickets later"*. The fix was correct and unenforceable: nothing checks that a new string field in `spec/openapi.yaml` declares a pattern. [T-0017](../../product/tickets/T-0017-automated-contract-conformance-tier.md) validates *responses* against the schema and does not cover specification-authoring completeness, so this has no home.

## Process & governance observations

**P1 — the deferred question from RETRO-SPRINT-002, now answerable.** That retro asked whether [`acceptance-test`](../../skills/acceptance-test/SKILL.md) should require the mutation and attribution practices its acceptors already use. This sprint's evidence changes the answer: **acceptance found 3 of 3 post-review defects using state-based exploration, and mutation found none of 19 blocking findings.** Requiring mutation in `acceptance-test` would mandate the technique that produced nothing and leave unnamed the two that produced everything. The question should be closed as *no* — and replaced by naming the techniques that worked (action 1).

**P2 — a documented fix was incomplete, and the validator, not the rule, caught it.** RETRO-SPRINT-001 action 4 says archived sprints' relative links *"must gain a `../` level"*. Applied literally while archiving SPRINT-003, one link stayed broken: a link written as a bare sibling path — `retrospectives/RETRO-SPRINT-002.md`, with no leading `../` — has no level to add and needs a *prepend* instead. Small, and exactly on this sprint's theme: the rule described the common case and was read as covering all of them.

Two limitations of [`validate.py`](../../../tools/validate-project-os/validate.py) surfaced while writing this document, neither of which let a defect through, both recorded rather than actioned:

- **It does not check that a row in the Active table is inside the Active table.** [T-0024](../../product/tickets/T-0024-spurious-validation-error-on-every-body-taking-endpoint.md)'s backlog row sat *below* the Completed table from creation until `dd95d06`, with the validator green throughout — and [T-0025](../../product/tickets/T-0025-documentation-truth-sweep.md)'s row was then appended beside it, inheriting the position by pattern-matching a neighbour.
- **Its link extraction does not skip inline code**, so a document that *quotes* a broken link — such as the sentence above, in its first draft — fails validation for describing a defect rather than having one.

Neither is proposed as an action: the first is a real gap but no defect escaped through it, and the second is cosmetic. Both are noted so a future validator change has the evidence to hand.

**P3 — lane discipline held, but only because a reviewer checked precedent instead of accepting an assertion.** [T-0006](../../product/tickets/T-0006-issue-lifecycle-fields.md) put `CURRENT_SPRINT.md` on a ticket branch; the reviewer measured that all 20 of that file's commits are `os:` trunk commits while `ARCHITECTURE.md` has ridden branches six times, and required the split. Separately, `73a1833` from T-0005 was found to be an *unmarked* lane deviation being cited as precedent — now labelled in T-0005's Work Log so nobody launders it into a rule again. **No governance change is proposed here:** the rule was right and was enforced; it is recorded because the enforcement depended on one reviewer's diligence.

**P4 — two Work Log entries are dated `2026-09-01` while the sprint closed on `2026-08-31`** (T-0006, reviewer sessions). Harmless here, but it makes chronological reconstruction wrong for anyone reading the log as a sequence. Not worth an action on its own; noted so it is recognised if it recurs.

**P5 — DoD item 4's deferral rule earned its wording.** It requires whoever defers to read the destination and cite the scope line that accepts the item. T-0006's acceptor followed it exactly and *declined* to attach finding G3 to T-0006 because the scope could not take it, leaving it homeless rather than falsely covered; [T-0025](../../product/tickets/T-0025-documentation-truth-sweep.md) was then created for it. That is the rule working as RETRO-SPRINT-001 intended.

## Improvement actions

| # | Action | Owner | Lands as |
| --- | --- | --- | --- |
| 1 | **Name the technique that found two-thirds of this sprint's defects.** Add to [TESTING.md](../../standards/TESTING.md), beside *"exercise the system in a state it was not built in"*: **"Run the claim, don't read it."** When a comment, Work Log entry, ADR or ticket asserts that a mechanism guarantees something — *"this fails loudly if…"*, *"the FK enforces this"*, *"the test gates the build"* — the claim is verified by executing the mechanism and observing the outcome, and a claim that cannot be executed is rewritten to what can. Add the check to [`review-code`](../../skills/review-code/SKILL.md) and [`acceptance-test`](../../skills/acceptance-test/SKILL.md) step lists as *"pick the load-bearing claims in the diff and run them"*. Evidence: D1 — 11 of 19 blocking findings, on all three tickets, and the reviewer made the error too. | maintainer (approval) + agent (drafting) | `evolve-governance` proposal — **project-agnostic, worth upstreaming per [FOUNDATION.md](../../FOUNDATION.md)** |
| 2 | **Make the gates self-reporting instead of writing the exit-code rule a third time.** One script — `tools/gates.sh` — that runs all six merge gates in order, captures each exit code directly, prints a table, exits non-zero if any gate failed, and **refuses to run against a dirty tree and fails if the tree changes during the run**. This closes D3 (no pipeline can eat a status), D4 (no concurrent mutation, and no gate verdict taken while its subject moves), the gate-ordering deviation, and the "cited the reviewer's measurement as my own" finding, because running one command produces your own evidence. | agent (ENG) | **[T-0026](../../product/tickets/T-0026-self-reporting-gate-runner.md)** |
| 3 | **Lint the specification for authoring completeness, so a fix to one field applies to the next one.** Every `string` property in `spec/openapi.yaml` declares a `pattern` (or an explicit, commented opt-out); wired into `check-drift.sh` or as a sibling gate. Evidence: D5 — the identical `U+0000` defect shipped in T-0004 and again in T-0006 two tickets later, and [T-0017](../../product/tickets/T-0017-automated-contract-conformance-tier.md)'s scope does not reach it. | agent (ENG) | **[T-0027](../../product/tickets/T-0027-specification-authoring-lint.md)** |

**Deliberately not actions.**

- **A rule requiring mutation in acceptance** — P1: rejected on this sprint's evidence rather than deferred again. RETRO-SPRINT-002's open question is hereby closed.
- **A fourth documentation sweep** — D2 already has [T-0025](../../product/tickets/T-0025-documentation-truth-sweep.md), created during the sprint with the mechanism in scope. Adding a retro action would duplicate it.
- **A governance change about lane discipline** — P3: the rule was correct and was enforced. Marking `73a1833` was the fix and it is done.
- **A rule forbidding work during a gate run** — folded into action 2 as a *mechanical* check rather than a prohibition. A rule that forbids all repository work during a ten-minute smoke run has a real cost in a serialised solo workflow, and the sprint file records that counter-argument; a script that detects the condition costs nothing.

---

## Governance changes applied

*Pending — action 1 requires maintainer approval per [WoW §15](../../governance/WAY_OF_WORKING.md) before any document changes. Actions 2 and 3 are tickets and need no approval to exist, only prioritisation.*
