# Guide: The Human's Role

Agents execute the process; a few decisions are structurally yours. This guide is the job description for the human(s) in the loop — typically wearing the Product Owner hat, sometimes the final-approval hat for governance and architecture.

## What only you can do

- **Answer escalations.** Agents stop and ask when product behavior is ambiguous, requirements conflict, an architectural choice changes business outcomes, security/privacy is unclear, something destructive or paid is needed, or the sprint commitment must change ([WoW §13](../governance/WAY_OF_WORKING.md)). Each escalation arrives with options, tradeoffs, and a recommended default — often you just confirm the default.
- **Own product truth.** Priorities, acceptance criteria content, scope changes on committed work, accepting/rejecting ideas, and whether an outcome actually satisfies the need.
- **Approve rule changes.** `evolve-governance` proposals that alter what's mandatory need a human yes, arriving as PRs on the protected governance paths.
- **Decide contested architecture.** ADRs whose options differ materially in cost, risk, or business consequence stay `Proposed` until you decide — the ADR itself is the decision brief.

## Your operating rhythm

**The 2-minute daily scan:** open [`delivery/CURRENT_SPRINT.md`](../delivery/CURRENT_SPRINT.md) and read *Blockers & Escalations*. Everything waiting on you is there — that section exists so you never have to hunt. Then a glance at `git log --oneline -15`: the `os:` messages are a readable delivery journal.

**When answering, write it down — in the repo.** The iron rule: an answer that exists only in chat unblocks nothing. Either edit the ticket's Work Log yourself, or give the answer to an agent and check it transcribed your words (attributed, dated) before acting. This is what lets the next session, next week, know why.

**Per sprint:** confirm the goal at planning (or delegate with "your call"); read the retrospective and its previous-actions accounting — repeatedly dropped actions are your signal the improvement loop needs weight behind it.

## Asking agents for work

Name the activity and the artifact; the skills do the rest:

- *"Refine T-0012"* / *"Plan the next sprint"* / *"Pick up the next ticket"* — clean skill invocations.
- *"Let's refine"* (`/refinement-session`) — the interactive batch: the agent ranks candidates by impact and dependency order (or criteria you name), you pick, it refines them one at a time with you on hand for live answers — the highest-leverage 30 minutes you can give the backlog before planning.
- *"Drain the sprint"* (`/run-sprint`) — the autonomous loop: it processes committed tickets end to end, parks anything needing you as `blocked` with a recorded escalation, keeps working everything else, and exits with one batched decision digest instead of interrupting you per question.
- *"There's a bug: pasting a URL with spaces 500s"* — expect a bug ticket + sprint recording, not a silent hotfix.
- *"Capture this idea: …"* — the cheapest way to get a thought out of your head and into the system.
- Avoid *"just quickly add X to what you're doing"* — that's the silent-scope-expansion anti-pattern; ask for a ticket instead. If it's genuinely urgent, say so: the DoR has an urgent-bug lane.

## What to spot-check (trust, but sample)

- A freshly-Ready ticket: would *you* recognize done from its acceptance criteria alone?
- A `done` ticket's Work Log: is there real acceptance evidence, or narration?
- `PROJECT.md` fact tags: anything `[confirmed]` you never actually confirmed?
- `python3 tools/validate-project-os/validate.py` — green means the state at least agrees with itself.

## Things to resist

- Answering only in chat (see above — it will bite the next session).
- Editing acceptance criteria mid-implementation casually — it's allowed *as a recorded PO decision*, but each one weakens the gate.
- Approving governance changes proposed to rescue a specific failing ticket — that's the canonical violation, however reasonable it sounds in the moment.
- Letting `[open]` questions rot in `PROJECT.md` §7 — they silently become blockers.
