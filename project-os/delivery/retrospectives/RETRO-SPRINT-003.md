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

> **Corrected 2026-09-01, before the action was merged.** The figures first published here were **11 of 19**, and both terms were wrong: the count mixed blocking with non-blocking findings and omitted six blocking ones. Caught by `claude-rev-8b12` reviewing the change this table justifies, and recounted from the three tickets' own verdict lines — [T-0004](../../product/tickets/T-0004-create-and-list-projects.md) 7, [T-0005](../../product/tickets/T-0005-create-and-read-issues.md) 4, [T-0006](../../product/tickets/T-0006-issue-lifecycle-fields.md) 7 = **18**. The conclusion strengthened; the arithmetic was still wrong, and a retro that miscounts its own evidence has no standing to require measurement of anyone else. The original figures are struck rather than deleted.

| Kind | Count | The findings |
| --- | --- | --- |
| **Shipped behaviour was wrong** | 3 | T-0004 F1 (`U+0000` → undeclared 500 with no body); T-0005 B1 (migration numbers the first issue in any existing project `GOTI-0`); T-0006 B1 (the same `U+0000` defect in a field added two tickets later) |
| **What the repository recorded was false or incomplete** | 13 | *False statements (9):* T-0004 B2, F2, C1; T-0005 B3, F1; T-0006 B3, F1, C1, C3. *Missing or silently incomplete records (4):* T-0004 B1 (a binding rule living in one controller's XML comment), C2 (a hazard recorded nowhere its inheritor would look); T-0005 B2 and T-0006 B2 (the contract silent on behaviour the API has) |
| **A test did not reach what it was aimed at** | 2 | T-0004 B3 (two criteria naming a field, neither test opening the body); T-0006 C2 (a regression test satisfied by the defect it was written for) |

**Thirteen of eighteen blocking findings — 72% — were defects in what the repository *says*, not in what it does.** Nine were statements that were simply false; the other four were records that were missing or said less than the truth. Both halves mislead a reader who acts on them, which is why they are counted together and separated within the row.

**Two numbers, answering different questions — and only one belongs in the rule.** 13 of 18 is this sprint's *defect profile* and is what D1 reports. But two of those four missing records — [T-0004](../../product/tickets/T-0004-create-and-list-projects.md) B1 (a binding rule correct in itself, living in a controller's XML comment) and C2 (a hazard recorded nowhere its inheritor would look) — have **no sentence to execute**, so the technique named in action 1 could never have found them; they are [DoD](../../governance/DEFINITION_OF_DONE.md) item 4's territory. **11 of 18 is what this rule reaches**, and that is the figure carried into [TESTING.md](../../standards/TESTING.md) and [`review-code`](../../skills/review-code/SKILL.md). Citing 13 there would have been the defect the standard's own *mutation record states what its mutant proves* rule describes: a true number doing work it cannot support. The distinction is `claude-rev-8b12`'s, raised after this retro's author asked it to challenge the classification. And the shape recurs *inside its own fixes*: [T-0006](../../product/tickets/T-0006-issue-lifecycle-fields.md) ran F2 → C1 → G1/G2, each one a false claim inside the fix for the previous false claim. [T-0004](../../product/tickets/T-0004-create-and-list-projects.md) named the pattern in its own Work Log — *"a claim about where evidence can come from, made without measuring… I reasoned about what a tool would do instead of watching it"* — and explicitly flagged it for this retro.

The reviewer's caution at [T-0006](../../product/tickets/T-0006-issue-lifecycle-fields.md) was that three instances on one claim-dense ticket is a weak sample for a rule. **Tested against the other two tickets, it holds:** T-0004 recorded three instances of its own, T-0005 four. It is a sprint-wide pattern, not a T-0006 artefact.

Two further facts sharpen what the action should be:

- **Mutation testing produced none of these findings.** Not one of the 18 blocking findings came from mutating. The two mutants run during T-0006 were *prompted by* someone checking a claim, then used to confirm it. This is consistent with the narrowed mutation mandate approved 2026-08-31, and is the first evidence about it.
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

