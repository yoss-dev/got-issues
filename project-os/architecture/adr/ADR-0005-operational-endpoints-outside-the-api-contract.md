# ADR-0005: Operational endpoints are outside the API contract

## Status

Accepted

## Date

2026-08-30

## Context

[ADR-0004](ADR-0004-contract-first-openapi-code-generation.md) and [`standards/ENGINEERING.md`](../../standards/ENGINEERING.md) state the project's central rule without qualification: *the OpenAPI specification is the only place the API surface is designed; no endpoint, field, status code, or error shape exists unless the spec says so.*

Refining [T-0001](../../product/tickets/T-0001-runnable-compose-stack.md) hit that rule head-on. The Compose stack needs a health endpoint for container health checks, and the ticket proves token validation against a protected endpoint — but `spec/openapi.yaml` and the generation pipeline arrive in [T-0002](../../product/tickets/T-0002-contract-first-codegen-pipeline.md), which depends on T-0001. The dependency runs the wrong way to resolve by reordering: read literally, T-0001 cannot be implemented without violating a standard.

The underlying question is what the contract-first rule is *for*. It exists so that clients generating from the specification get the truth, and so that the API's design happens deliberately in one reviewable place. A health endpoint serves neither audience: Docker and operators consume it, no client generates against it, and it carries no domain semantics.

## Decision

**Operational endpoints are outside the API contract.** Health, readiness, and metrics endpoints are infrastructure, not product API surface: they are implemented directly, are not declared in `spec/openapi.yaml`, and are not generated.

Everything else remains bound by the contract-first rule. The test is the audience: **if a client would generate code against it, it belongs in the specification.** An endpoint returning domain data is never operational, however convenient the label would be.

Operational endpoints are documented in the repository README rather than the specification, since the specification is the product's user-facing documentation ([DOCUMENTATION.md](../../standards/DOCUMENTATION.md)) and operators are a different audience.

## Options Considered

1. **Exempt operational endpoints (chosen)** — keeps the contract-first rule aimed at the product API, where it earns its value, and unblocks T-0001 without pulling the pipeline forward. Chosen by the maintainer, 2026-08-30.
2. **No exception — declare health in the specification** — purest reading of ADR-0004, and it keeps a single rule with no boundary to police. Rejected: it forces T-0001 to create a minimal specification and run the generator purely to declare `/health`, effectively merging part of T-0002 into a ticket already too large, and it puts operational concerns into the document clients generate from.
3. **T-0001 ships no HTTP endpoints at all** — defer every endpoint until the specification exists. Rejected: Compose health checks would have nothing to probe, "is the stack healthy?" would have no answer, and the ticket's central acceptance criterion becomes unverifiable. It defers the question rather than answering it.

## Consequences

### Positive

- T-0001 is implementable as written, and the circular dependency with T-0002 disappears.
- The contract-first rule stays absolute where it matters — every endpoint a client touches is still spec-first, and ADR-0004's guarantee is undiminished.
- Container health checks, and later any metrics scraping, work the way the surrounding ecosystem expects rather than through a generated contract that fits them badly.

### Negative

- **The project now has a boundary to police**, and boundaries get pushed. "It's operational" is an available excuse for skipping the spec, and the rule's value depends on reviewers refusing it. The audience test above is the defence, but it is judgment, not mechanism.
- The drift check cannot protect operational endpoints: they can change silently, since nothing generates from them.
- The API's full HTTP surface is no longer described in one document — a reader must consult both the specification and the README.
- A future decision to expose richer operational data (per-dependency health, build metadata) will re-raise this boundary rather than settle it.

## Risks

- **Scope creep through the exemption.** An endpoint gets labelled operational to avoid a spec change, and the contract-first guarantee erodes one convenience at a time. Noticed in review, if reviewers apply the audience test; invisible if they do not. This is the main reason the exemption is written down rather than left as folklore.
- If operational endpoints ever need authorisation semantics beyond "authenticated", the absence of a declared contract will be felt.

## Follow-up Actions

- T-0001 keeps its health-endpoint criterion and proves token validation against a protected operational endpoint.
- If the exemption is observed being stretched, revisit with a superseding ADR rather than tightening informally.

## Related ADRs

- Scopes [ADR-0004](ADR-0004-contract-first-openapi-code-generation.md) — this ADR narrows the surface that ADR-0004's rule governs; it does not supersede it.
- Depends on [ADR-0003](ADR-0003-initial-technology-stack.md) — the stack whose health checks raised the question.

## Related Tickets

- [T-0001](../../product/tickets/T-0001-runnable-compose-stack.md) — surfaced the conflict during refinement
- [T-0002](../../product/tickets/T-0002-contract-first-codegen-pipeline.md) — owns the contract this ADR scopes
