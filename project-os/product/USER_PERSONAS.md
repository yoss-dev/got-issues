# User Personas

These are the *users of the product*. The delivery team's working personas are in [`governance/PERSONAS.md`](../governance/PERSONAS.md) — do not confuse the two.

Tickets and refinement reference these personas by name ("As Priya the integrator…"). Keep the set small — only personas whose needs actually differ.

> **Status:** the *audience* is confirmed — the company's own engineers and their internal automation (`PROJECT.md` §2). The persona **details** below are still drafted from the product shape, **not** from user research: no one has interviewed an actual internal user. Treat the roles as real and the specific goals and frustrations as `[assumption]`.

## Format

```markdown
## <Name> — <one-line role>

- **Context:** where/when they use the product
- **Goals:** what they are trying to achieve
- **Frustrations:** what gets in their way today
- **Proficiency:** relevant technical/domain skill level
- **Key scenarios:** the 2–3 situations that matter most
```

## Personas

## Sam — engineer tracking the team's work `[assumption]`

- **Context:** an engineer inside the company using Got Issues to track work on the team's projects. During the PoC, also the person running the stack under Docker Compose.
- **Goals:** capture work as issues under a project, see what is in flight and what is next, and keep the history of why something changed.
- **Frustrations:** the current third-party tracker is outside the company's control, and its structure is heavier than the team needs; a flat to-do list loses the structure that makes work trackable across projects.
- **Proficiency:** expert developer; comfortable with `curl`, containers, and reading an OpenAPI document directly.
- **Key scenarios:**
  1. Start the whole stack from a clean clone and authenticate.
  2. Create a project and file issues against it, then move them through states as work progresses.
  3. Look up what is assigned and unresolved right now.

## Priya — the internal integrator building against the API `[assumption]`

- **Context:** an engineer on the company's internal tooling, writing a script, bot, or service that reads and writes issues — CI posting failures as issues, a dashboard, a chat command. The reason the API is the product.
- **Goals:** get a generated client from the published specification and be productive without reading implementation code; trust that the documented behaviour is the actual behaviour.
- **Frustrations:** trackers whose published API drifts from the docs; undocumented pagination, error shapes, and auth scopes discovered only in production; rate limits and API-key handling on a service the company does not control.
- **Proficiency:** expert developer; consumes OpenAPI documents with code generators as a matter of course.
- **Key scenarios:**
  1. Generate a client from the specification and authenticate a machine client via OAuth client credentials.
  2. Page through issues in a project and filter by status or assignee.
  3. Create an issue and add a comment from an automated process, handling errors predictably.
