# Way of Working

This document is the project's constitution. It governs how humans and AI agents collaborate to deliver software. It sits at the top of the precedence order: no ticket, standard, skill, or agent preference overrides it.

**Convention:** *MUST / MUST NOT* marks mandatory rules. *SHOULD* marks strong guidelines an agent may deviate from only with a recorded reason. Everything else is advice.

## 1. Principles

1. **Process exists to improve software delivery, not to create ceremony.** If a rule stops paying for itself, change it via [`evolve-governance`](../skills/evolve-governance/SKILL.md) — don't ignore it and don't worship it.
2. **Explicit state over hidden state.** Anything a teammate would need tomorrow lives in a repository file, never only in a conversation.
3. **Outcomes over tasks.** Tickets describe the outcome and its constraints, not the implementation.
4. **Engineering judgment over blind compliance.** Rules bound judgment; they do not replace it. When judgment says a rule is wrong, follow the rule *and* flag it — or escalate.
5. **Small, finished, verified.** Prefer small work items driven to Done over broad work in progress.
6. **Automation over repetition.** A manual step performed three times is a candidate for automation or a skill.

## 2. Roles and personas

Work is performed under explicit personas defined in [PERSONAS.md](PERSONAS.md). An agent MAY adopt several personas across a task but MUST identify the active persona when performing a formal activity (refinement, acceptance, ADR approval, governance change), and MUST respect that persona's authority boundaries. One agent MUST NOT act as both implementer and acceptance tester for the same ticket in the same session; acceptance requires a fresh, independent read of the requirements. Every actor uses the stable identity convention in [`standards/GIT.md`](../standards/GIT.md); implementer/reviewer/acceptor independence is judged by those identifiers.

## 3. Sources of truth and precedence

When instructions conflict, the higher item wins:

1. This document
2. [`PROJECT.md`](../PROJECT.md) — constraints and configuration
3. Accepted ADRs in [`architecture/adr/`](../architecture/adr/README.md)
4. [Definition of Ready](DEFINITION_OF_READY.md) / [Definition of Done](DEFINITION_OF_DONE.md)
5. Sprint goal and commitments in [`delivery/CURRENT_SPRINT.md`](../delivery/CURRENT_SPRINT.md)
6. The ticket's requirements and acceptance criteria
7. [`standards/`](../standards/ENGINEERING.md)
8. Skill instructions in [`skills/`](../skills/README.md)
9. Agent judgment

**Conflict handling (mandatory):** an agent that detects a conflict between two sources MUST NOT silently pick either one. It MUST: (a) follow the higher-precedence source if the conflict is unambiguous, and record the conflict in the ticket's Work Log and the sprint's Notes; or (b) stop and escalate per §13 if following the higher source would be destructive, ambiguous, or materially change business outcomes. Editing a governance document to *dissolve* a conflict mid-task is forbidden.

**Stale documentation:** if repository documentation contradicts observable reality (code, running systems), reality wins for the task at hand, and the agent MUST record the discrepancy (ticket Work Log + an entry in `PROJECT.md` §7 or a backlog ticket to fix the doc). Do not blindly follow documents you can see are wrong; do not silently "fix" them either.

## 4. Work item lifecycle and state model

Tickets live as files in [`product/tickets/`](../product/BACKLOG.md), indexed by [`product/BACKLOG.md`](../product/BACKLOG.md). The ticket file's `status` field is authoritative for ticket state; `CURRENT_SPRINT.md` is authoritative for what is committed to the sprint.

### States

| State | Meaning |
| --- | --- |
| `backlog` | Captured and prioritized, not yet Ready |
| `ready` | Meets the Definition of Ready |
| `committed` | Selected into the current sprint |
| `in-progress` | Claimed by an owner; work underway |
| `blocked` | Cannot proceed; blocker recorded |
| `in-acceptance` | Implementation complete and verified by the engineer; awaiting independent acceptance |
| `done` | Meets the Definition of Done; verified |
| `dropped` | Deliberately abandoned; reason recorded |

Refinement is an *activity* performed on `backlog` tickets, not a state. Ideas are not tickets; they live in [`product/IDEAS.md`](../product/IDEAS.md) until promoted.

### Legal transitions

```text
backlog ──refine-ticket──▶ ready ──plan-sprint──▶ committed ──pick-up-ticket──▶ in-progress
in-progress ──engineer verification passed──▶ in-acceptance ──complete-ticket──▶ done
in-progress ◀──acceptance failed (defects recorded)── in-acceptance
in-progress ◀──▶ blocked            (blocker recorded / resolved)
committed | in-progress ──returned to backlog (Work Log preserved)──▶ ready
ready ──invalidated by new information──▶ backlog
backlog | ready ──deliberate decision, reason recorded──▶ dropped
```

