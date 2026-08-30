# Security Standards

Baseline secure-development rules, applied by every persona and owned by the Security Engineer persona. Security findings are never silently accepted: they are fixed, or ticketed with severity and a human-approved deferral.

## Universal rules

### Secrets

- No secrets, credentials, tokens, or private keys in the repository — ever, including in history, tests, fixtures, and documentation examples. Use the project's secret store / environment injection (defined below).
- A leaked secret is treated as compromised: rotate first, then clean up.

### Input & data

- All external input (users, APIs, files, queues) is validated at the boundary; injection-safe APIs (parameterized queries, safe templating) are mandatory.
- Personal data is identified in tickets during refinement (DoR conditional item), minimized, and never written to logs.
- Authentication and authorization changes always get Security-persona review during refinement and acceptance.

### Dependencies

- New dependencies are checked for maintenance health and known vulnerabilities before adoption; the check is noted in the ticket Work Log.
- Vulnerability reports from tooling are triaged, not muted: fix, upgrade, or ticket with severity.

### Design

- Least privilege for services, tokens, and CI jobs.
- Security-relevant events (authn/authz failures, privilege changes) are logged — without secrets or personal data.
- Threats are considered during refinement for any ticket touching auth, money, personal data, file handling, or external input; the DoR requires naming the concern on the ticket.

## Escalation

The Security persona escalates to a human more readily than any other persona: unclear privacy implications, potential compliance impact, or a vulnerability in production always stop autonomous work per [WoW §13](../governance/WAY_OF_WORKING.md).

## Project-specific rules

Set at bootstrap 2026-08-30.

- **Secret management mechanism** `[default]`: local development uses .NET user-secrets or an untracked `.env` consumed by Compose; a committed `.env.example` documents every required variable with placeholder values. Container configuration arrives as environment variables. No secret — including Duende signing keys, client secrets, and database passwords — is ever committed. Development signing keys are generated locally, never checked in. A production secret store is `[open]`: nothing is deployed yet, and this must be settled before anything is.
- **Authentication & authorization** `[confirmed]`: Duende IdentityServer is the only issuer of tokens; the API is a resource server that validates JWT bearer tokens and **never** handles or stores credentials. Authorization uses **global roles `admin` and `member`**, read from the token claim on every request and never persisted — company-wide, never per project. A missing or unrecognised role claim is refused, never defaulted to `member`: that default is a plausible-looking line of code and a real authorisation hole. Any change touching token validation, scopes, or roles gets Security-persona review during both refinement and acceptance (universal rule above), and never ships without tests that prove the *negative* case — that an unauthorised caller is refused. Global roles mean a single check guards each endpoint: get it wrong and there is no second layer behind it.
- **Never disable authentication to make a test or a local run work.** Use a test authentication handler scoped to the test host ([TESTING.md](TESTING.md)) `[default]`.
- **Compliance/regulatory requirements: `[open]` (`PROJECT.md` Q8).** No regime was stated, but this is an **internal company tool holding real employees' names and email addresses** — that is personal data belonging to identifiable people, and a PoC label does not by itself exempt it. Before real employee data is loaded, confirm with the company what applies (GDPR or equivalent) and whether a PoC is covered by an existing assessment. Until then: minimise what is stored, never log it, and do not treat "nobody mentioned compliance" as clearance.
- **Dependency scanning tooling** `[default]`: `dotnet list package --vulnerable --include-transitive` run before adding a dependency and periodically; findings are fixed, upgraded, or ticketed with severity — never muted. NuGet lock files pin versions. No automated scanning service exists (no CI — `PROJECT.md` Q6).
- **Data classification & retention** `[default]` / `[open]`: the only personal data anticipated is user display names and email addresses from the identity provider, plus whatever users type into issue titles, descriptions, and comments — treat free-text fields as potentially containing personal data and never log their contents. Retention, deletion, and export flows are **`[open]`** — none are designed. A ticket introducing user-data deletion or export must settle this first.
- **Input validation** `[confirmed]`: request validation is declared in the OpenAPI specification and enforced by generated model binding plus explicit checks in controllers. A validation rule that exists only in code and not in the spec is a contract defect. All database access goes through EF Core's parameterised queries; any raw SQL must be parameterised and justified in the ticket's Work Log.
