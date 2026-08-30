# Skills

Skills are the executable procedures of this framework: step-by-step operational instructions for every repeatable delivery activity. An agent asked to "refine T-0042" opens `refine-ticket/SKILL.md` and follows it — it does not improvise its own process.

## How an agent executes a skill

1. Open the skill's `SKILL.md`.
2. Load **only** the files listed under *Context to Load* (plus anything those files explicitly point at for the ticket in question). Do not read the whole repository.
3. Adopt the persona(s) named under *Active Persona(s)* and respect their authority boundaries in [`governance/PERSONAS.md`](../governance/PERSONAS.md).
4. Verify *Preconditions*. If one fails, stop and follow *Failure / Escalation* — do not push through.
5. Follow the *Procedure*, making only decisions the skill and persona permit, modifying only the files listed under *State Changes*.
6. Run the *Validation* checks — including the consistency validator (`python3 tools/validate-project-os/validate.py`) before pushing process-state commits — persist all outputs to the repository, and end with the state changes committed. **If you stop early for any reason, record where you got to (usually the ticket Work Log) first.**

Skill instructions sit at precedence level 8: every governance document, ADR, and ticket requirement overrides them. A skill that conflicts with governance is a bug — flag it via [`evolve-governance`](evolve-governance/SKILL.md).

## Skill format

Every `SKILL.md` uses the same sections: Purpose · When to Use · Active Persona(s) · Inputs · Preconditions · Context to Load · Procedure · Validation · Outputs · State Changes · Failure / Escalation · Example. New skills are created via `evolve-governance` and follow this format.

The YAML frontmatter (`name`, `description`) makes these skills directly loadable by agent harnesses. The monorepo root ships a `.claude/skills` symlink pointing at this directory, so Claude Code discovers every skill automatically (invoke as `/refine-ticket`, etc.); on Windows, replace the symlink with a directory junction or a copy kept in sync.

## Catalog

| Skill | Activity | Primary persona |
| --- | --- | --- |
| [`bootstrap-project`](bootstrap-project/SKILL.md) | Initialize the framework for a new project | SM |
| [`capture-idea`](capture-idea/SKILL.md) | Record a raw idea without commitment | PO |
| [`create-ticket`](create-ticket/SKILL.md) | Turn an understood need into a backlog ticket | PO |
| [`refine-ticket`](refine-ticket/SKILL.md) | Multi-perspective refinement toward Ready | BA |
| [`refinement-session`](refinement-session/SKILL.md) | Interactive batch refinement: rank, select, refine one at a time | PO |
| [`plan-sprint`](plan-sprint/SKILL.md) | Set a goal and commit ready work | PO |
| [`run-sprint`](run-sprint/SKILL.md) | Orchestrate the loop: drain the sprint, stop only for human decisions | SM |
| [`pick-up-ticket`](pick-up-ticket/SKILL.md) | Safely claim a ticket and plan the work | ENG |
| [`implement-ticket`](implement-ticket/SKILL.md) | Build within scope and standards | ENG |
| [`review-code`](review-code/SKILL.md) | Independent pre-merge engineering review | ENG |
| [`acceptance-test`](acceptance-test/SKILL.md) | Independently verify acceptance criteria | QA |
| [`complete-ticket`](complete-ticket/SKILL.md) | Final DoD gate and state updates | QA |
| [`create-adr`](create-adr/SKILL.md) | Record an architectural decision | ARCH |
| [`evolve-governance`](evolve-governance/SKILL.md) | Change the rules, safely | SM |
| [`retrospective`](retrospective/SKILL.md) | Close a sprint and improve the system | SM |
