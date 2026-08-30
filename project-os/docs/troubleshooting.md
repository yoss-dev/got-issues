# Troubleshooting

Symptoms → causes → fixes. Ordering principle everywhere: **fix the state, never the checker; record the fix, never rewrite history.**

## The validator is red

Read the finding — it names the file and the rule. Fix the *state* (the mirror that drifted, the checkbox that was skipped, the owner that lingered) with a normal `os:` commit. Never edit the validator to make a finding go away; if you believe a rule itself is wrong, that's an [`evolve-governance`](../skills/evolve-governance/SKILL.md) proposal.

## My claim push was rejected

You lost the race — another agent claimed between your pull and push. Working as designed: `git pull`, discard your claim edit, pick another ticket via `pick-up-ticket`. Never force-push a claim through.

## A ticket is `in-progress` but its owner has vanished

Stale-claim protocol ([WoW §7](../governance/WAY_OF_WORKING.md)): confirm no commits from that owner for 24 h (`git log --oneline --all | grep <id>` or the ticket's history), then as SM: status back to `committed` (or `blocked` if a blocker is recorded), owner cleared, Work Log entry with the evidence, `os: T-NNNN stale claim released`. Takeover resumes from the existing Work Log. Don't do this to an active owner because you want the ticket.

## Ticket file and sprint/backlog disagree

The ticket file is authoritative for status; the sprint file for what's committed and the goal. Fix the mirror to match the authority, `os:` commit, run the validator. If the disagreement revealed a process miss (e.g., a handover that skipped the sprint update), note it in sprint Notes for the retro.

## Work reached `done` without real acceptance

Symptoms: `accepted_by` equals `implemented_by` (the validator catches this), or acceptance "evidence" is just the implementer's claims restated. Fix: status back to `in-acceptance` (`os:` commit, reason in the Work Log), then a genuinely fresh session runs `acceptance-test`. Don't quietly leave it — this is the most corrosive shortcut the framework guards against.

## An agent is improvising process

It invented a workflow, skipped a claim, wrote a board in `apps/`, or edited criteria. Point it at the skill for the activity and the precedence order (root `CLAUDE.md` → `project-os/README.md`). Undo out-of-place artifacts; anything that revealed a genuine skill gap goes to the retro. Repeat offenses usually mean the skill's *When to Use* is unclear — fix the skill, not just the agent.

## Worktree/branch litter

```bash
git worktree list          # what exists
git worktree remove <path> # merged tickets shouldn't keep one
git worktree prune         # clean deleted paths
git branch --merged main   # branches safe to delete
```

A branch outliving its ticket's sprint is a blocker signal, not litter — check the ticket before deleting anything unmerged. Recover an interrupted ticket from its branch + Work Log.

## "No remote" confusion / solo mode

Fresh copies have no `origin`, so pushes fail and claim collision-detection is void. Either configure the remote (normal mode) or acknowledge [solo mode](../standards/GIT.md) — one agent at a time, recorded in `PROJECT.md` §6. Never run two concurrent agents without a remote.

## An escalation got answered but work is still blocked

Almost always: the answer lived only in chat. Write it into the ticket's Work Log (attributed, dated), clear the blocker entry, and the next `pick-up-ticket`/session can proceed. See [WoW §13 — Answers](../governance/WAY_OF_WORKING.md).

## Governance edit was rejected / seems impossible

Governance paths travel lane 2 by design: branch, PR, human review (CODEOWNERS-protected where configured). If you're trying to change a rule mid-ticket to get unblocked — stop; that's the pattern the protection exists for. Record the conflict, escalate, finish or park the ticket under the current rules.

## Something not covered here

State the symptom in the sprint's Notes (so the retro sees it), then reason from the precedence order: WoW → PROJECT.md → ADRs → DoR/DoD → sprint → ticket → standards → skills → judgment. When documentation contradicts observable reality, reality wins for the task at hand — and the discrepancy gets recorded.
