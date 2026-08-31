# ADR-0006: Generate server contracts with NSwag, keeping OpenAPI Generator for clients

## Status

Rejected

> **Rejected 2026-08-30, the same day it was proposed.** The blocking evidence in the Context below — that the `aspnetcore` generator cannot emit async controller methods — was **wrong**. The generator needs *two* options, `operationIsAsync` **and** `operationResultTask`; the spike set only the first. With both, it emits `Task<IActionResult>`, and a controller implementing it builds clean under this project's full settings. The maintainer challenged the finding and was right.
>
> [ADR-0004](ADR-0004-contract-first-openapi-code-generation.md) therefore **stands, unsuperseded**. The body below is preserved unedited as the record of what was proposed and why it was wrong. The secondary findings it cites — no ASP.NET Core 9/10 target, and a vulnerable transitive `Newtonsoft.Json` via `JsonSubTypes` — are real, and are [T-0002](../../product/tickets/T-0002-contract-first-codegen-pipeline.md)'s to handle.

## Date

2026-08-30

## Context

[ADR-0004](ADR-0004-contract-first-openapi-code-generation.md) chose OpenAPI Generator for both halves of the contract-first pipeline — `aspnetcore` for server contracts, `csharp` for clients — and named its own biggest risk: that the `aspnetcore` templates might not suit ASP.NET Core 10. It deferred the verdict to "the first real endpoint". Refinement judged that too late and the maintainer commissioned [T-0011](../../product/tickets/T-0011-spike-aspnetcore-generator-viability.md), a time-boxed spike, to answer it before [T-0002](../../product/tickets/T-0002-contract-first-codegen-pipeline.md) built a pipeline around it.

The spike ran generator **7.18.0** against a representative throwaway contract and found one disqualifying problem, evidenced across three separate generator configurations:

**Generated server methods are synchronous and take no cancellation token.** `operationIsAsync=true` is accepted without complaint and silently ignored on this template path. Every method is emitted as `public abstract IActionResult CreateWidget(...)` — never `Task<IActionResult>`.

[`standards/ENGINEERING.md`](../../standards/ENGINEERING.md) requires async I/O throughout, forbids `.Result`/`.Wait()`, and requires `CancellationToken` to reach EF Core. A synchronous controller boundary in a database-backed API leaves only bad options: block a thread (forbidden), abandon async EF Core, or implement every endpoint as a custom `IActionResult` doing its real work in `ExecuteResultAsync`. This would apply to **every endpoint in the product, permanently**.

Two secondary findings compound it. The generator has **no ASP.NET Core 9 or 10 target** (`aspnetCoreVersion` stops at 8.0), so generated projects say `net8.0` and need post-processing to build on this project's runtime — on generated files ADR-0004 forbids editing. And the emitted project drags **`Newtonsoft.Json` with a high-severity advisory** (GHSA-5crp-9r3c-p9vr) which fails this project's warnings-as-errors build; setting `useNewtonsoft=false` does not remove it, because `JsonSubTypes` pulls it back. It is fixable only by pinning around the tool's own output.

The `csharp` **client** generator, by contrast, tested clean: `library=generichost` (the default) uses **System.Text.Json**, emits async methods with `CancellationToken`, carries no Newtonsoft, and builds on `net10.0`.

## Decision

Split the pipeline by audience:

- **Server contracts are generated with NSwag** — abstract controllers and DTOs the API implements, targeting current ASP.NET Core with async signatures and cancellation support.
- **Clients remain OpenAPI Generator** (`csharp`, `library=generichost`, System.Text.Json), preserving the polyglot capability that motivated ADR-0004: the same specification can still emit TypeScript, Python, or Go clients without a new toolchain.

Everything else in ADR-0004 stands unchanged and is **not** superseded: the specification is still hand-authored first at `spec/openapi.yaml`; generated code is still never hand-edited; generated output is still committed; a spec change and its regenerated output still travel together; drift is still a merge gate. Only the server-side tool changes.

## Options Considered

