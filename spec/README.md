# spec/

The **contract**. `openapi.yaml` (OpenAPI 3.1) is hand-authored here *before* any implementation, and is the only place the API surface is designed — resources, schemas, status codes, error shapes, and auth scopes.

Server contracts and clients are generated from this file into [`libs/`](../libs/README.md) and are never hand-edited; see [ADR-0004](../project-os/architecture/adr/ADR-0004-contract-first-openapi-code-generation.md) and the contract-first rule in [engineering standards](../project-os/standards/ENGINEERING.md).

Because the specification *is* the product's user-facing documentation ([documentation standards](../project-os/standards/DOCUMENTATION.md)), every operation and schema carries a description written for someone who has never seen the code.

> The specification itself does not exist yet — it arrives with the first implementation ticket.
