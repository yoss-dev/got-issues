---
id: T-0012
title: Pin container images to immutable digests
type: technical
status: ready
priority: normal
owner: none
implemented_by: none
accepted_by: none
depends_on: [T-0001]
adrs: [ADR-0003]
created: 2026-08-30
updated: 2026-08-31
---

# T-0012: Pin container images to immutable digests

## Problem / Context

Raised as a non-blocking finding during T-0001's independent review (`claude-rev-2c8d`, 2026-08-30) and deliberately deferred: pinning to digests the implementer could not verify would have been worse than leaving it.

`compose.yaml` and the API Dockerfile reference **floating tags** — `mcr.microsoft.com/dotnet/sdk:10.0`, `mcr.microsoft.com/dotnet/aspnet:10.0`, `postgres:18-alpine`. Those tags move. Two builds of the same commit can therefore produce different images, which is the same class of problem the NuGet lock file solves for packages ([PROJECT.md](../../PROJECT.md) §5) and which [ENGINEERING.md](../../standards/ENGINEERING.md) addresses directly: *"Pin or lock dependency versions per ecosystem convention."* Base images are dependencies.

## Desired Outcome

Every container base image is referenced by an immutable digest, so a given commit builds the same images tomorrow as today.

## User / Business Value

Reproducibility. Without it, "it worked yesterday" has no answer, and a silent base-image change is indistinguishable from a code defect — expensive to diagnose precisely when something is already wrong.

## Scope

### In Scope

