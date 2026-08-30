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

Set at bootstrap 2026-08-30.

- **User-facing documentation is the OpenAPI specification** `[confirmed]`. `spec/openapi.yaml` is authored first and is the contract, so it is also the reference documentation: every operation, schema, error, and scope carries a `summary`/`description` written for a reader who has never seen the code. Documentation quality in the spec is reviewed like any other contract change — an endpoint with no description is incomplete, not merely undocumented. There is no separate API guide to drift out of date, and no UI to document ([`PROJECT.md`](../PROJECT.md) §3).
- **The root README must work from a clean clone** `[confirmed]`: prerequisites (Docker, .NET SDK, JDK for code generation), `docker compose up`, how to obtain a token, how to run the tests, and how to regenerate from the spec. A ticket that changes any of those steps fixes the README in the same change.
- **Operational runbooks:** none required yet `[confirmed]` — nothing is deployed; Compose is the only way the system runs. A runbook becomes mandatory with the first deployment target (`PROJECT.md` Q6 territory).
- **Generated code carries no hand-written documentation** `[confirmed]`: comments in generated output would be overwritten on the next regeneration. Document the *behaviour behind* the contract in the implementing code, and the contract itself in the spec.
- **Doc review expectations:** documentation changes are reviewed like code `[default]`.
- **Decisions live in the Work Log, rationale in ADRs** — see the routing table above. Chat is not a project record ([WoW](../governance/WAY_OF_WORKING.md)).