1. **NSwag for the server, OpenAPI Generator for clients (chosen)** — resolves the async problem at its root, is .NET-native so it tracks current ASP.NET Core, integrates with MSBuild (removing the JDK from the server path), and keeps the polyglot client story. Costs: two tools, and NSwag's server output is itself unverified here.
2. **Keep OpenAPI Generator for both** — one tool, one config, no churn, and the spike proved the output *compiles* under this project's settings. Rejected: it forces a synchronous controller boundary on every endpoint, which either violates ENGINEERING.md or imposes a strange pattern product-wide, and it obliges the project to pin around a vulnerable transitive dependency indefinitely.
3. **Keep OpenAPI Generator and relax the async standard** for generated boundaries. Rejected: it changes a sound engineering rule to accommodate a tool, on a database-backed API where async I/O is the point. Fixing the tooling is cheaper than degrading the codebase.
4. **Write custom Mustache templates** overriding the generator's controller template. Rejected: the project would then maintain code-generation templates as a side quest — real work, permanently, for one maintainer on a proof of concept.

## Consequences

### Positive

- Controllers can be `async` with `CancellationToken`, so ENGINEERING.md's async rule survives contact with the generated contract.
- NSwag targets current ASP.NET Core, removing the `net8.0`-retargeting friction and the need to post-process generated project files.
- The vulnerable `Newtonsoft.Json` chain leaves the server path.
- Server generation can run in MSBuild, so the JDK is needed only for client generation — a lighter default developer setup.
- The polyglot client capability, the original reason for choosing OpenAPI Generator, is fully retained.

### Negative

- **Two toolchains** where ADR-0004 deliberately chose one: two configurations, two upgrade paths, two failure modes. This is the cost ADR-0004 paid to avoid, now being paid anyway.
- **NSwag's output is unverified.** This ADR trades a measured problem for a documented promise. It should not be accepted without checking NSwag's generated controllers the way T-0011 checked OpenAPI Generator's — otherwise it repeats the mistake ADR-0004 made.
- Churn: [T-0002](../../product/tickets/T-0002-contract-first-codegen-pipeline.md) was refined and made Ready against the previous decision, and its Technical Notes (the pinned container image, `tools/generate.sh`) need rework before it can be implemented.
- Two generators mean two idioms in one repository — generated server contracts and generated clients will not look like each other.

## Risks

- **NSwag may have its own disqualifying flaw**, discovered the same way. Mitigation: verify before accepting this ADR, not after. A second unvalidated tool choice would be the same error twice.
- Splitting the pipeline weakens the "one command regenerates everything" property; if the two halves can drift independently, the drift check must cover both.
- If NSwag's C# client is materially better than OpenAPI Generator's, there will be pressure to drop the polyglot generator — which would quietly discard the reason ADR-0004 chose it. That is a decision to make deliberately, not by convenience.

## Follow-up Actions

- **Verify NSwag's server output before this ADR is accepted** — the same bar T-0011 applied: async signatures, cancellation, .NET 10 target, dependency health, and a controller implementable under `TreatWarningsAsErrors`.
- Re-refine [T-0002](../../product/tickets/T-0002-contract-first-codegen-pipeline.md) against whichever decision is accepted; it is currently Ready against ADR-0004 and would be stale.
- If accepted, set [ADR-0004](ADR-0004-contract-first-openapi-code-generation.md) to `Superseded by ADR-0006` and cross-link.

## Related ADRs

- Supersedes (if accepted) the server-generator half of [ADR-0004](ADR-0004-contract-first-openapi-code-generation.md). The contract-first principle, the spec-first authoring rule, and the drift gate are unchanged.
- Scoped by [ADR-0005](ADR-0005-operational-endpoints-outside-the-api-contract.md), which is unaffected.

## Related Tickets

- [T-0011](../../product/tickets/T-0011-spike-aspnetcore-generator-viability.md) — the spike that produced the evidence
- [T-0002](../../product/tickets/T-0002-contract-first-codegen-pipeline.md) — the ticket this decision reshapes
