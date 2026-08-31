---
id: T-0019
title: Decide the resource server's clock-skew allowance instead of inheriting five minutes
type: technical
status: backlog
priority: normal
owner: none
implemented_by: none
accepted_by: none
depends_on: [T-0009]
adrs: [ADR-0003]
created: 2026-08-31
updated: 2026-08-31
---

# T-0019: Decide the resource server's clock-skew allowance instead of inheriting five minutes

## Problem / Context

Found during [T-0015](T-0015-compose-stack-smoke-test.md) while building AC6's expired-token
case. The API configures `JwtBearer` without setting `TokenValidationParameters.ClockSkew`,
so it keeps the library default of **five minutes**: a token is accepted for five minutes
after the `exp` its issuer stamped on it.

Tokens live one hour (`exp - iat` = 3600 on a decoded member token), so the grace extends a
token's usable life by rather more than 8%. Nobody chose that. It is not recorded in
[ADR-0003](../../architecture/adr/ADR-0003-initial-technology-stack.md), in the ticket that
built the resource server ([T-0010](T-0010-duende-identity-host.md)), or in the one that
built authorisation ([T-0009](T-0009-role-authorisation-and-user-projection.md)) — the
number is simply the framework's.

The default is defensible: it exists because clocks drift between issuer and resource
server, and every service here runs in one Compose stack off one host clock, where the
justification is weakest. The point of this ticket is **not** that five minutes is wrong.
It is that the number is currently an accident, and it is the kind of accident that is
discovered during an incident.

It also has a testing cost, already paid once: T-0015's expired-token check cannot use a
freshly-expired token, because a token that expired seconds ago is still valid. That check
now mints a token an hour past expiry to clear the window — correct, but the window is why.

## Desired Outcome

The allowance is a recorded decision with a stated reason, and the resource server enforces
whatever was decided.

## User / Business Value

A revoked or expired token that keeps working for five minutes is a small security fact
nobody has agreed to. This is a PoC for an internal tool, so the risk is modest — which is
exactly why it is cheap to settle now rather than after the system holds real data.

## Scope

### In Scope

- A decision on the allowance, with its reason recorded where the next reader will find it.
- The resource server configured to match.
- A test proving the boundary the decision names.

### Out of Scope

- Token lifetime itself (an identity-host concern, not the resource server's).
- Revocation and introspection — a different mechanism for a different problem.

## Acceptance Criteria

- [ ] AC1: Given the resource server's configuration, when it is read, then the clock-skew allowance is set explicitly with a comment giving the reason, not left to the library default.
- [ ] AC2: Given a token expired by more than the allowance, when it is presented, then the request is refused with 401.
- [ ] AC3: Given a token expired by less than the allowance (if the decision keeps a non-zero one), when it is presented, then the outcome matches what the decision says it should be — proven, not assumed.
- [ ] AC4: Given the decision, when it is recorded, then it states why that number and not another; "the framework default" is not a reason.

## Examples / Scenarios

- Allowance zero: a token one second past `exp` is refused.
- Allowance thirty seconds: a token ten seconds past `exp` is accepted; one two minutes past is refused.

## Dependencies

None beyond the existing resource server.

## Risks / Unknowns

- **Zero skew is not obviously right.** If the identity host and the API ever run on
  different hosts with drifting clocks, zero produces intermittent 401s that are miserable
  to diagnose. The decision should say what it assumes about clocks.
- **Changing it changes T-0015's expired-token test**, which currently clears a five-minute
  window by an hour. It will still pass under a smaller allowance, but the reason its margin
  is so large should move with the decision.

## Testing Notes

The boundary belongs in the smoke tier: it needs a real issuer to construct the token, which
is why T-0015 owns the expired case today.

## Relevant ADRs & Documentation

- [T-0015](T-0015-compose-stack-smoke-test.md) — where this was found, and where the expired-token check lives
- [SECURITY.md](../../standards/SECURITY.md)

## Definition of Ready

- [ ] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — not yet refined.

## Definition of Done

- [ ] Meets [DoD](../../governance/DEFINITION_OF_DONE.md) — checked by `complete-ticket`; deviations require recorded PO/human approval.

---

## Work Log

### 2026-08-31 — Product Owner (claude-sm-9d4e)

- **Did:** Raised from T-0015 rather than fixed in passing: that ticket adds verification and does not change the resource server, and quietly altering token validation under a coverage ticket is how decisions stop being visible.
- **Decided:** framed as "decide the number", not "set it to zero" — the ticket should not pre-empt the judgement it exists to force.
- **Remaining:** refinement.
- **Open questions / blockers:** none.
- **Branch / PR:** n/a
- **Test state:** n/a — not started.