- Replace floating tags with `image@sha256:…` digests in `compose.yaml` and the Dockerfiles, keeping the human-readable tag in a comment so the version is still legible.
- **The OpenAPI Generator image in `tools/generate.sh`.** It is pinned to `v7.18.0` — an explicit version, but still a *mutable tag*: the same tag can be repushed. Added 2026-08-31 from T-0002's acceptance (`claude-qa-5a71`, note N1); not a defect there, since AC8 asked for an explicit version pin and has one, but the same class of dependency — and the one whose output is committed to this repository.
- Document how to refresh a digest deliberately.
- Verify the stack still builds and runs (T-0001's criteria remain true).

### Out of Scope

- Automated update tooling (Renovate/Dependabot) — that needs a remote and CI (`PROJECT.md` Q6).
- Pinning NuGet packages; already handled by the lock file.

## Acceptance Criteria

- [ ] AC1: Given the repository, when `compose.yaml`, the Dockerfiles and `tools/generate.sh` are inspected, then every image is referenced by digest and no mutable tag remains.
- [ ] AC1b: Given the digest-pinned generator, when `./tools/generate.sh` runs, then the output is byte-identical to what is committed — the pin changes provenance, not content.
- [ ] AC2: Given a pinned digest, when the stack is built and started, then all services reach a healthy state as in T-0001.
- [ ] AC3: Given the README or a comment beside each pin, when a reader asks how to move to a newer base image, then the documented procedure answers it.
- [ ] AC4: Given each pinned digest, when it is inspected, then it is the **multi-architecture manifest digest**, not a single-architecture image digest — verifiable with `docker buildx imagetools inspect`, and the tag comment beside it records which.
- [ ] AC5: Given `apps/GotIssues.SmokeTests`, when `tools/smoke.sh` runs against the pinned stack, then all twelve checks pass — [T-0015](T-0015-compose-stack-smoke-test.md) is what turns AC2 from a manual observation into an automated one, and a bad pin is precisely the failure it was built to catch.

## Examples / Scenarios

- Rebuild after upstream publishes a new `postgres:18-alpine`: the build uses the pinned digest, unchanged.
- Deliberate refresh: follow the documented procedure, digest changes in one reviewable commit.
- Pin an `arm64`-specific digest by mistake: caught by AC4 before it reaches a machine that cannot run it (**counter-example** — this is the failure mode most likely to pass every local check on the machine that introduced it).
- Repush of `openapitools/openapi-generator-cli:v7.18.0` upstream: the digest pin means `generate.sh` still produces the committed tree, and AC1b is what proves it.

## Dependencies

**T-0001** — the images and compose file must exist.

## Risks / Unknowns

- **The generator image matters more than the base images**, because it writes code that is committed: a silently different generator produces a silently different tree, and the drift check would then report a diff nobody caused. That makes it the pin most worth getting right.
- Digests are architecture-specific unless the manifest-list digest is used. Pinning an `arm64` image digest would break any future `amd64` build. **Use the multi-arch manifest digest**, and the ticket should verify that explicitly rather than assume it.
- Pinned images go stale silently, including on security patches. Without CI there is nothing to notice; the documented refresh procedure is the only mitigation, and that is a real limitation to state rather than paper over.

## Testing Notes

Verified by rebuilding from scratch and re-running T-0001's stack criteria — **which is now automated**: [T-0015](T-0015-compose-stack-smoke-test.md) landed 2026-08-31 and `tools/smoke.sh` covers cold start, restart and token validation against the real stack (AC5). The manual half is AC4's manifest check, which is a one-off inspection per pin rather than an ongoing cost.

**Mutate first** ([TESTING.md](../../standards/TESTING.md)): AC1b is the claim worth breaking — change the pinned generator digest to a different published version and confirm `check-drift.sh` reports a diff. A pin whose effect on output is untested is a provenance claim, not a reproducibility one.

## Relevant ADRs & Documentation

- [ADR-0003](../../architecture/adr/ADR-0003-initial-technology-stack.md) — the stack and the Compose constraint
- [ENGINEERING.md](../../standards/ENGINEERING.md) — dependency pinning

## Definition of Ready

- [x] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — evaluated 2026-08-31 during `refinement-session`. All nine universal items hold. Item 8 (testable) improved materially since creation: T-0015's smoke tier now automates what was a manual stack check (AC5). Conditional items: security relevant — a digest pin is a supply-chain control, and the staleness limitation is stated rather than papered over; no UX, no data-shape change, no ADR-bar decision.

## Definition of Done

- [ ] Meets [DoD](../../governance/DEFINITION_OF_DONE.md) — checked by `complete-ticket`.

---

## Work Log

### 2026-08-30 — Software Engineer (claude-sm-9d4e)

- **Did:** Created to capture a T-0001 review deferral, so DoD item 4 is satisfied by a linked ticket rather than Work Log prose.
- **Decided:** none.
- **Remaining:** Refinement, then implementation.
- **Open questions / blockers:** none.
- **Branch / PR:** n/a
- **Test state:** n/a — not started.

### 2026-08-31 — Software Engineer (claude-sm-9d4e)

- **Did:** Widened to cover the OpenAPI Generator image in `tools/generate.sh`, from T-0002's acceptance note N1.
- **Decided:** widened rather than leaving the note homeless or citing a scope that did not accept it. This ticket's In Scope named only `compose.yaml` and the Dockerfile, so pointing the generator image here would have been the false-pointer failure [DoD](../../governance/DEFINITION_OF_DONE.md) item 4 exists to prevent — already made twice on this project. One sentence of scope is cheaper than a third instance.
- **Decided:** recorded that the generator pin is the one most worth getting right, because that image writes code which is committed.
- **Remaining:** Refinement.
- **Open questions / blockers:** none.
- **Branch / PR:** n/a
- **Test state:** n/a — not started.

### 2026-08-31 — Refinement (claude-sm-9d4e) — PO · BA · ENG · ARCH · QA · SEC

**Product (PO).** Value unchanged and now cheaper to realise: [T-0015](T-0015-compose-stack-smoke-test.md)
landed, so "the stack still works after pinning" is an automated check rather than a person
remembering to try it (AC5).

**Analysis (BA).** Two ambiguities closed as criteria. **AC4** requires the *multi-architecture
manifest* digest — the ticket's Risks named this and no criterion enforced it, which is exactly
the shape of gap that reads as covered. **AC5** binds the behavioural half to the smoke tier
rather than to a person. Added the arm64 counter-example because it is the failure that passes
every check on the machine that introduces it.

**Engineering (ENG).** Mechanical: four images across `compose.yaml`, two Dockerfiles and
`tools/generate.sh`. The generator pin is the one with teeth — its output is committed, so a
silently different generator produces a diff the drift check reports and nobody caused.

**Architecture (ARCH).** No ADR-bar decision; [ENGINEERING.md](../../standards/ENGINEERING.md)
already requires pinning and this applies it to the ecosystem it had not reached.

**QA.** All criteria verifiable; AC1b named as the one to mutate, since a pin whose effect on
output is untested is a provenance claim rather than a reproducibility one.

**Security.** A digest pin is a supply-chain control. The honest limitation stays stated: pinned
images go stale silently, including on security patches, and without CI nothing notices. The
documented refresh procedure (AC3) is the whole mitigation, and calling it sufficient would be
the claim outrunning the evidence this project keeps catching.

**Sizing.** Well within the guideline.

- **Did:** Applied all six perspectives; added AC4 and AC5; folded T-0015's arrival into the
  testing approach.
- **Decided:** manifest-list digests, not per-architecture; the smoke tier owns the behavioural
  verification.
- **Remaining:** implementation.
- **Open questions / blockers:** none.
- **DoR verdict:** **ready.**
- **Branch / PR:** n/a
- **Test state:** n/a — not started.
