---
id: T-0011
title: "SPIKE: is OpenAPI Generator's aspnetcore output workable on ASP.NET Core 10?"
type: spike
status: in-acceptance
priority: high
owner: none
implemented_by: claude-sm-9d4e
accepted_by: none
depends_on: []
adrs: [ADR-0004, ADR-0006]
created: 2026-08-30
updated: 2026-08-30
---

# T-0011: SPIKE — is OpenAPI Generator's `aspnetcore` output workable on ASP.NET Core 10?

## Problem / Context

[ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md) commits the project to generating server contracts with OpenAPI Generator's `aspnetcore` generator, and names its own biggest risk: the generator's templates may not suit ASP.NET Core 10 — outdated scaffolding, weak nullable-reference-type support, awkward async signatures. The ADR says the verdict should be recorded when the first real endpoint is implemented.

Refinement judged that too late. Contract-first is a **hard constraint** (`PROJECT.md` §4) and the generator choice shapes how every controller in the codebase is written; discovering the output is unworkable *during* [T-0002](T-0002-contract-first-codegen-pipeline.md) would invalidate that ticket and any sprint built around it. The maintainer chose (2026-08-30) to answer the question first, in a time box.

## The Question

**Can OpenAPI Generator's `aspnetcore` generator produce server contracts that a .NET 10 / ASP.NET Core 10 project can implement cleanly — and if not, does [ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md) need superseding?**

Sub-questions worth answering while there:

- Does the generated code compile against .NET 10 with nullable reference types enabled and warnings as errors ([ENGINEERING.md](../../standards/ENGINEERING.md))?
- Are the generated controllers *implementable* — abstract classes or interfaces a hand-written controller can inherit — or do they assume they are the implementation?
- Are async signatures and cancellation tokens usable, or does the output force synchronous handlers?
- Does the `csharp` client generator produce something usable from the same document?
- Can the generator run from its official container image, removing the host-JDK prerequisite?

## Why It Matters Now

If the answer is no, the fallback in ADR-0004 is NSwag for the server while keeping OpenAPI Generator for future non-C# clients. Making that switch **before** T-0002 costs a superseding ADR; making it after costs a rebuilt pipeline and a broken sprint. The specification itself — the real asset — is unaffected either way.

## Time Box

**4 hours.** If the answer is not clear by then, the finding *is* "not clear in half a day", which is itself a signal about the toolchain, and the spike ends with that recorded.

## Output

- Findings written into this ticket's Work Log: what was generated, what compiled, what did not, with the actual error output where it failed.
- A verdict: **ADR-0004 stands** / **ADR-0004 should be superseded** (with the proposed alternative).
- If superseding: an ADR drafted via `create-adr`, and T-0002 updated to match before it is planned.
- If standing: T-0002's generator risk retired, and any lessons folded into its Technical Notes.

## Scope

### In Scope

- A throwaway OpenAPI document with one resource, one error shape, and one security scheme — enough to exercise the generator, not enough to be a product contract.
- Running both generators (`aspnetcore`, `csharp`), by container image if that works.
- Compiling the output against .NET 10 with the project's intended settings.
- Recording the evidence.

### Out of Scope

- Any production code. **Everything this spike produces is disposable and MUST NOT ship** — per the [DoD](../../governance/DEFINITION_OF_DONE.md)'s spike rule, code from a spike goes through a normal ticket or not at all.
- The real `spec/openapi.yaml` — that is T-0002's.
- Evaluating NSwag in depth. If the verdict is "supersede", the alternative's evaluation belongs in the superseding ADR, not here.

## Dependencies

None. This spike deliberately does **not** depend on [T-0001](T-0001-runnable-compose-stack.md): it needs only a scratch project and the generator, so it can run before or alongside the stack work. That is the point of answering it early.

Requires either a host JDK (25 verified present, 2026-08-30) or the generator's container image.

## Risks / Unknowns

