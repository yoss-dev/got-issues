# ADR-0004: Generate server contracts and clients from a hand-authored OpenAPI specification using OpenAPI Generator

## Status

Accepted

## Date

2026-08-30

## Context

The maintainer set contract-driven development as a hard constraint ([`PROJECT.md`](../../PROJECT.md) §4): the OpenAPI specification is written first, and controllers and clients are generated from it, automated as far as the toolchain allows. What remained to decide was *which* toolchain — a choice that is expensive to reverse, because it determines the generated code's shape and therefore how every controller in the codebase is written.

Forces:

- The API is the product ([PRODUCT_VISION](../../product/PRODUCT_VISION.md)). Its specification must be authored deliberately, not inferred from attributes on controllers.
- Drift between spec and implementation is the failure mode this constraint exists to prevent. The pipeline must make drift detectable, ideally at build time.
- The stack is .NET ([ADR-0003](ADR-0003-initial-technology-stack.md)), so C# generation quality matters most — but the integrator persona may eventually want clients in other languages.
- One maintainer: toolchain ceremony has to stay low.

## Decision

The OpenAPI 3.1 specification lives at `spec/openapi.yaml` and is **hand-authored first**. It is the only place the API surface is designed.

Code is generated from it with the **OpenAPI Generator CLI**, using two generators:

- `aspnetcore` — abstract server controllers and DTOs, which the API service implements;
- `csharp` — the typed client SDK.

Rules that follow from this and are binding on every ticket:

1. **Generated code is never hand-edited.** Output lands in its own directory under `libs/`; changing behaviour means changing the spec and regenerating.
2. **Generated output is committed**, so regeneration produces a reviewable diff and drift is visible in review.
3. **A spec change and its regenerated output travel in the same commit.** A build or check that regenerates and finds a diff is a failure, not a warning.
4. Generation is driven by a script in `tools/`, so it is one command for a human and one command for an agent.

## Options Considered

1. **OpenAPI Generator CLI, `aspnetcore` + `csharp` generators (chosen)** — the polyglot, widely used generator. Chosen by the maintainer for its language coverage: the same specification can later emit TypeScript, Python, or Go clients for integrators without changing the pipeline. Mature, heavily exercised across ecosystems, and explicitly spec-first.
2. **NSwag** — the .NET-native option, with MSBuild integration that makes generation part of `dotnet build` and no external runtime dependency. Rejected: C#-only, so a future non-C# client would need a second toolchain, and the maintainer prioritised polyglot coverage over the tighter .NET integration.
3. **Kiota for clients + NSwag for the server** — Microsoft's first-party client generator produces the most ergonomic C# clients. Rejected: two tools, two configurations, two upgrade paths, and Kiota's fluent request-builder style is a different idiom from the generated server contracts. Not worth the moving parts for a solo project.
4. **Code-first with a generated specification (Swashbuckle/`Microsoft.AspNetCore.OpenApi`)** — the .NET default: write controllers, emit the spec. Rejected outright: it inverts the required direction. The spec becomes a description of whatever the code happens to do, which is precisely the failure mode ("the API is an afterthought") the product positions itself against.

## Consequences

### Positive

- The specification cannot drift from the implementation: the code that defines the surface *is* generated from it, and committed output makes any divergence show up as a diff.
- API design happens in one reviewable file, before implementation — the contract is discussed on its own terms.
- Clients in other languages cost a generator invocation, not a new toolchain.
- Implementers get an unambiguous instruction for where new code goes: implement the generated interface, never define routes.
- The published specification is, by construction, the accurate API documentation ([DOCUMENTATION.md](../../standards/DOCUMENTATION.md)).

### Negative

- **A JDK is now a build dependency.** OpenAPI Generator is a Java tool, so every developer machine and any future CI image needs a JDK (25 verified locally) alongside the .NET SDK. This is a genuine cost of choosing polyglot coverage over NSwag's MSBuild integration.
- **Generated C# is less idiomatic than NSwag's or Kiota's**, and the `aspnetcore` generator's output shape is opinionated; the codebase must live with its conventions rather than the team's.
- **Committed generated code produces large, noisy diffs** and can create merge conflicts that carry no information.
- **Generation is a separate step**, not part of `dotnet build`, so "regenerate after editing the spec" is a discipline the process must enforce rather than something the compiler catches.
- The spec becomes a bottleneck artefact: every change starts there, which is the point, but it slows down changes that would otherwise be a one-line edit.
- Upgrading the generator can reshape all generated code at once, producing a large mechanical diff that is hard to review.

## Risks

- **The `aspnetcore` generator's output proves awkward** for a real ASP.NET Core 10 application (outdated templates, poor nullable-reference-type support, clumsy async signatures). Noticed as soon as the first endpoint is implemented — this is the earliest and most important thing to validate. Mitigation: the fallback is NSwag for the server while keeping OpenAPI Generator for non-C# clients; the spec, which is the real asset, is unaffected either way.
- **Discipline erosion**: someone hand-edits generated code under time pressure and the guarantee silently dies. Noticed only if a regeneration check exists — hence rule 3, which must become an actual automated check rather than a written intention.
- **Committed generated output becomes intolerable noise.** Noticed in review fatigue; mitigated by generating at build time into an ignored directory instead, at the cost of losing diff-visible drift detection.

## Follow-up Actions

- Ticket the specification skeleton and generation script (`tools/`), including the "regenerate produces no diff" check — via `create-ticket` in the first refinement pass.
- Validate the `aspnetcore` generator's output against ASP.NET Core 10 on the first real endpoint, and record the result in that ticket's Work Log. If it fails the bar, this ADR needs superseding rather than quiet deviation.
- Add the JDK requirement to the root README prerequisites (done at bootstrap).

## Related ADRs

- Depends on [ADR-0003](ADR-0003-initial-technology-stack.md): controller-based ASP.NET Core was chosen *because* this generator emits abstract controllers.

## Related Tickets

None yet — this decision predates the first ticket. Tickets implementing the pipeline link back here.