**P1 — the deferred question from RETRO-SPRINT-002, now answerable.** That retro asked whether [`acceptance-test`](../../skills/acceptance-test/SKILL.md) should require the mutation and attribution practices its acceptors already use. This sprint's evidence changes the answer: **acceptance found 3 of 3 post-review defects using state-based exploration, and mutation found none of the 18 blocking findings.** Requiring mutation in `acceptance-test` would mandate the technique that produced nothing and leave unnamed the two that produced everything. The question should be closed as *no* — and replaced by naming the techniques that worked (action 1).

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
| 1 | **Name the technique that found two-thirds of this sprint's defects.** Add to [TESTING.md](../../standards/TESTING.md), beside *"exercise the system in a state it was not built in"*: **"Run the claim, don't read it."** When a comment, Work Log entry, ADR or ticket asserts that a mechanism guarantees something — *"this fails loudly if…"*, *"the FK enforces this"*, *"the test gates the build"* — the claim is verified by executing the mechanism and observing the outcome, and a claim that cannot be executed is rewritten to what can. Add the check to [`review-code`](../../skills/review-code/SKILL.md) and [`acceptance-test`](../../skills/acceptance-test/SKILL.md) step lists as *"pick the load-bearing claims in the diff and run them"*. Evidence: D1 — 13 of 18 blocking findings, on all three tickets, and the reviewer made the error too. | maintainer (approval) + agent (drafting) | `evolve-governance` proposal — **project-agnostic, worth upstreaming per [FOUNDATION.md](../../FOUNDATION.md)** |
| 2 | **Make the gates self-reporting instead of writing the exit-code rule a third time.** One script — `tools/gates.sh` — that runs all six merge gates in order, captures each exit code directly, prints a table, exits non-zero if any gate failed, and **refuses to run against a dirty tree and fails if the tree changes during the run**. This closes D3 (no pipeline can eat a status), D4 (no concurrent mutation, and no gate verdict taken while its subject moves), the gate-ordering deviation, and the "cited the reviewer's measurement as my own" finding, because running one command produces your own evidence. | agent (ENG) | **[T-0026](../../product/tickets/T-0026-self-reporting-gate-runner.md)** |
| 3 | **Lint the specification for authoring completeness, so a fix to one field applies to the next one.** Every `string` property in `spec/openapi.yaml` declares a `pattern` (or an explicit, commented opt-out); wired into `check-drift.sh` or as a sibling gate. Evidence: D5 — the identical `U+0000` defect shipped in T-0004 and again in T-0006 two tickets later, and [T-0017](../../product/tickets/T-0017-automated-contract-conformance-tier.md)'s scope does not reach it. | agent (ENG) | **[T-0027](../../product/tickets/T-0027-specification-authoring-lint.md)** |

**Deliberately not actions.**

- **A rule requiring mutation in acceptance** — P1: rejected on this sprint's evidence rather than deferred again. RETRO-SPRINT-002's open question is hereby closed.
- **A fourth documentation sweep** — D2 already has [T-0025](../../product/tickets/T-0025-documentation-truth-sweep.md), created during the sprint with the mechanism in scope. Adding a retro action would duplicate it.
- **A governance change about lane discipline** — P3: the rule was correct and was enforced. Marking `73a1833` was the fix and it is done.
- **A rule forbidding work during a gate run** — folded into action 2 as a *mechanical* check rather than a prohibition. A rule that forbids all repository work during a ten-minute smoke run has a real cost in a serialised solo workflow, and the sprint file records that counter-argument; a script that detects the condition costs nothing.

---

## Governance changes applied — 2026-08-31

**Action 1 was approved by the maintainer (human) on 2026-08-31**, in the words *"apply action 1"*, and applied via [`evolve-governance`](../../skills/evolve-governance/SKILL.md). Owning personas: QA / Test Engineer for [TESTING.md](../../standards/TESTING.md), Scrum Master for the skills.

**The precondition holds.** [`evolve-governance`](../../skills/evolve-governance/SKILL.md) flags any change that would make an in-flight ticket newly pass a gate. There is no in-flight ticket — SPRINT-003 closed before this ran — and the change makes verification strictly *stricter*: it adds a class of blocking finding.

| # | Change | Artifacts touched |
| --- | --- | --- |
| 1 | **"Run the claim, don't read it."** A sentence asserting that a mechanism guarantees something is verified by executing the mechanism and observing the outcome, not by reading the code and reasoning about what it must do — comments, Work Log entries, ADR sentences, mutation records and commit messages alike. A claim that cannot be executed is rewritten to one that can, and a claim found false is corrected in place rather than deleted. Six worked examples, each with what was claimed and what measurement showed. | [TESTING.md](../../standards/TESTING.md) — new section between *Verification must be attributable* and *The gate* |
| 2 | **`review-code` picks the load-bearing claims in the diff and runs them**, as a new step 4: a false claim is a **blocking** finding at the same level as a defect in behaviour; look hardest inside fixes; check your own review text by the same rule; and when a fix is a reworded claim, ask whether a mechanism delivers the guarantee a reader would act on. | [review-code](../../skills/review-code/SKILL.md) — new step 4; former steps 4–7 renumbered 5–8 |
| 3 | **`acceptance-test` runs the load-bearing claims**, as a bullet in step 3 beside *put the running system into a state it was not built in*. | [acceptance-test](../../skills/acceptance-test/SKILL.md) — step 3 |

**Reviewed by `claude-rev-8b12`** on branch `gov-run-the-claim` — **Request changes**, five blocking findings, all closed before merge. The review is recorded in full below. Three are worth surfacing here because they change what was applied:

- **The change contained a false claim about a false claim — twice.** The `acceptance-test` bullet first credited acceptance with finding T-0006 B3, which **code review** raised on its first pass. The replacement then described T-0006 **C1** while calling it acceptance's — and C1 was raised by the acceptance-fix *review* pass. Both were caught by `claude-rev-8b12` walking T-0006's entry headers in order and building a provenance table, rather than reading the finding text and inferring who wrote it. The bullet now cites **G1 and G2**, verified against that table: acceptance measured that a guard documented as failing the build left `dotnet build` at exit 0, and that a test's stated predicate did not match the one it implemented.

  **This is worth more than the fix.** Getting a finding's provenance wrong three times in one change — plus the earlier misattribution of the false line at T-0006:1762 — says the failure is not carelessness about *this* fact. Attribution is exactly the kind of claim the new rule targets: it reads as recalled rather than checked, and there is a cheap mechanism (the Work Log's own entry headers, in order) that settles it in seconds. The technique the action names would have caught every one of them, applied to itself.
