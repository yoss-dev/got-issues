# ADR-0001: Record architecture decisions as ADRs in this repository

## Status

Accepted

## Date

2026-08-30

## Context

This project is delivered by a mix of humans and AI agents who do not share memory. Architectural decisions made in a conversation are lost the moment the session ends, which leads to re-litigated choices, contradictory implementations, and architecture that exists only implicitly in the code. The framework requires an auditable trail explaining why the system is shaped the way it is.

## Decision

All architectural decisions meeting the significance bar defined in [adr/README.md](README.md) are recorded as sequentially numbered, immutable ADR files in `architecture/adr/`, created via the `create-adr` skill, and linked bidirectionally with the tickets that motivated them. Accepted ADRs sit at precedence level 3 in the [Way of Working](../../governance/WAY_OF_WORKING.md): below project constraints, above DoR/DoD and ticket content.

## Options Considered

1. **ADRs in-repo (chosen)** — decisions versioned with the work they govern; agents can load them as context.
2. **Decisions in an external tool (wiki, tracker)** — rejected: invisible to agents working from the repository, drifts from code, breaks the self-contained-repo principle.
3. **No formal records; rely on code and commit messages** — rejected: preserves *what* changed but not *why*, and not what was rejected.

## Consequences

### Positive

- Decisions survive agent sessions and personnel changes; conflicts between agents resolve by citing a document rather than opinion.
- "Why?" is answerable years later, including for rejected options.

### Negative

- Writing ADRs costs time; the significance bar must be policed so trivial ADRs don't bury important ones.

## Risks

If agents skip ADRs under delivery pressure, undocumented architecture accumulates. Mitigated by the DoD (conditional requirement) and the acceptance-test skill checking for it.

## Follow-up Actions

None — the `create-adr` skill and DoD hook already operationalize this decision.

## Related ADRs

None (first ADR).

## Related Tickets

None (framework bootstrap).