- The spike may produce an ambiguous answer — output that compiles but is unpleasant. The verdict then becomes a judgment call, and the honest outcome is to say so and let the maintainer decide rather than forcing a binary.
- A scratch spec may not exercise the cases that actually hurt (polymorphism, nullable composition, complex error shapes). Keeping it minimal is a deliberate trade for the time box; a clean spike verdict is therefore weaker evidence than a real contract would give, and that limitation belongs in the findings.

## Definition of Ready

- [x] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — evaluated 2026-08-30. Per the DoR's **spike exception**, a spike needs the question, why it matters now, a time box, and the output form; all four are stated above. Acceptance criteria about product behaviour deliberately do not apply.

## Definition of Done

- [ ] Meets [DoD](../../governance/DEFINITION_OF_DONE.md) — for spikes: the question is answered (or the time box expired with findings), the findings are in this ticket, and follow-up tickets/ADRs are created and linked.

---

## Work Log

### 2026-08-30 — Business Analyst (claude-sm-9d4e)

- **Did:** Created during [T-0002](T-0002-contract-first-codegen-pipeline.md)'s refinement in a `refinement-session`, after the ARCH pass judged the generator risk too large to carry into implementation. The maintainer chose the spike route over validating inside T-0002.
- **Decided:** No dependency on T-0001 — the question needs a scratch project, not the stack. Making it independent means it can be answered immediately, which is the whole value.
- **Decided:** Time box 4 hours, with "no clear answer in half a day" as an admissible finding rather than a reason to keep going.
- **Remaining:** Run it.
- **Open questions / blockers:** none.
- **Branch / PR:** n/a
- **Test state:** n/a — not started.

### 2026-08-30 — Software Engineer (claude-sm-9d4e) — findings and verdict

Spike run in a scratch directory; **all code produced is disposable and none of it ships** (DoD spike rule). No branch was created — the deliverable is this write-up. Generator: `openapi-generator-cli` **7.18.0**, run both as the official container image and as the JAR on the local JDK 25. Throwaway spec: one resource with create + paginated list, an enum, a nullable optional field, an RFC 9457 problem shape, and a bearer security scheme.

## Verdict: **ADR-0004 should be superseded — for the server half only**

The `csharp` **client** generator is good and should be kept. The `aspnetcore` **server** generator cannot produce controllers this project can implement without breaking a standard. That is the split ADR-0004 itself anticipated: *"the fallback is NSwag for the server while keeping OpenAPI Generator for non-C# clients."*

## The blocking finding: generated server methods are synchronous

`operationIsAsync=true` is **silently ignored**. Every generated method is:

```csharp
public abstract IActionResult CreateWidget([FromBody]CreateWidgetRequest createWidgetRequest);
```

Never `Task<IActionResult>`, and never with a `CancellationToken`. I tested three configurations — `classModifier=abstract,operationModifier=abstract`, `operationModifier=abstract` alone, and neither — and got **0 async methods and 2 sync methods every time**. The flag is accepted without complaint and has no effect on this template path.

Why this is disqualifying rather than annoying: [ENGINEERING.md](../../standards/ENGINEERING.md) requires *"`async`/`await` all the way down for I/O; no `.Result`/`.Wait()`. Pass `CancellationToken` through to EF Core and HTTP calls."* A synchronous controller method that must query EF Core leaves only bad options — block on `.Result` (explicitly forbidden, and a deadlock risk), use EF Core's synchronous APIs (throwing away async I/O in an API whose entire job is database work), or return a custom `IActionResult` whose `ExecuteResultAsync` does the real work (technically possible, and a deeply strange pattern to impose on every endpoint in the product). There is also no `CancellationToken` at the boundary at all, so the cancellation half of that rule cannot be satisfied.

This is not a preference. It is a generated contract that cannot be implemented in compliance with a standard, on **every** endpoint, for the life of the project.

## Secondary findings

