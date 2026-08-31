---
id: T-0011
title: "SPIKE: is OpenAPI Generator's aspnetcore output workable on ASP.NET Core 10?"
type: spike
status: ready
priority: high
owner: none
implemented_by: none
accepted_by: none
depends_on: []
adrs: [ADR-0004]
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
