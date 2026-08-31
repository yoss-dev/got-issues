# Definition of Done (DoD)

**Question answered:** what must be true before a work item can genuinely be considered complete?

Done is verified by [`complete-ticket`](../skills/complete-ticket/SKILL.md) after independent acceptance ([`acceptance-test`](../skills/acceptance-test/SKILL.md)). The implementer's word is input, not evidence. Modifying this document to let a specific incomplete ticket pass is the canonical governance violation ([WoW §15](WAY_OF_WORKING.md)).

## Universal requirements (every ticket except spikes — see below)

1. **Implementation complete** — everything In Scope is built; nothing Out of Scope was smuggled in.
2. **All acceptance criteria verified** — independently, against the running software or executed tests, with results recorded in the ticket.
3. **Automated tests exist and pass** — new behavior is covered per [`standards/TESTING.md`](../standards/TESTING.md); the full relevant suite passes. A skipped or flaky-ignored test is a failing test unless a ticket tracks it and the ticket says so.
4. **No known unrecorded defects** — every defect found is either fixed or captured as a bug ticket linked from this one, with the PO persona accepting the deferral. **A deferral is captured only when the destination ticket's scope actually accepts it:** whoever defers reads the destination and cites the scope line or acceptance criterion that takes it on, adding one if none exists. A link to a ticket that does not cover the item is worse than no link, because it reads as covered — see [RETRO-SPRINT-001](../delivery/retrospectives/RETRO-SPRINT-001.md), where a residual was pointed at a ticket whose Out of Scope explicitly disowned it.
5. **Code quality** — change reviewed; linting/static analysis clean per [`standards/ENGINEERING.md`](../standards/ENGINEERING.md); no leftover debug scaffolding, dead code, or TODOs without ticket references.
6. **Documentation updated** — per [`standards/DOCUMENTATION.md`](../standards/DOCUMENTATION.md): user-facing docs, interface docs, and README/setup instructions affected by the change.
7. **Work Log complete** — decisions, test results, and any deferred items are recorded in the ticket; repository state alone tells the full story.
8. **State updated** — ticket `status: done`, sprint table updated, backlog index updated.

## Conditional requirements (when the change touches the area)

- **Regression tests** — a fixed bug has a test that fails without the fix.
- **ADR recorded** — any decision meeting the [ADR bar](../architecture/adr/README.md) has an Accepted ADR linked from the ticket.
- **Security** — new external input validated; no secrets in code or history; dependency changes checked per [`standards/SECURITY.md`](../standards/SECURITY.md).
- **Accessibility** — user-facing UI meets the accessibility requirements in the standards.
- **Observability** — new operationally significant behavior is logged/metered per project conventions.
- **Migrations** — data migrations are scripted, reversible or explicitly one-way (stated), and tested.
- **Deployment** — the change deploys cleanly through the project's pipeline; feature flags/config documented; rollback path known.

## Spikes

A spike is Done when: the question is answered (or the time box expired with findings), the findings are written in the ticket, and follow-up tickets/ADRs are created and linked. Code produced by a spike is disposable by default and MUST NOT ship as production code without going through a normal ticket.
