---
id: T-0017
title: Automate the contract-conformance test tier TESTING.md already defines
type: technical
status: backlog
priority: high
owner: none
implemented_by: none
accepted_by: none
depends_on: [T-0002]
adrs: [ADR-0004]
created: 2026-08-31
updated: 2026-08-31
---

# T-0017: Automate the contract-conformance test tier `TESTING.md` already defines

## Problem / Context

Raised by `claude-qa-5a71` during [T-0002](T-0002-contract-first-codegen-pipeline.md)'s acceptance as their strongest recommendation.

[`TESTING.md`](../../standards/TESTING.md) defines three test tiers, and the third reads: *"**Contract** — with the integration tests — The OpenAPI spec: responses validated against the declared schemas."* **Nothing performs it.** The tier exists in the standard and not in the repository.

The cost is measurable rather than theoretical. T-0002 shipped with **six contract defects**, every one of them the document disagreeing with the system:

| # | Defect | Found by |
| --- | --- | --- |
| 1 | Page sizes: prose said capped, schema said maximum | a test failing |
| 2 | `GET` declared 200/401 while returning 400 — and a test asserted that undeclared 400 | review, by hand |
| 3 | `page` declared `minimum: 1` enforced only by a `Math.Max` in code | review, by hand |
| 4 | `label` declared non-nullable while returning `null` | review, by hand |
| 5 | `401` declared a problem document, returned an empty body | review, by hand |
| 6 | The spec claimed generated clients enforce bounds; they do not | acceptance, by hand |

**Four of the six** — 2, 3, 4 and 5 — are response-versus-schema mismatches that a conformance tier would have caught **mechanically, on the first run**. They instead took three review rounds and an acceptance pass, and defect 5 survived all of them because the tests asserted status codes without asserting bodies.

The acceptor performed the tier by hand for one response and found it clean. Doing that by hand for every response, every time, is not a plan.

## Desired Outcome

Every response the API returns in the test suite is validated against the schema `spec/openapi.yaml` declares for it, automatically, so a divergence fails a test rather than waiting for someone to notice.

## User / Business Value

This is the mechanism that makes contract-first *self-enforcing* rather than a discipline. [ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md)'s whole premise is that the specification is the truth; the drift check protects spec-versus-generated-code, and nothing protects spec-versus-actual-behaviour. This closes that half.

For Priya the integrator — the persona whose existence justifies the API-first design — it is the difference between a published contract that is accurate and one that is merely intended.

## Scope

### In Scope

- Validate responses against the declared schema for the operation and status code: required properties present, types correct, **no undeclared properties**, declared nullability honoured.
- Cover the failure shapes too — 400 and 401 must match `Problem`, which is where defect 5 hid.
- Assert the *declared status codes*: a response the specification does not declare is a failure, which is defect 2.
- Wire it into the existing integration tier so it runs with `dotnet test` and needs no separate command.
- Prove it by mutation, per [TESTING.md](../../standards/TESTING.md): reintroduce each of defects 2–5 in turn and confirm the tier catches it.

### Out of Scope

- Request validation — the generated contract already enforces that server-side.
- Client-side conformance (defect 6's territory); the specification now states plainly that enforcement is the server's.
- Replacing the drift check, which protects a different property.
- Publishing or hosting the specification.

## Acceptance Criteria

- [ ] AC1: Given a response from any endpoint exercised by the integration suite, when it is checked, then it is validated against the schema declared for that operation and status code.
- [ ] AC2: Given a response containing a property the schema does not declare, when the suite runs, then it fails.
- [ ] AC3: Given a response missing a property the schema marks required, when the suite runs, then it fails.
- [ ] AC4: Given a response whose status code the specification does not declare for that operation, when the suite runs, then it fails.
- [ ] AC5: Given a `400` or `401` response, when it is checked, then it is validated against `Problem` like any other — the failure shapes are covered, not just the success ones.
- [ ] AC6: Given each of T-0002's defects 2, 3, 4 and 5 reintroduced one at a time, when the suite runs, then it fails on each — **demonstrated by mutation, not asserted**.
- [ ] AC7: Given the habitual `dotnet test`, when it runs, then the conformance checks run with it and the suite stays fast enough to run habitually.

## Examples / Scenarios

- Add an undeclared property to a response model: red.
- Change `label` back to non-nullable in the spec while the API returns null: red.
- Make `/placeholders` return an undeclared 418: red.
- Strip the problem body from a 401: red — this is defect 5, which three passes of status-code assertions missed.

## Dependencies

**T-0002** — the specification and the generated contracts.

Related: [T-0016](T-0016-generation-output-ownership.md) protects spec-versus-generated-code; this protects spec-versus-behaviour. Neither substitutes for the other.

## Risks / Unknowns

- **Choosing a validator matters more than it looks.** A library that ignores `additionalProperties` or OpenAPI 3.1 union types (`[string, 'null']`) would pass defects 4 and would have passed defect 4 as originally written. Refinement should check the candidate against T-0002's actual defects before adopting it.
- OpenAPI 3.1 support is uneven across .NET tooling — the generator itself warns its 3.1 support is beta.
- If conformance runs on every response it may slow the integration tier; measure before optimising.
- A tier that only ever passes proves nothing, which is why AC6 requires the four historical defects be reintroduced.

## Testing Notes

AC6 is the criterion that makes this ticket honest. Four real defects exist in this repository's history with known reproductions; the tier is credible exactly to the degree it catches them.

## Relevant ADRs & Documentation

- [ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md) — the contract-first premise this enforces
- [TESTING.md](../../standards/TESTING.md) — where the Contract tier is defined but unimplemented
- [T-0002](T-0002-contract-first-codegen-pipeline.md) — the six defects, with reproductions

## Definition of Ready

- [ ] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — not yet refined.

## Definition of Done

- [ ] Meets [DoD](../../governance/DEFINITION_OF_DONE.md) — checked by `complete-ticket`.

---

## Work Log

### 2026-08-31 — Software Engineer (claude-sm-9d4e)

- **Did:** Created from T-0002's acceptance. The acceptor performed the Contract tier by hand, found it clean for one response, and observed that the tier `TESTING.md` defines has never been implemented.
- **Decided:** priority `high`, unusually for a testing ticket. The evidence is that four of T-0002's six defects were mechanically detectable and instead consumed three review rounds and an acceptance pass — and one of them survived all four because assertions checked status codes and not bodies. Every product ticket from T-0004 onward adds endpoints to a contract nothing verifies behaviourally.
- **Remaining:** Refinement. The validator choice is the substantive decision and should be tested against T-0002's real defects rather than chosen on reputation.
- **Open questions / blockers:** none.
- **Branch / PR:** n/a
- **Test state:** n/a — not started.