Agents MUST NOT skip states. In particular: work MUST NOT move to `done` without passing through independent acceptance, and MUST NOT enter `in-progress` without being `committed` (see urgent work, §7).

## 5. Backlog management

- The **Product Owner persona owns backlog order**. Anyone may propose; only the PO persona reorders, and material reorders get a one-line reason in the backlog changelog.
- Every ticket follows [`templates/TICKET_TEMPLATE.md`](../templates/TICKET_TEMPLATE.md) and has a stable ID (`T-NNNN`, never reused).
- The backlog is not a dumping ground: raw thoughts go to `IDEAS.md`; tickets are created only for work someone can articulate an outcome and value for. Tickets untouched and unvalued for a long period SHOULD be `dropped`, not hoarded.

## 6. Refinement and sprint planning

- Refinement ([`refine-ticket`](../skills/refine-ticket/SKILL.md)) reviews a ticket from Product, Engineering, QA, Architecture, and — where applicable — UX and Security perspectives, and evaluates it against the [Definition of Ready](DEFINITION_OF_READY.md). A ticket MUST NOT be marked `ready` merely to keep work moving; "not ready, because X" is a successful refinement outcome.
- Sprint planning ([`plan-sprint`](../skills/plan-sprint/SKILL.md)) sets a sprint goal and selects `ready` tickets by priority, dependency order, risk, and capacity. `CURRENT_SPRINT.md` is the authoritative commitment list.
- Oversized work MUST be split before entering a sprint, along outcome seams (thin end-to-end slices), not technical layers.

## 7. Sprint execution