**No ASP.NET Core 9 or 10 target.** `aspnetCoreVersion` offers 2.0–8.0 and defaults to 8.0. Generated projects say `<TargetFramework>net8.0</TargetFramework>`. Retargeting to `net10.0` does compile — but the generated `.csproj` is generated output, and [ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md) forbids hand-editing it, so every regeneration would need post-processing or a `Directory.Build.props` override. Workable, but it is permanent friction on a tool that has no target for the runtime this project is built on.

**A vulnerable transitive dependency that configuration cannot remove.** As generated, the server pulls `Microsoft.AspNetCore.Mvc.NewtonsoftJson` **3.0.0** — a six-year-old package on a .NET 8 target — bringing `Newtonsoft.Json` 12.0.2 with a **high-severity advisory** (GHSA-5crp-9r3c-p9vr). Under this project's `TreatWarningsAsErrors`, that is `NU1903: Warning As Error` and **the build fails**.

Per the maintainer's instruction mid-spike to prefer System.Text.Json, I regenerated with `useNewtonsoft=false`, which removes the MVC Newtonsoft package. **Newtonsoft came back anyway** at 10.0.1, dragged in by `JsonSubTypes` 1.8.0, which the generator emits regardless. So the vulnerability is not configurable away; it needs a repository-level pin (`Newtonsoft.Json` 13.0.3) in our own project files. That works — but it means the contract-first pipeline permanently obliges us to pin around its own output.

**Other observations.** Generated `Nullable` is `annotations`, not `enable`, so the generated assembly is not null-checked to this project's standard. The generator warns *"OpenAPI 3.1 support is still in beta"* — and this project's contract is 3.1 ([PROJECT.md](../../PROJECT.md) §5).

## What worked well

