# ADR-0008: Role restrictions are enforced by policy attributes and declared in the contract as a description and a 403

## Status

Accepted

Raised as a blocking review finding on [T-0004](../../product/tickets/T-0004-create-and-list-projects.md) by `claude-rev-3e77`, 2026-08-31: the reasoning was sound and the *location* was wrong — a rule binding every future endpoint was recorded only in one controller's XML documentation, invisible from the ticket that writes the next one.

## Date

2026-08-31

## Context

Authorisation in this system is by **global role** — `admin` or `member` — carried as a `role`
claim in the token ([ADR-0003](ADR-0003-initial-technology-stack.md), `PROJECT.md` §5).
[T-0009](../../product/tickets/T-0009-role-authorisation-and-user-projection.md) established
the two policies and the rule that `[Authorize(Roles = …)]` and `RequireRole` must never be
used instead, because their exact-match semantics refuse an admin where the policy grants one.

[ADR-0004](ADR-0004-contract-first-openapi-code-generation.md) requires the API surface to be
designed in `spec/openapi.yaml` and generated: a controller declaring its own routes is a
review rejection. T-0004 was the first ticket to need an endpoint restricted to one role, and
it exposed a genuine gap between those two decisions.

**OpenAPI cannot express this restriction.** Its security model is schemes and scopes. A role
claim is neither. The document declares `security: bearerAuth` globally, and the generator
emits a bare `[Authorize]` — correct, and silent about which roles may call what. The
alternatives are all worse than they look:

- **Declare a fake OAuth scope per role.** The contract would then describe an authorisation
  mechanism the system does not implement, and a generated client would request scopes the
  identity host does not issue. A contract that lies about its mechanism is worse than one that
  is silent about it.
- **Enforce nothing and rely on documentation.** Rejected on sight: the restriction is a
  security control.
- **Push the check into the controller body**, returning 403 by hand. This works and is
  invisible to the framework's authorisation pipeline, to `[Authorize]` metadata, and to anyone
  auditing which endpoints are protected. It also re-derives what a role means at each call
  site, which is exactly what T-0009's policies exist to prevent.

## Decision

**Role restrictions are enforced by `[Authorize(Policy = …)]` attributes on the concrete
controller, and declared in the contract as an operation description plus a declared `403`
response.**

Both halves are required. Neither alone is sufficient:

- **The attribute is the enforcement.** It uses the policy constants from
  `AuthorizationPolicies`, never the framework's role syntax, and it sits on the concrete
  controller because the generated contract is regenerated and never hand-edited.
- **The contract is the declaration.** Every operation whose access depends on a role says so
  in its `description`, and declares `403` among its responses. A client generating from the
  document therefore knows the endpoint can refuse an authenticated caller, and what that means.

**An endpoint restricted in code and silent in the contract is a defect**, not a style
preference. It is the same failure as a response that declares `application/problem+json` and
returns something else: the document promising something other than what the system does.

This does not weaken [ADR-0004](ADR-0004-contract-first-openapi-code-generation.md). Applying a
policy is not declaring a route: no routing, binding, or status-code declaration moves into
hand-written code. The rule that a controller declaring its own routes is a review rejection
stands unchanged.

## Options Considered

**1. Attribute plus contract declaration — chosen.** The only option where enforcement is
framework-visible *and* the contract stops lying by omission. Costs: the restriction lives in
two places, so they can drift, and nothing mechanical checks that they agree.

**2. Fake OAuth scopes in the specification.** Rejected: it would make the document describe a
mechanism that does not exist, and generated clients would request scopes nothing issues. This
is worse than silence because it is confidently wrong.

**3. Imperative checks in the controller body.** Rejected: invisible to the authorisation
pipeline and to any audit of protected endpoints, and it re-derives role meaning per call site
— the specific failure T-0009's policies were built to end.

**4. Wait for the generator to support it.** Rejected as indefinite. OpenAPI has no concept of
a role claim; this is not a tooling gap that closes.

## Consequences

### Positive

- The restriction is enforceable, framework-visible, and auditable: every protected endpoint
  carries a policy attribute that authorisation middleware and tooling can see.
- Clients generating from the contract learn that an operation can return 403 and why.
- The precedent is now written down where [T-0005](../../product/tickets/T-0005-create-and-read-issues.md)
  and everything after it will find it, rather than in one controller's XML comment.

### Negative

- **The declaration and the enforcement can drift.** Nothing today fails when an operation
  gains a policy attribute and its description is not updated, or declares 403 and enforces
  nothing. That is a real gap and it is the reason for the follow-up below.
- Two files must change for one decision, which is a small tax on every restricted endpoint.

## Risks

- **Drift is silent and this project's recurring defect is exactly that** — a document claiming
  more than the code does ([RETRO-SPRINT-001](../../delivery/retrospectives/RETRO-SPRINT-001.md),
  [RETRO-SPRINT-002](../../delivery/retrospectives/RETRO-SPRINT-002.md)). A conformance tier
  that compared declared 403s against policy attributes would close it mechanically.
- A future endpoint could be restricted by a policy nobody declares in the contract, and every
  test would still pass, because the tests exercise the policy rather than the document.

## Follow-up Actions

- [T-0017](../../product/tickets/T-0017-automated-contract-conformance-tier.md) is the natural
  home for a check that declared 403s and policy attributes agree. Recorded in its Work Log.

## Related ADRs

- [ADR-0004](ADR-0004-contract-first-openapi-code-generation.md) — contract-first generation, which this decision works within rather than around
- [ADR-0005](ADR-0005-operational-endpoints-outside-the-api-contract.md) — the structurally identical precedent: something real that the contract deliberately does not carry, decided in an ADR rather than in a comment
- [ADR-0003](ADR-0003-initial-technology-stack.md) — global roles as a token claim

## Related Tickets

- [T-0004](../../product/tickets/T-0004-create-and-list-projects.md) — where the gap surfaced; first endpoint restricted to a role
- [T-0009](../../product/tickets/T-0009-role-authorisation-and-user-projection.md) — the policies this applies
- [T-0017](../../product/tickets/T-0017-automated-contract-conformance-tier.md) — where the drift risk could be closed