- **The evidence count was wrong** — see the correction on D1 above.
- **The scope extension was reversed.** The applied change also added a bullet to [`implement-ticket`](../../skills/implement-ticket/SKILL.md); the approved action named three artifacts, not four. It was flagged in the commit message rather than hidden, but the reviewer's objection is correct and better than the flag: [WoW §15](../../governance/WAY_OF_WORKING.md) makes approval a **precondition**, not a label, and RETRO-SPRINT-002 declined a structurally identical fourth change one sprint ago and routed it to the next retro. Reverted. **It is re-proposed as a candidate for SPRINT-004's retro** — the argument stands that a rule living only in the checker skills implies someone else will catch an implementer's false claims, and 13 of 18 findings say implementers write most of them; it needs its own approval and a narrower wording, since the reverted bullet said *"every claim you write"* where the other two skills scope to **load-bearing** claims.

### A conflict found while applying this, recorded and deliberately not resolved

[WoW §3](../../governance/WAY_OF_WORKING.md) requires that a detected conflict be followed to the higher-precedence source and **recorded**, and forbids editing a governance document to dissolve a conflict mid-task. Both apply here.

**[GIT.md](../../standards/GIT.md) and [`evolve-governance`](../../skills/evolve-governance/SKILL.md) disagree about how a governance change reaches the trunk.** GIT.md's *Governance path protection* entry, marked `[confirmed]`, says changes to `project-os/{governance,standards,templates,skills}` travel **lane 2 — branch and reviewed merge**. `evolve-governance` step 3 permits *"in solo mode, a direct commit with the approval recorded in the change log entry"*. This project **is** in solo mode, so the two genuinely conflict rather than addressing different cases.

Standards rank **7** and skills rank **8** in [WoW §3](../../governance/WAY_OF_WORKING.md)'s precedence order, so GIT.md wins and this change went via a branch and an independent review — which is also how the reviewer's five findings were found, so the more expensive path paid for itself on its first use.

**The three prior governance changes did not.** `b3b83b8`, `b18fd47` and `2006cf2` are all single-parent trunk commits touching `standards/` and `skills/` — verified by `git log --format=%p`, not assumed. They are **unmarked deviations from the higher-precedence rule**, and they are recorded as such here for the same reason `73a1833` was marked in [T-0005](../../product/tickets/T-0005-create-and-read-issues.md)'s Work Log during SPRINT-003: an unmarked deviation gets cited as precedent, and citing it launders it into a rule. Two of this session's mistakes came from exactly that.

**Not resolved here, by rule.** Which document should change is a real question — requiring branch-and-review for a typo fix is heavy, and `evolve-governance`'s own *clarity-only* class already anticipates that — but resolving it mid-task is what [WoW §3](../../governance/WAY_OF_WORKING.md) forbids. It goes to SPRINT-004's retro as a proposal, with the observation that the choice is between narrowing GIT.md by change class and deleting the skill's solo-mode clause.

---

## Review of the action-1 change

### 2026-08-31 — Software Engineer (claude-rev-8b12) — review of `gov-run-the-claim` @ `0ed3714` — **Request changes**

Reviewed under [`review-code`](../../skills/review-code/SKILL.md) against `main`, with
[`evolve-governance`](../../skills/evolve-governance/SKILL.md) as the applicable procedure. Not the
implementer. I applied the rule the change adds to the change itself: every historical assertion in
the new text was run against the repository or the Work Log it cites, and two were run against the
build.

**Verdict: Request changes.** Five blocking findings. The rule itself is right, its six worked
examples are all true, and the recorded conflict analysis is correct in every part I could check —
the problems are one false claim inside the new text, one arithmetic claim that does not survive
being run, two required `evolve-governance` steps not performed, and an unapproved fourth change.

#### What I verified rather than read

| Claim in the new text | How checked | Result |
| --- | --- | --- |
| *"the test fails the build" — it fails `dotnet test`; `dotnet build` exits 0* (T-0006 G1) | Added `Unspecified = 0` to `IssueStatus` in a scratch copy; ran both gates, exit codes read from the tool | **True.** `dotnet build --no-incremental` → exit 0, 0 warnings. `dotnet test` → exit 1, `IssueLifecycleEnumTests` failing and naming the member and column |
| *"xUnit runs the classes in parallel" — they share one collection* (T-0005 F4) | Grepped the integration project; no `xunit.runner.json`, no assembly-level parallelism attribute | **True.** All ten integration classes carry `[Collection(PostgresFixtureDefinition.Name)]`, so they run sequentially |
| *"the foreign key enforces this criterion" — measured, it produced a 500 where the criterion required a 400* (T-0006 B3) | [T-0006](../../product/tickets/T-0006-issue-lifecycle-fields.md) Work Log, implementer's mutant and the reviewer's re-run | **True**, and measured twice: `Expected: BadRequest, Actual: InternalServerError` |
| *"this fails loudly if someone later adds a zero member" — it silenced the warning that had been reporting the problem* (T-0006 C1) | T-0006 Work Log, C1 | **True** |
| *"the integration tier structurally cannot host this assertion" — it could* (T-0004 C1) | [T-0004](../../product/tickets/T-0004-create-and-list-projects.md) Work Log, C1 and its closure | **True**, and the narrower replacement was itself measured by the reviewer |
| *"this closes the class, not just the instance" — the class stayed open* (T-0005 B3) | [T-0005](../../product/tickets/T-0005-create-and-read-issues.md) Work Log, B3 | **True** |
| *mutation produced none of them* | The three Work Logs; T-0006 states it explicitly for its own ten | **True** as far as the record shows |

