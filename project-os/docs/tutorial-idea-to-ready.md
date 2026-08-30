# Tutorial: From Idea to Ready

Goal: take a raw thought through capture → ticket → refinement until it verifiably meets the Definition of Ready. This is the product half of the lifecycle; nothing here writes code.

## 1. Capture the idea (30 seconds, zero commitment)

Pat mentions: *"people keep pasting month-old intranet links from Slack that 404 — we should have go/ short links."* Anyone (or `/capture-idea`) appends to [`product/IDEAS.md`](../product/IDEAS.md) per the [idea template](../templates/IDEA_TEMPLATE.md):

```markdown
## IDEA-001: go/ short links

- **Status:** captured
- **Date / Source:** 2026-09-01 — pat; recurring Slack complaints
- **Idea:** memorable go/name links that redirect to intranet URLs
- **Motivation:** deep links rot and are unfindable; people re-ask monthly
- **Possible value:** less time hunting links; fewer stale-link 404s
- **Unresolved questions:** who may create links? namespaces per team? edit vs immutable?
```

Key discipline: capture faithfully, **don't refine here**. No priority, no design, no promises. Ideas that turn out bad are rejected later with a one-line reason and kept — that's institutional memory, not clutter.

## 2. Promote to a ticket (a Product Owner decision)

When the PO persona judges it worth pursuing, `/create-ticket` creates `product/tickets/T-0001-create-short-link.md` from the [ticket template](../templates/TICKET_TEMPLATE.md) and registers it in [`BACKLOG.md`](../product/BACKLOG.md). What matters at this stage:

- **Outcome, not implementation:** "an employee can claim `go/<name>` pointing at a URL and the link redirects" — not "build a POST /links endpoint".
- **Honest gaps:** the namespace question goes into *Risks / Unknowns*, not quietly resolved by the agent. An imperfect ticket that admits its gaps beats a polished fiction.
- The idea's status line becomes `promoted → T-0001`.

## 3. Refine until Ready — or say why not

`/refine-ticket T-0001`. The agent rotates through five perspectives (see [the skill](../skills/refine-ticket/SKILL.md)) and this is where the ticket earns its quality:

- **Product:** is this still valuable? Would the PO recognize "done" from the criteria alone?
- **Analysis:** the ambiguity hunt. The test for every criterion: *could two reasonable implementers build different things and both claim compliance?* Before: "short links work". After: `Given go/payroll → https://intranet/x, GET /payroll responds 302 with Location: https://intranet/x; unknown names respond 404 with a "claim this link" page.` Edge cases become *Examples / Scenarios*: name collisions, reserved names, malformed targets, 100-character names.
- **Engineering:** implementable? Hidden dependencies? Pointers into the codebase (marked as suggestions).
- **Architecture:** does anything meet the [ADR bar](../architecture/adr/README.md)? A redirect-latency requirement might; naming a handler function doesn't.
- **QA:** is every criterion verifiable? Testing Notes filled in.

Then the honest verdict:

- **Everything holds** → `status: ready`, DoR checkbox ticked (the [validator](../../tools/validate-project-os/validate.py) enforces this), Work Log entry lists the perspectives applied.
- **A real gap** — say the namespace question turns out to change the data model → the ticket **stays `backlog`** with the question recorded and routed to pat. *"Not ready, because X" is a successful refinement.* Marking tickets Ready to keep work moving is the anti-pattern the DoR exists to stop.

Pat answers by writing into the ticket (or the agent transcribes the answer verbatim, attributed) — an answer that lives only in chat doesn't count ([WoW §13](../governance/WAY_OF_WORKING.md)).

## 4. Know the shortcuts

The DoR bends deliberately for two cases ([DoR exceptions](../governance/DEFINITION_OF_READY.md)): **urgent production bugs** enter with repro + severity + a verifiable "fixed means" statement, and **spikes** need only a question, a time box, and an output form. Everything else queues through the full gate.

Next: [implementing a feature](tutorial-implement-feature.md).
