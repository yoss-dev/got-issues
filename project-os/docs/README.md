# Framework Help

Guides and tutorials for working with the delivery framework. These documents **explain**; the governance documents **rule**. If a guide here ever contradicts [`WAY_OF_WORKING.md`](../governance/WAY_OF_WORKING.md), the governance document wins and the guide has a bug — fix it via a normal ticket.

## The mental model in five ideas

1. **The repository is the team's only memory.** Anything a teammate — human or agent — would need tomorrow lives in a file, never only in a conversation. Tickets carry a Work Log precise enough that a stranger can resume the work.
2. **Personas, not job titles.** Whoever performs an activity adopts the matching persona ([PERSONAS.md](../governance/PERSONAS.md)) and inherits its authority limits. The critical separations: the implementer never accepts their own work, reviewers don't fix the code they review, and only the Product Owner persona touches acceptance criteria.
3. **Work flows through gates.** Idea → ticket → *Ready* (DoR gate) → sprint → in-progress → *reviewed merge* → in-acceptance → *independent acceptance + DoD gate* → done. Gates are enforced by the [validator](../../tools/validate-project-os/validate.py) and git mechanics wherever mechanically possible.
4. **Two git lanes** ([GIT.md](../standards/GIT.md)): delivery state (claims, statuses, sprint files) commits straight to the trunk with `os:` messages; source code travels ticket branches — each in its own worktree — and merges via reviewed PR.
5. **Skills are the executable process.** Every repeatable activity has a `SKILL.md` in [`skills/`](../skills/README.md); agents follow them instead of improvising. In Claude Code they're slash commands: `/pick-up-ticket`, `/refine-ticket`, etc.

## Find what you need

| I want to… | Read |
| --- | --- |
| Start a brand-new project from the foundation | [Tutorial: bootstrapping a project](tutorial-bootstrap.md) |
| Turn an idea into sprint-ready work | [Tutorial: from idea to Ready](tutorial-idea-to-ready.md) |
| Plan a sprint and ship a feature through all gates | [Tutorial: implementing a feature](tutorial-implement-feature.md) |
| Look up a status, command, or convention fast | [Cheatsheet](cheatsheet.md) |
| Understand my job as the human in the loop | [Guide: the human's role](guide-humans.md) |
| Fix a stuck or inconsistent situation | [Troubleshooting](troubleshooting.md) |
| Understand a specific activity in depth | The activity's [`SKILL.md`](../skills/README.md) — skills are reference docs too |
| Know the actual rules | [`WAY_OF_WORKING.md`](../governance/WAY_OF_WORKING.md), [DoR](../governance/DEFINITION_OF_READY.md), [DoD](../governance/DEFINITION_OF_DONE.md), [GIT.md](../standards/GIT.md) |

## The three tutorials share one running example

A small internal tool — **go-links**, a team URL shortener (`go/payroll` → the long intranet URL) — adopted as a fresh project, taken from raw idea to a shipped first feature. Names used throughout: `pat` (the human), `claude-po-3e21`, `claude-eng-4f2a`, `claude-qa-b81d` (agent sessions, ids per the [identity convention](../standards/GIT.md)).