Six for six. The section's examples are the strongest part of the change and none of them needed
correcting.

#### Blocking

**R1 — the `acceptance-test` bullet attributes a review finding to acceptance. This is a false
claim inside the rule against false claims.**

`skills/acceptance-test/SKILL.md` step 3 now reads: *"acceptance found several that two and three
review passes had read past — **including a mutation record attributing a criterion's enforcement to
a foreign key that in fact produced the wrong status code**."*

That is T-0006 B3, and B3 was raised by **code review, first pass** — `### 2026-08-31 — Code review
(claude-rev-7a03) — ENG · ARCH — Request changes`, T-0006 Work Log line 515, with B3 at line 612
under that entry's `### Blocking`. No review pass read past it; it was the first thing to find it,
before acceptance ever ran. The sentence is exactly the shape the new TESTING.md section describes:
a correct-sounding inference from an adjacent fact (B3 *was* about a mutation record; acceptance
*did* find several claims late) written without opening the log.

The clause is also placed where it does the most damage — it is the one sentence in the change that
tells an acceptor what this technique is *for them*, and it credits acceptance with a finding
acceptance did not make.

**Fix:** substitute a finding acceptance actually made. T-0006 G1 and G2 both qualify and are
better examples anyway: G1 is the *"fails the build"* claim, found by acceptance after **four**
review passes, one of which had written the same claim while approving the fix for it.

**R2 — "eleven of nineteen blocking findings" does not survive being run against the tickets.**