- **The client generator is genuinely good.** `library=generichost` — which is the **default**, and uses **System.Text.Json** — produced a clean client with no Newtonsoft anywhere, both sync and async method pairs with `CancellationToken`, and it **built successfully on `net10.0`**. (My first run explicitly chose `library=httpclient`, which is the Newtonsoft-based one; that was my error, not the tool's.)
- **Consuming the generated server contract does work mechanically.** With the Newtonsoft pin in the consuming project, a controller inheriting the generated abstract class and implementing its members **builds clean under this project's full settings** — `Nullable=enable`, `TreatWarningsAsErrors`, `latest-recommended` analysers, 0 warnings. So the objection is the *shape* of the contract, not an inability to compile it.
- **Both distribution routes work.** The container image and the JAR both run. Note for [T-0002](T-0002-contract-first-codegen-pipeline.md): the image pull **exceeded a 10-minute timeout** on first fetch here before succeeding in the background; the 30 MB JAR downloaded in seconds. T-0002's refinement chose the container image to avoid a host-JDK prerequisite — that decision is still defensible, but a first-run pull is slow enough to be worth knowing.

## Recommendation

Supersede [ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md) with a split pipeline: **NSwag for the server contracts** (it generates async controller interfaces with `CancellationToken`, targets current ASP.NET Core, and is .NET-native), **OpenAPI Generator for clients** (keeping the polyglot capability that motivated the original choice). The specification itself — the actual asset — is unaffected either way, exactly as ADR-0004 predicted.

**I have not verified NSwag's output.** That claim rests on its documented behaviour, not on evidence I gathered, and it should be checked before the superseding ADR is accepted rather than trading one unvalidated assumption for another.

**Time box:** the question was answered well inside the 4 hours. Nothing was left unexplored that would change the verdict.

**Stopping here per the sprint plan.** SPRINT-001's Notes say that if this spike returns "supersede", the loop stops and re-plans rather than absorbing it, because T-0002's scope changes materially — a WoW §7 conversation, not a mid-sprint adjustment. A **Proposed** ADR-0006 accompanies this ticket; accepting it is the maintainer's decision.

### 2026-08-30 — Software Engineer (claude-sm-9d4e) — CORRECTION: verdict reversed

**The maintainer challenged the async finding and was right. The previous entry's blocking finding is withdrawn, and with it the recommendation to supersede ADR-0004.**

**What I got wrong.** I set `operationIsAsync=true`, saw synchronous `IActionResult` methods, tested three *modifier* combinations, and concluded the flag was ignored. It was not. The `aspnetcore` generator splits the concern across **two** options, and I only used one:

- `operationIsAsync` — makes the method body async
- **`operationResultTask`** — makes the method *return* `Task<>`

With both set, the generator emits exactly what is needed:

```csharp
public abstract Task<IActionResult> CreateWidget([FromBody]CreateWidgetRequest createWidgetRequest);
```

My three-configuration test varied `classModifier`/`operationModifier` while leaving `operationResultTask` at its default of `false` throughout — so it re-confirmed my own mistake three times rather than testing the hypothesis. That is the lesson worth keeping: I read the option list, found a plausible match, and stopped looking. `operationResultTask` was in the same list.

**Re-verified.** An async controller inheriting the generated abstract class, using `HttpContext.RequestAborted` for cancellation, **builds clean** under this project's full settings — `Nullable=enable`, `TreatWarningsAsErrors`, `latest-recommended` analysers, 0 warnings.

## Corrected verdict: **ADR-0004 stands. Do not supersede.**

ENGINEERING.md's async rule is satisfiable: methods are `async Task<IActionResult>` and take no `.Result`/`.Wait()`. The generated signature still carries no `CancellationToken` **parameter**, but `HttpContext.RequestAborted` supplies one inside the method, which meets the standard's requirement that cancellation reach EF Core. That is a mild ergonomic wart, not a violation.

## The working configuration, for T-0002 to inherit

```
-g aspnetcore
--additional-properties=aspnetCoreVersion=8.0,buildTarget=library,\
  classModifier=abstract,operationModifier=abstract,\
  operationIsAsync=true,operationResultTask=true,\
  nullableReferenceTypes=true,useSwashbuckle=false,useNewtonsoft=false,\
  packageName=<name>
```

Client: `-g csharp --additional-properties=library=generichost` (the default; System.Text.Json, async with `CancellationToken`, no Newtonsoft, builds on `net10.0`).

## Findings that survive the correction

These are real, were verified, and remain T-0002's problems to solve — none is disqualifying:

1. **No ASP.NET Core 9/10 target.** `aspnetCoreVersion` stops at 8.0 and emits `<TargetFramework>net8.0</TargetFramework>`. It compiles fine once retargeted, but generated project files must not be hand-edited, so T-0002 needs a `Directory.Build.props` override or a post-generation step.
2. **A vulnerable transitive `Newtonsoft.Json`.** `useNewtonsoft=false` removes the MVC package, but `JsonSubTypes` 1.8.0 pulls Newtonsoft 10.0.1 back in, carrying GHSA-5crp-9r3c-p9vr. Under `TreatWarningsAsErrors` this is `NU1903: Warning As Error` and the build fails. Fixed by pinning `Newtonsoft.Json` 13.0.3 at the repository level — in our own files, so the never-hand-edit-generated-code rule holds.
3. **Generated `Nullable` is `annotations`, not `enable`** — the generated assembly is not null-checked to this project's standard.
4. **The generator warns that OpenAPI 3.1 support "is still in beta"**, and this project's contract is 3.1.
5. **First container-image pull exceeded 10 minutes** here; the 30 MB JAR took seconds. T-0002's choice of the container image stands, but the first-run cost is worth documenting.

**Consequence:** [ADR-0006](../../architecture/adr/ADR-0006-nswag-for-server-contracts.md) is **Rejected** — the evidence that motivated it was my error. [T-0002](T-0002-contract-first-codegen-pipeline.md) is **not** stale and remains Ready; it should absorb the configuration above and findings 1–2 during implementation.