- **Claiming (mandatory):** before starting work an agent MUST run [`pick-up-ticket`](../skills/pick-up-ticket/SKILL.md): re-read the fresh sprint and ticket state, verify the ticket is `committed` with no owner and no unmet dependencies, then set `owner` and `status: in-progress` in the ticket file and mirror it in the sprint table in a single commit made directly on the trunk and pushed immediately (see [`standards/GIT.md`](../standards/GIT.md)). If the commit conflicts, someone else claimed it — pull and select another ticket. One owner per ticket at a time.
- **Stale claims:** a claimed ticket whose owner has produced no related commit for 24 hours `[default]` may be released by any agent acting as SM: verify the inactivity in git history, set `status:` back to `committed` (or `blocked`, if an unresolved blocker is recorded), clear `owner`, add a Work Log entry stating the release and evidence, and commit `os: T-NNNN stale claim released`. Takeover then goes through `pick-up-ticket`, resuming from the existing Work Log. Never release a claim that shows recent activity to free up a ticket you want.
- **Unplanned/urgent work MUST NOT be started silently.** To add work mid-sprint: create a ticket, record it under **Discovered / Unplanned Work** in `CURRENT_SPRINT.md` with a justification and its impact on the sprint goal, add it to the Committed Work table like any other committed ticket, then proceed. Urgent production bugs may use the DoR exceptions ([DoR §Exceptions](DEFINITION_OF_READY.md)). If the addition materially endangers the sprint goal, escalate to the human Product Owner first.
- **Blockers:** when blocked, set `status: blocked`, record the blocker (what, why, what would unblock, who can unblock) in the ticket and the sprint's Blockers section, then either pick other work or escalate. Never idle silently; never "work around" a blocker by cutting scope without recording it.
- **Discovered work** (necessary work found during implementation that is outside the ticket's scope): create a ticket for it and link it. Fold it into the current ticket only when it is genuinely inseparable from the acceptance criteria, and say so in the Work Log. Silently expanding scope is forbidden.
- **Scope changes** to a committed ticket (acceptance criteria added/removed/weakened) are Product Owner decisions, recorded in the ticket with reason and date. Implementers MUST NOT change acceptance criteria.

## 8. Implementation

- Source changes travel on a ticket branch and reach the trunk only through a reviewed merge; delivery-process state commits directly to the trunk — both lanes per [`standards/GIT.md`](../standards/GIT.md).
- Follow the ticket's scope, accepted ADRs, and [`standards/ENGINEERING.md`](../standards/ENGINEERING.md). Stay inside **In Scope**; deliver everything in **Acceptance Criteria**; touch nothing in **Out of Scope**.
- Keep the ticket's **Work Log** current: implementation plan, significant decisions, progress, remaining work, test results. The test is: *could a different agent resume this ticket right now from repository state alone?* If not, the log is behind — update it before ending any session.
- If implementation surfaces a genuinely ambiguous requirement or a decision with materially different business/architectural consequences, STOP feature work on that question and follow §12 (ADR) or §13 (escalation). Inventing requirements is forbidden.
- Refactoring that supports the ticket is part of the ticket. Larger refactoring gets its own `technical` ticket.

## 9. Testing and acceptance

- Engineers write automated tests with the implementation per [`standards/TESTING.md`](../standards/TESTING.md). The full relevant test suite MUST pass before a ticket moves to `in-acceptance`; "mostly passing" is failing.
- Acceptance ([`acceptance-test`](../skills/acceptance-test/SKILL.md)) is performed under the QA persona by someone/something other than the implementer's session, verifying acceptance criteria **independently of the implementer's claims** — by running tests, exercising the software, and inspecting behavior, not by reading the Work Log and agreeing with it.
- Acceptance criteria MUST NOT be rewritten after implementation to make the implementation pass. If a criterion is discovered to be wrong, that is a Product Owner scope decision (§7), recorded with a reason — and it is expected to be rare.
- A failed acceptance sends the ticket back to `in-progress` with each failure documented: expected vs. observed, and whether it is an implementation defect or a requirement ambiguity.

## 10. Code review

- Every change SHOULD be reviewed before merge via the [`review-code`](../skills/review-code/SKILL.md) skill, under the Software Engineer persona (plus Architect for structural changes) and by a session other than the implementer's — self-review does not count. Review checks: correctness, tests, adherence to standards and ADRs, scope discipline.
- Review findings that are out of the ticket's scope become tickets, not scope creep.

## 11. Bug handling

- Every confirmed defect gets a `bug` ticket with reproduction steps, expected vs. observed behavior, and severity. Fixes include a regression test.
- Defects found during acceptance of a ticket belong to that ticket (it isn't Done). Defects found later are new bug tickets — do not reopen `done` tickets.

## 12. Architectural decisions

Decisions that materially affect architecture, system boundaries, data models, major dependencies, infrastructure, public APIs, security architecture, or cross-cutting conventions MUST be recorded as ADRs via [`create-adr`](../skills/create-adr/SKILL.md) — *before or during* the work, never reconstructed from memory later. Routine implementation details do not get ADRs. Architecture invented during implementation without documentation is a defect.

## 13. Human escalation

Agents MUST stop autonomous execution and request a human decision when:

- product behavior is genuinely ambiguous after reading all relevant artifacts;
- two valid requirements or governance sources conflict irreconcilably;
- a destructive or hard-to-reverse operation is required (data deletion, force-push, production changes, spending);
- an architectural choice has materially different business consequences;
- security or privacy implications are unclear;
- acceptance criteria contradict this document or `PROJECT.md` constraints;
- the sprint commitment must materially change;
- external credentials, permissions, or paid services are required.

An escalation MUST state: (1) the issue, (2) why the agent cannot safely decide, (3) viable options, (4) tradeoffs, (5) a recommended default where one exists. Record it in the ticket's Work Log and the sprint's Blockers. Do NOT escalate routine implementation choices an experienced engineer would make — that is what agent judgment (precedence level 9) is for.

**Answers:** an escalation is resolved only when the decision is recorded in the repository — the human writes it into the ticket's Work Log (or the blocker entry), or the agent receiving the answer transcribes it verbatim, attributed and dated, before acting on it. An answer that exists only in chat unblocks nothing; the blocker entry is cleared only after the recorded answer exists.

## 14. Documentation

Documentation follows [`standards/DOCUMENTATION.md`](../standards/DOCUMENTATION.md). The rule of thumb: document decisions and interfaces, not narration. `done` includes documentation (see [DoD](DEFINITION_OF_DONE.md)).

## 15. Continuous improvement and changing this document

- Retrospectives ([`retrospective`](../skills/retrospective/SKILL.md)) run at sprint end and produce concrete, owned improvement actions — not sentiments.
- Governance documents (this file, DoR, DoD, standards, templates, skills) change only via [`evolve-governance`](../skills/evolve-governance/SKILL.md): explicit proposal with reason, expected improvement, affected artifacts, and migration notes; approved by a human or by the persona that owns the document (see PERSONAS.md); recorded in a retrospective or decision log. **Governance MUST NOT be changed to make a specific failing or incomplete ticket pass** — that is the canonical forbidden move.
- Any agent MAY propose an improvement at any time by recording it in the current sprint's Notes or the retrospective input; proposing is encouraged, silently rewriting is forbidden.

## 16. Anti-patterns (explicitly forbidden or flagged)

Process theater; giant tickets; vague acceptance criteria ("works correctly"); criteria rewritten after implementation; undocumented architecture decisions; marking work Done with failing tests or known unrecorded defects; silent scope expansion; unrecorded sprint additions; two agents on one ticket; decisions that exist only in chat; speculative abstractions ("we might need it"); ADRs for trivia; backlog as a dumping ground; blindly following documentation that contradicts reality.
