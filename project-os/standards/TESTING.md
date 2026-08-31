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

### Claims about coverage must be falsifiable

**A test is not shown to guard a behaviour until it has been seen to fail when that behaviour breaks.** That principle stands. What follows is when the repository requires you to *demonstrate* it, because demonstrating it everywhere proved to cost more than it returned.

**Mutation is required when the test is the only thing standing between a claim and nothing:**

- the claim is one another ticket's Definition of Done depends on;
- or a reviewer or acceptor challenges a coverage claim — then the answer is a mutant, not an argument.

**Mutation is not required — and should not be performed for its own sake — when:**

- the property is enforced by the compiler, an analyser, a database constraint or a framework invariant. **Record the enforcement instead**: it is a stronger guarantee than a test, and a mutant the build rejects is evidence about the build (see below).
- the claim is already evidenced by a test that has been observed failing during development. Say so; a green test you watched go red is the same evidence.

**Proportionality, once you are mutating:**

- **One mutant per claim, not per assertion.** A claim with five assertions needs one mutant that reaches them, not five.
- **Do not re-mutate an unchanged claim.** Re-run only when the code under it changed shape, and say which change made the old evidence stale.
- **Use the cheapest tier that can host the mutant.** A mutant the integration tier can see must not be run against the smoke tier.

#### A mutant only counts if it reaches the assertion

The build accepting it is necessary and not sufficient. A mutant can be stopped by the compiler, by an analyser, by a test fixture's own guard, or by an unrelated failure — and a **red suite is not proof the mutant caused it**. Before believing a red or a green, confirm the thing you changed is the thing that caused it.

SPRINT-003 produced four invalid mutants inside two tickets: one rejected by the compiler (CS0534), one stopped by EF's `PendingModelChangesWarning` before a single assertion ran, one whose failure was an unrelated 401, and one that silently did nothing because it was registered before the call it meant to override. Two came from reviewers rather than implementers. Each expectation was reasonable, which is what made the check feel unnecessary.

A mutant the build rejects is **mis-filed, not worthless**: record it as a compiler- or constraint-enforced invariant, then run one the build accepts.

#### The mutation record states what the mutant proves

A mutant killed by both the old and the new code shows the new code works, not that it is stronger; showing strength needs a mutant the old code survives. Under this standard the record *is* the evidence, so a record that overstates its mutant is the same defect as an assertion that overstates its subject.

#### Why the mandate is narrow

Both parts of this section are empirical. In SPRINT-001 every blocking review finding was a coverage claim that read as true and was not, which is why mutation became mandatory. By SPRINT-003 the practice had produced roughly eighty recorded mutants, nine of them invalid, and had begun generating review rounds about mutation records rather than about defects.

Where it earned its place, it did so decisively — a mutant that **passed** revealed that a stack check could not detect a missing migration step at all ([T-0015](../product/tickets/T-0015-compose-stack-smoke-test.md)), and another showed that twelve of thirteen tests could not distinguish a correct issue-number allocator from one that duplicates under concurrency ([T-0005](../product/tickets/T-0005-create-and-read-issues.md)). Both fit the narrowed rule above.

Where the sprint's two most serious defects came from was somewhere else entirely: **exercising the running system in a state it was not built in.** See the next section.

### Verification must be attributable

A check against a locally-served endpoint proves nothing unless the response is bound to the process under test. On a machine running more than one stack — which is normal — a `curl` to `localhost` can be answered by something else entirely while the thing you are testing has failed to start.

Any verification against a running service therefore:

- runs under its own project name (`docker compose -p <name>`), so it cannot collide with another stack;
- asserts the specific container is running and healthy **before** any HTTP response is trusted;
- confirms attribution by stopping that container and observing the endpoint stop answering.

The same principle applies to tool output: **read the exit status of the tool you are checking, not of a pipeline it feeds.** `dotnet format … | grep …` reports grep's status.

SPRINT-001 recorded seven instances of a green signal measured from the wrong source, including the same port-collision false pass made twice — the second time by the person who had just written up the first as a lesson.

#### Exercise the system in a state it was not built in

The highest-yield verification this project has done is not a test and not a mutant. It is putting the running system into a state nobody wrote it for, and watching:

- a request carrying input nobody anticipated — a `U+0000` in a name produced an HTTP 500 with a zero-length body, a response the contract never declared ([T-0004](../product/tickets/T-0004-create-and-list-projects.md));
- a **database that already holds rows** — reverting a stack to the previous schema and running the real migrator revealed that every existing project's first issue would have been numbered 0, unreadable through the only declared read path ([T-0005](../product/tickets/T-0005-create-and-read-issues.md));
- a dependency removed underneath a live service — stopping PostgreSQL under an authenticated API.

Each was found by a person driving real infrastructure, and **none would have been found by any test in the suite, because every test starts from a state the code was designed for.** Both of the sprint's two worst defects came from this and neither came from mutation.

**These rules bind the test infrastructure too.** Test code is not where the rules come from; it is somewhere they apply, and it is the place they are most often skipped. Concretely:

- **Every command result is read, including teardown.** SPRINT-002's smoke harness enforced exit-code discipline on the stack it checked and discarded the result in the one place nothing checked — its own `DisposeAsync` — leaking containers and volumes on every run, invisibly ([RETRO-SPRINT-002](../delivery/retrospectives/RETRO-SPRINT-002.md)).
- **Identifiers that must be unique are not truncated.** A name shortened to a "long enough" width silently dropped the random component and made two runs share one namespace. A cap chosen to be big enough is the same defect with a larger number.
- **Gates are run in the working copy under test.** With work split across a trunk checkout and a ticket worktree, a gate run in the wrong one measures the wrong tree. SPRINT-002 recorded a validator result as green that was red on the branch it described.

### The gate

- The full relevant suite passes before a ticket enters `in-acceptance`. "Passes on my machine" with red CI is red.
- Coverage numbers are a signal, not a goal; an untested critical path fails review regardless of the percentage.
- A coverage claim without a recorded mutation is an assertion, not evidence.

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
