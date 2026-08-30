# Documentation Standards

What gets documented, where, and to what bar. The principle: **document decisions, interfaces, and operations — not narration.** A document nobody will consult is ceremony; delete it or don't write it.

## Where things go (single home per fact)

| Information | Home |
| --- | --- |
| What the project is, constraints, stack | [`PROJECT.md`](../PROJECT.md) |
| Why the system is shaped this way | [`architecture/adr/`](../architecture/adr/README.md) |
| Current system map | [`architecture/ARCHITECTURE.md`](../architecture/ARCHITECTURE.md) |
| How we work | [`governance/`](../governance/WAY_OF_WORKING.md) |
| Why a change was made, decisions during work | the ticket's Work Log |
| How to use / operate the software | the product codebase's own README / docs (see below) |
| Domain vocabulary | [`governance/GLOSSARY.md`](../governance/GLOSSARY.md) |

Duplicate a fact into a second home only with a link back to the authoritative one.

## Universal rules

- **Interfaces are documented where they are consumed:** public APIs, module boundaries, configuration options, and CLI surfaces get reference documentation kept next to the code and updated in the same change.
- **Setup must work from scratch:** the codebase README lets a new contributor (human or agent) build, run, and test the project by following it literally. If a step drifts, fixing the README is part of the ticket that broke it.
- **Code comments explain constraints and non-obvious *why*,** never restate the code. Commented-out code is deleted.
- **Tickets are documentation:** a ticket's Work Log records significant decisions, alternatives rejected in passing, and test evidence — precise enough for a stranger to resume the work.
- Markdown for prose; diagrams as Mermaid in Markdown (diffable) rather than binary images where possible; relative links between repository artifacts.
- Stale documentation is a defect: file a ticket when found; fix in place when the fix is within your current ticket's scope.

## Project-specific rules

> ⚠ **Replace during `bootstrap-project`.**

- **User-facing documentation:** *TBD — what exists (user guide? API portal?), where, and who updates it.* `[open]`
- **Operational runbooks:** *TBD — required for deployables; location and format.* `[open]`
- **Doc review expectations:** documentation changes reviewed like code `[default]` — confirm or replace.
