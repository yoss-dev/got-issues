# Testing Standards

What "tested" means on this project. Applied by the Software Engineer persona during implementation and enforced independently by the QA persona during acceptance.

## Universal rules

### What must be tested

- **Every acceptance criterion maps to at least one automated test** where automation is feasible; where it is not (visual look-and-feel, exploratory concerns), the ticket's Testing Notes say how it is verified instead.
- **New logic gets unit tests**; behavior that spans components gets at least one integration-level test through the real seams.
- **Every fixed bug gets a regression test that fails without the fix.** No exceptions — this is how the same bug is prevented from returning.
- Edge cases surfaced during refinement (ticket *Examples / Scenarios*) are the test designer's primary checklist.

### How tests must behave

- Deterministic: a test that flakes is fixed or quarantined *with a ticket* the same day; an ignored flaky test without a ticket is a DoD violation.
- Independent: tests do not depend on execution order or leftover state.
- Fast enough to run habitually; slow suites are split so the habitual tier stays fast.
- Tests assert *behavior/outcomes*, not implementation details; a refactor that preserves behavior should not break them.
- Test code is production code: same review, same standards, no copy-paste sprawl.

### The gate

- The full relevant suite passes before a ticket enters `in-acceptance`. "Passes on my machine" with red CI is red.
- Coverage numbers are a signal, not a goal; an untested critical path fails review regardless of the percentage.

## Project-specific rules

Set at bootstrap 2026-08-30. Frameworks are `[default]` (agent-proposed, nobody objected); the *policies* below follow from confirmed constraints.

### Test frameworks & runners

- **xUnit** as the test framework and runner (`dotnet test`) `[default]`.
- **`WebApplicationFactory`** for API-level tests through the real ASP.NET Core pipeline `[default]`.
- **Testcontainers** for PostgreSQL: integration tests run against a real database in a container, never an in-memory provider `[default]`. The EF Core in-memory provider does not enforce constraints or translate real SQL — a test that passes on it proves little.

### Test tiers & where they live

| Tier | Location | Runs against |
| --- | --- | --- |
| Unit | `<project>.UnitTests` beside the project under test | Pure logic, no I/O |
| Integration / API | `<project>.IntegrationTests` | `WebApplicationFactory` + PostgreSQL in Testcontainers |
| Contract | with the integration tests | The OpenAPI spec: responses validated against the declared schemas |

- **Generated code is not unit-tested** — testing a generator's output tests the generator. Test the behaviour *behind* the generated contract `[default]`.
- **Every endpoint in the specification has at least one integration test** against real PostgreSQL. This is a stated success criterion ([`PROJECT.md`](../PROJECT.md) §3) `[default]`.

### How to run the suite

Agents MUST run these before claiming green — "passes on my machine" with anything red is red:

```bash
dotnet build                          # must be warning-clean
dotnet test                           # unit + integration (Docker must be running)
./tools/generate.sh && git diff --exit-code   # spec/codegen drift check — must be empty
```

The drift check is part of the suite, not an extra: a non-empty diff means the committed code no longer matches the contract ([ADR-0004](../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md)) `[confirmed]`.

*(Exact script paths are established by the first implementation ticket; until then, run the equivalent commands and correct this section in that ticket.)*

### Coverage expectations

No numeric target `[default]`. The bar is behavioural: every acceptance criterion maps to a test, every endpoint has an integration test, every fixed bug has a regression test that fails without the fix.

### Test data & fixtures policy

- No production data in tests; no real personal data in fixtures `[default]`.
- Each integration test owns its data and cleans up or runs against a fresh container — tests never depend on another test's leftovers `[default]`.
- Auth in tests: obtain real tokens from the identity host where the test is about authorisation; otherwise use a test authentication handler. Never disable authentication globally to make a test pass `[default]`.