The figure appears three times in the diff — [TESTING.md](../../standards/TESTING.md) (*"mutation
produced none of the nineteen blocking findings, and eleven of them were false claims"*),
`review-code` step 4, and `acceptance-test` step 3 — and it is transcribed from D1 above rather than
recounted. Counted from the tickets' own verdicts:

| Ticket | Blocking findings, per the ticket's own verdict lines |
| --- | --- |
| T-0004 | B1, B2, B3 (*"Three blocking findings"*), F1, F2 (*"Two blocking findings"*), C1, C2 (*"Two blocking findings remain"*) — **7** |
| T-0005 | B1, B2 (*"Two blocking findings"*), B3 (*"One blocking finding"*), F1 (*"one blocking finding"*) — **4** |
| T-0006 | B1, B2, B3, C1, C2, C3 (the ticket states *"review raised six blocking findings (B1, B2, B3, C1, C2, C3)"*), F1 (*"FAIL — one blocking finding (F1, DoD item 6)"*) — **7** |

**Eighteen, not nineteen** — and with different membership. D1's list includes seven findings the
tickets record as explicitly **non-blocking** (T-0005 F3 and F4; T-0006 F2, F3, F4, G1, G2 — T-0006's
acceptance entry heads them *"The three non-blocking findings, all taken"*, and its PASS entry says
*"raised four non-blocking findings"*) while omitting six that are blocking (T-0004 B1, F2, C2;
T-0005 B2; T-0006 B2, F1). The arithmetic is self-consistent — 18 − 6 + 7 = 19 — which is why it
reads as sound.

**The conclusion survives; the number does not.** Classifying the eighteen actual blocking findings:
ten are a false claim or a false document (T-0004 B2, C1, F2; T-0005 B3, F1; T-0006 B3, C1, C3, F1,
and T-0004 C2 as a false pointer), three more are a document that is silently incomplete rather than
wrong (T-0004 B1's missing ADR, T-0005 B2, T-0006 B2), three are defects in shipped behaviour
(T-0004 F1, T-0005 B1, T-0006 B1), and two are a test that did not reach its target (T-0004 B3,
T-0006 C2). *"Two-thirds of blocking findings were defects in what the repository says"* holds under
any of these readings. **"Four defects in shipped behaviour" is true only if non-blocking findings
are counted** — T-0006 F3 is the fourth, and it is non-blocking.

**Fix:** either restate the population honestly (*"of the twenty-five findings recorded across the
three tickets, blocking and not…"*), or recount against blocking findings only and use the numbers
that come out. Do not carry a number into a rule about running claims without running it. D1 itself
should carry the correction, since the table is the source the three documents quote.

**R3 — `evolve-governance` steps 4 and 6 were not performed, and the precedent is 3 for 3 the other
way.**

- **Step 4, durable record:** *"an entry in the current retro … date, change, reason, approver."*
  This document's closing section still reads *"Pending — action 1 requires maintainer approval …
  before any document changes."* Nothing anywhere in the repository records that the maintainer
  approved it, when, or in what words; the approval exists only in the commit message body of
  `0ed3714` and in the session that produced it. That is [WoW §16](../../governance/WAY_OF_WORKING.md)'s
  *decisions that exist only in chat*, and the Validation section of `evolve-governance` names
  *"approval recorded for rule-content changes"* explicitly.
- **Step 6, foundation classification:** action 1's own row in the table above designates it
  **project-agnostic, worth upstreaming per [FOUNDATION.md](../../FOUNDATION.md)**. No row was
  added to the contribution table. All eight prior governance changes have one, including the three
  this change most resembles.

`b3b83b8`, `b18fd47` and `2006cf2` each carried the document changes, the `FOUNDATION.md` row and the
retro/sprint record **in one commit**. Choosing lane 2 splits that — the retro and `FOUNDATION.md`
are delivery/lineage state — but splitting it is not the same as dropping it, and right now the half
that records *why the rulebook changed* does not exist. Land both, and say in the retro entry how
the two commits relate so the pair is auditable.

**R4 — the conflict is recorded nowhere in the repository, and the analysis is correct.**

I checked all three parts of it and the implementer is right on each:

- [`standards/GIT.md`](../../standards/GIT.md) line 81 (`[confirmed]`, project-specific): changes to
  `project-os/{governance,standards,templates,skills}` *"still travel **lane 2** (branch + reviewed
  merge)"*, restating the universal lane rule at line 31. Correct.
- `evolve-governance` step 3 permits *"in solo mode, a direct commit with the approval recorded in
  the change log entry"*, and its first clause is conditioned on path protection *"where path
  protection exists"* — GIT.md line 81 confirms no CODEOWNERS. So the skill does permit what the
  standard forbids. A real conflict, not an invented one.
- [WoW §3](../../governance/WAY_OF_WORKING.md) ranks `standards/` (7) above `skills/` (8), so GIT.md
  wins. Correct, and it is also the conservative direction — more review, not less.
- `b3b83b8`, `b18fd47` and `2006cf2` are each single-parent commits on `main` touching
  `project-os/skills/` and `project-os/standards/`. Verified by `git log --format=%P`. All three are
  unmarked lane-2 deviations.

**What is missing is the record.** WoW §3 is mandatory: an agent that follows the higher-precedence
source *"MUST … record the conflict in the ticket's Work Log and the sprint's Notes."* There is no
ticket and no active sprint, so this retro is the only available home, and it says nothing. The
finding about the three prior commits is worth more than the resolution — it is a three-instance
unmarked deviation in the rule that protects governance from silent rewrites — and it currently
exists only in a handover message. Record the conflict, the resolution and the three prior
deviations here, the way P3 recorded `73a1833` so nobody laundered it into a rule.

**R5 — the `implement-ticket` extension is outside what "apply action 1" authorises. Reverse it and
re-propose.**

Saying so plainly, as asked: **reverse it.** Not because the argument is wrong — it is the best
argument in the change, and the evidence supports it — but because this project decided this exact
question one sprint ago and decided it the other way. RETRO-SPRINT-002's governance record closes
with:

> *"Whether the skill should require what its practitioners already do is a real question, but it is
> a **fourth** change and was not among the three approved. Recorded here for SPRINT-003's retro
> rather than slipped in alongside them."*

Same shape, same skill-list argument, same "it is obviously right" pull — and that session declined
and routed it to the next retro, where it was in fact answered (P1 above). Flagging is materially
better than hiding, and I want that on the record; but [WoW §15](../../governance/WAY_OF_WORKING.md)
makes approval a precondition of application, not a label applied afterwards, and
`evolve-governance` step 2 requires the owning persona *and* a human for a rule-content change. A
bullet added to a skill's mandatory verification step is a rule-content change.

There is also a substantive reason to reverse rather than wave through. The bullet reads *"**Every**
claim you write is verified by running it"* — unbounded — while `review-code` step 4 and
`acceptance-test` step 3 both scope the same rule to *load-bearing* claims and `review-code` defines
what that means. That is the proportionality the mutation mandate was narrowed for on 2026-08-31,
and the extension is the one place in the change that drops it. If the maintainer approves it next,
it should be approved in the scoped wording, not this one.

#### Non-blocking

**N1 — one numbered reference to `review-code` is now stale.** The renumbering itself is clean and
leaving step 3 alone was right — RETRO-SPRINT-002 line 102 cites *"review-code (step 3)"* and step 3
is untouched. But `T-0004-create-and-list-projects.md:438` reads *"Per review-code §4, an in-diff
decision meeting the ADR bar without an ADR is blocking"*, and that rule is now step **5**. It is the
only other numbered reference in the repository (I grepped all 42 non-skill mentions of
`review-code`). It is a historical Work Log line, so annotate in place per this sprint's own
practice rather than rewriting it — and note that the new step 4 also ends *"is a blocking finding"*,
so the stale pointer half-reads as still correct, which is the worse failure.

**N2 — `gov:` is not a message form GIT.md defines.** [GIT.md](../../standards/GIT.md) *Commit
messages* defines exactly two: `T-NNNN:` for source and `os:` for process. `0ed3714` is the only
`gov:` commit in the repository's history; all three prior governance changes used `os:`. Either
adopt the prefix deliberately in GIT.md or use `os:`. (This entry follows the coordinating session's
instruction to use `gov:`, and the same finding applies to it.)

**N3 — for a future ticket, not this change.**
`apps/GotIssues.Api.IntegrationTests/Infrastructure/PostgresContainerFixture.cs` says *"all **nine**
integration classes carry `[Collection(...)]`"*. There are **ten**. The corrected comment T-0005 F4
produced is right about the mechanism and wrong about the count, and the count is the part that will
drift again as classes are added. On theme, and outside this diff — belongs with
[T-0025](../../product/tickets/T-0025-documentation-truth-sweep.md) if its scope takes it.

#### `evolve-governance` compliance, and the D3 objection

**Atomic:** yes for the rulebook. TESTING.md, `review-code`, `acceptance-test` and
`implement-ticket` are mutually consistent after `0ed3714`; no document is left describing an
older rule; the validator is green (exit 0, 27 tickets, 10 ADRs) and all four new cross-links
resolve. Not atomic for the *record* — see R3.

**Contradictions upward:** none found. The new section sits at standards level (7) below
[WoW](../../governance/WAY_OF_WORKING.md), and nothing in it contradicts WoW §9/§10,
[PROJECT.md](../../PROJECT.md), an accepted ADR or the DoD. Its *"Where this sits relative to
mutation"* paragraph is explicitly compatible with the narrowed mandate rather than quietly widening
it — I looked for that specifically, since a claims rule is an easy place to smuggle mutation back
in.

**Is it exhortation? No — and it is not vulnerable to D3's objection.** D3 rejected another sentence
because the sentence already existed, was well written, and was read by the agent that broke it; the
remedy there was mechanical because the failure is mechanically detectable (a pipeline eating an exit
code). Neither half transfers. This rule does not exist yet — D1's whole point is that the technique
is *named nowhere* — and no script can execute an English sentence to see whether it is true, so the
mechanical remedy is unavailable rather than declined. What makes it a rule rather than advice is one
line: **`review-code` step 4's "a false claim is a blocking finding", at the same level as a defect
in behaviour.** That names a gate and a consequence, and `review-code` bounds it to load-bearing
claims and says what load-bearing means. Keep that sentence exactly as written; it is doing the work.

The one place the objection *does* land is R5's bullet, which is an unbounded "verify everything you
write" with no gate attached — which is what exhortation looks like.

#### To close this review

1. Fix the `acceptance-test` attribution (R1).
2. Recount or restate the population, in D1 and in all three documents that quote it (R2).
3. Add the retro record with approver and date, and the `FOUNDATION.md` contribution row (R3).
4. Record the conflict, its resolution, and the three prior unmarked deviations (R4).
5. Revert the `implement-ticket` bullet and carry it to SPRINT-004's retro as a proposal (R5).

N1–N3 are take-or-leave and need no re-review. Re-request when 1–5 are on the branch; I will re-run
the two executable claims and the count.

### 2026-09-01 — Software Engineer (claude-rev-8b12) — re-review of `gov-run-the-claim` @ `4b5e992` — **Request changes**

Second pass, same reviewer, still not the implementer. R2, R3, R4 and R5 are closed, and three of
them closed better than I asked. **R1 is not closed.** The replacement is half right: one of its two
examples is acceptance's, the other is the review pass's, and it is described in the vocabulary this
change's own standard assigns to the review finding. You asked me to check the replacement rather
than take it, and this is why that was the right request.

#### R1 — not closed. The replacement moved the misattribution one finding over.

`acceptance-test` step 3 now reads:

> *"Acceptance found claims that review had read past: on [T-0006], after two review passes had
> approved it, acceptance measured that **(a)** a comment claiming a guard "fails loudly" described
> the opposite of what the mechanism did, and **(b)** a test's stated predicate did not match the one
> it implemented."*

I walked T-0006's entry headers in order and fixed the provenance of every finding named:

| Entry | Line | Actor | Verdict | Findings |
| --- | --- | --- | --- | --- |
| Code review, first pass | 515 | `claude-rev-7a03` | Request changes | B1, B2, **B3** |
| Code review, second pass | 847 | `claude-rev-7a03` | **Approve** | — |
| Acceptance, round 1 | 1064 | `claude-qa-2e64` | FAIL | F1 (blocking), F2, F3, F4 |
| **Code review, acceptance-fix pass** | **1432** | **`claude-rev-7a03`** | **Request changes** | **C1**, C2, C3 |
| Code review, third pass | 1718 | `claude-rev-7a03` | **Approve** | — |
| Acceptance, round 2 | 1959 | `claude-qa-2e64` | PASS | **G1**, **G2**, G3, G4 |

**(b) is right.** G2 — *"the guard's discovery predicate is wider than its own description"* — is
`claude-qa-2e64`'s, raised in the round-2 acceptance entry at line 2077. Acceptance's own, measured
against the model (`GetDefaultValue()` non-null for `Number`, `Id`, `CreatedAt` with no
`Relational:DefaultValue` annotation). Correctly attributed.

**(a) is C1, and C1 was review's.** *"Fails loudly"* is not G1's phrase; it is the `HasSentinel`
comment's, quoted in this change's own TESTING.md as C1's bullet — *"this fails loudly if someone
later adds a zero member" — **it did the opposite**; it silenced the warning that had been reporting
the problem ([T-0006] C1)*. Clause (a) reproduces both halves of that gloss, "fails loudly" and "the
opposite". C1 was raised by `claude-rev-7a03` in the **acceptance-fix review pass** at line 1432,
under its `### Blocking` heading at line 1499. Acceptance never saw it; the reviewer found it in the
implementer's fix for acceptance's F2.

**G1 is not describable as "the opposite".** G1 is *"the guard does not fail the build, and two
places say it does"* — the guard *does* fail, and *does* name the member; it fails `dotnet test`
while `dotnet build --no-incremental` exits 0. Wrong gate, not inverted behaviour. The round-2
acceptance entry draws the distinction itself: **"This is C1's shape at one lower amplitude, inside
C1's own fix."** Same shape, different finding, different finder.

So the sentence still credits acceptance with a review finding — the second time in this change, and
the phrasing came from the same place as the first: an adjacent, correct-sounding formulation
already in the neighbouring document, reused without opening the log it points at. The `TESTING.md`
bullet list gets both C1 and G1 exactly right; only the `acceptance-test` prose merges them.

**Fix — state G1 as G1.** Something close to: *"…acceptance measured that a comment claiming the
guard "fails the build" was wrong about which gate — `dotnet build` exits 0 and only `dotnet test`
fails — and that a test's stated predicate did not match the one it implemented."* That is two
genuine round-2 acceptance findings, both after two approving review passes, and the first is a
sharper example anyway: the same claim also stood in the **reviewer's** own approving entry at line
1762, which is the datum the next bullet in `review-code` depends on.

**One clause I checked and it holds:** *"after two review passes had approved it"* is exact — lines
847 and 1718 are the two Approvals preceding round-2 acceptance. Better than the *"two and three
review passes"* it replaced.

#### R2 — closed, and the challenge you invited has an answer

The recount is right; I get the same 18 from the same verdict lines, and the 9/4 split inside the
middle row is the honest move — a contract that is *silent* and a comment that is *false* mislead a
reader differently.

**On T-0004 B1 and C2: your instinct is right, and the reason is sharper than "misplaced rather than
untrue."** The test is not whether the record is false. It is **whether this technique could have
found it.** *Run the claim, don't read it* takes a sentence and executes the mechanism it names.
There is no sentence to execute in C2 — the hazard is recorded nowhere — and none in B1, where a
correct decision sits in a controller's XML comment instead of an ADR. **Those two are invisible to
the rule they are being cited to justify.** They are evidence for [DoD](../../governance/DEFINITION_OF_DONE.md)
item 4's false-pointer rule, which already exists and already fired on this sprint (P5).

The other two in that row **are** reachable: T-0005 B2 declares a 403 and never says who is refused,
and T-0006 B2 describes two of four distinguishable inputs — both are checkable assertions about
behaviour that exercising the API falsifies. So the technique's actual yield is **11 of 18**, and it
is a real 11 this time rather than the coincidence the first one was.

**Both numbers are true; they answer different questions.** *13 of 18* is the sprint's defect
profile and belongs in D1, where it now sits, correctly. *11 of 18* is what this rule can find, and
that is the number doing evidentiary work in [`review-code`](../../skills/review-code/SKILL.md)
step 4 and [TESTING.md](../../standards/TESTING.md), where a reader takes it as "this is what the
technique catches." Non-blocking, because what is written is true as stated — but this change's own
standard is that *a record that overstates its mutant is the same defect as an assertion that
overstates its subject*, and one clause fixes it: cite 13 for the profile and name the 11 the
technique reaches.

#### R3, R4, R5 — closed

- **R3.** Approver, date, approving words, owning personas and the precondition are all in
  *Governance changes applied*; the [FOUNDATION.md](../../FOUNDATION.md) row is present and its
  figures match the recount. The change-table format matches RETRO-SPRINT-002's precedent.
- **R4.** Recorded, correctly, and **not resolved** — which is the right call and the harder one.
  I re-verified the three prior commits are single-parent (`git log --format=%P`) and that GIT.md
  itself supports the solo-mode premise (*"A remote alone does not end solo mode… This project is in
  that state deliberately"*), which [PROJECT.md](../../PROJECT.md) §6 confirms. Marking the three
  alongside `73a1833` is the same remedy P3 chose, applied to the rule that protects governance.
- **R5.** [`implement-ticket`](../../skills/implement-ticket/SKILL.md) is byte-identical to `main`
  (`git diff main` empty). Re-proposed with the load-bearing scoping. Nothing further owed.

#### Non-blocking

**N4 — the correction note on D1 is wrong about its own chronology.** It reads *"Corrected
2026-08-31, before this retro's action was applied."* The action was applied in `0ed3714` at
2026-08-31 23:47; the correction is `4b5e992` at **2026-09-01 00:00:46**. So it is dated a day early
*and* it came after the application, not before — it came before the **merge**, which is the true and
more interesting claim. This retro's own P4 is about exactly this drift making chronological
reconstruction wrong. One clause: *"Corrected 2026-09-01, after the action was applied and before it
reached the trunk."*

**N5 — carried forward, unchanged.** `PostgresContainerFixture.cs` says nine integration classes
where there are ten; agreed it belongs to a ticket and not to a governance change. The `gov:`/`os:`
prefix plan is right — correcting it at the squash without rewriting history is the better of the two
options, and this entry uses `gov:` for the same reason the last one did.

#### What I re-ran

The six worked examples in [TESTING.md](../../standards/TESTING.md) are byte-identical to the ones I
verified at `0ed3714` (`git diff d981d12 4b5e992 -- standards/TESTING.md` touches only the mutation
paragraph and its new source note), so the two executable claims stand as measured: a zero enum
member leaves `dotnet build --no-incremental` at exit 0 and takes `dotnet test` to exit 1, and all
ten integration classes share one xUnit collection. Counts re-derived from the tickets' verdict
lines: 7 + 4 + 7 = 18. `validate.py` exit 0 — 27 tickets, 10 ADRs.

#### To close

One thing: **R1**, stated as G1. N4 in the same commit since one is being made. N5 needs no action
here. Re-request and I will check the replacement against T-0006's entry table above — third time,
and by then the provenance of every finding on that ticket is written down in one place, which is
most of what made this hard.

### 2026-09-01 — Software Engineer (claude-rev-8b12) — third review of `gov-run-the-claim` @ `d90c9d3` — **Approve**

**R1 is closed.** Checked against the provenance table rather than against the fix description:
`awk` over lines 1959–2262 of [T-0006](../../product/tickets/T-0006-issue-lifecycle-fields.md)
confirms **G1** (*"the guard does not fail the build, and two places say it does"*) and **G2**
(*"the guard's discovery predicate is wider than its own description"*) both sit inside the
`### 2026-08-31 — QA / Test Engineer (claude-qa-2e64) — re-acceptance` entry. Both are acceptance's.
No review entry intervenes.

The description now matches the finding rather than a neighbouring one. *"A guard documented as
failing the build in fact left `dotnet build` at exit 0 and failed only `dotnet test`"* is G1's
fault stated as G1's fault, and it is the one claim in this change I verified by execution rather
than by reading: with a zero enum member present, `dotnet build --no-incremental` exits 0 with no
warnings and `dotnet test` exits 1 naming the member and column. The published sentence and the
measurement agree.

**Your reading of the three misattributions is better than mine and I want it on the record.** I
reported them as three findings; you recorded one, and the one is right. All three came from
inferring authorship from finding *text* when a cheap mechanism — the Work Log's entry headers, in
order — settles it exactly. That is the rule's own subject: a claim that reads as recalled rather
than checked, with an executable check available. A governance change whose three defects were all
instances of the fault it names is the strongest evidence the section could have, and D1 now carries
it.

**Counts.** `review-code` and [TESTING.md](../../standards/TESTING.md) now say **11 of 18**, with
TESTING.md spelling out why 13 is not the figure a reader of *this rule* should act on, and D1
keeping 13 as the sprint's defect profile with the boundary written out. Re-derived: 9 false
statements + T-0005 B2 + T-0006 B2 = 11 reachable; T-0004 B1 and C2 have no sentence to execute.
Both numbers now say what they can support.

**Everything I was told did not change, did not change.**
`git diff main -- skills/implement-ticket/SKILL.md` is **empty**; the six worked examples in
TESTING.md are **byte-identical** to those measured at `0ed3714`; `validate.py` **exit 0** — 27
tickets, 10 ADRs.

#### `evolve-governance` and the precedence check, final

Atomic across the rulebook (TESTING.md, `review-code`, `acceptance-test` mutually consistent; no
document left describing an older rule; every cross-link resolves). Record complete: approver,
approving words, date, owning personas, the precondition, the change table, and the
[FOUNDATION.md](../../FOUNDATION.md) contribution row. Nothing contradicts
[WoW](../../governance/WAY_OF_WORKING.md), [PROJECT.md](../../PROJECT.md), an accepted ADR or the
DoD; the mutation relationship is stated as complementary rather than widened. The conflict is
recorded, followed upward, and routed rather than resolved. **Approve — merge may proceed.**

#### Two suggestions, take or leave; no re-review

- **S1 — the [FOUNDATION.md](../../FOUNDATION.md) row is the one place the 13/11 distinction is not
  drawn.** It cites *"13 were defects in what the repository recorded"* under **Evidence**. True, and
  it is the copy that travels to other projects, where a maintainer reads the evidence line as the
  rule's yield — which is the inflation the other two documents just corrected. One clause: *"13
  were defects in what the repository recorded; 11 of those are claims this technique reaches."*
- **S2 — one paraphrase inside quotation marks.** `acceptance-test` quotes the predicate's
  description as *"every enum column carrying a database default"*; `IssueLifecycleEnumTests.cs:38`
  reads *"Every enum property in the model that carries a database default"*. Property, not column,
  and G2 is partly *about* that word. Trivial, and in a document about quoting accurately.

#### On the merge: squash with an `os:` message — do that

Ruling, since you asked rather than assumed. **Squash, as you propose.**

- [GIT.md](../../standards/GIT.md) sets squash-merge as the default *and* confirms it in the
  project-specific section. Deviating needs a recorded reason and there is none; "the reviewer would
  like their commits kept" is not one.
- `os:` is right and `gov:` was the deviation. GIT.md defines exactly two message forms and the
  squash is where that gets corrected without rewriting history — including my three, which is the
  better outcome for me as well as for you.
- **Nothing the framework relies on lives in commit topology.** GIT.md is explicit that *"the Work
  Log verdict **is** the review record"*, and independence is judged by comparing identifiers
  (WoW §2). All three verdicts sit in this section under `claude-rev-8b12`, dated, with their
  evidence. Squashing loses no part of the record the rules ask for.

Two things to put in the squash message, both because GIT.md's stated rationale for `os:` is that
the delivery history stays readable from `git log` alone:

1. **Say it took three review rounds.** A single trunk commit otherwise reads as a first-pass
   approval, and the fact that the rule's own technique caught three defects in the rule is the most
   useful thing in this branch.
2. **Name the lane exception you are using.** The squashed commit carries governance paths
   (`skills/`, `standards/` — lane 2) together with delivery state (this retro, `FOUNDATION.md` —
   lane 1). GIT.md permits exactly one mixing exception, for a Work Log riding with the change it
   describes; this retro entry is that exception's direct analogue and I agree it applies. Given R4
   is about unmarked lane deviations being cited later as precedent, say so in the message rather
   than leaving the next reader to infer it.

Nothing further owed to me. Good change, and better for the three rounds.
