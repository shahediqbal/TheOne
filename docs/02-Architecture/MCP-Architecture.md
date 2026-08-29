# MCP Architecture Document

Expands `ADR-002-MCP-ReadOnly-First.md`. Covers `TheOne.McpServer` in detail.

## Purpose

Give AI/research clients controlled, read-only access to approved knowledge in the Digital Library — without exposing write paths to original content, matching the project's core preservation principle.

## Position in the Solution

```
MCP Client (Claude, or other MCP-compatible agent)
        |  JWT Bearer token
        v
   TheOne.API  (same Identity/JWT auth as all other clients)
        |
        v
 TheOne.McpServer  (MCP protocol handler)
        |
        v
 TheOne.Application  (same use-case layer as the REST API — no bypass)
        |
        v
 TheOne.Domain / TheOne.Persistence
```

`TheOne.McpServer` does **not** talk to the database directly. It calls into `TheOne.Application` the same way `TheOne.API` controllers do, so every MCP tool call is subject to the same authorization, validation, and audit logging as a normal API request. This is the architectural enforcement of "AI will not directly modify or publish original content" — it isn't a policy note, it's a missing write-side use case: no `ApproveContent` or `PublishContent` handler is exposed to the MCP layer at all in the initial phase.

## Authentication

- Same JWT scheme as the REST API (v5 §6 / Authentication-Authorization-Detailed-Design.md).
- MCP clients authenticate as a **service account** with a dedicated role (`AIResearchClient`) scoped only to the read-only permissions below — never as an impersonated end user.
- Every MCP tool call is logged to `AuditLogs` with `ActorUserId` = the service account, so usage is fully traceable.

## Tools (Initial, Read-Only)

| Tool | Input | Output | Backing Use Case |
|---|---|---|---|
| `SearchKnowledge` | query string, optional filters (book, language, date range) | ranked list of `ContentBlock` excerpts + references | `SearchLibraryQuery` (same handler the reader UI's search box calls) |
| `GetBookReference` | bookId | book metadata (title, author, volumes) | `GetBookQuery` |
| `GetChapterContent` | chapterId | approved `ContentVersion` text only — never draft/in-review versions | `GetChapterContentQuery` |
| `FindRelatedTopics` | topic/keyword | related books/chapters by tag or FTS proximity | `FindRelatedQuery` |
| `GetAuthorInformation` | authorId or name | author metadata, list of works | `GetAuthorQuery` |

All five map to existing or trivially-extended read query handlers — no new business logic, just an MCP-shaped entry point.

## Boundary Enforcement

- Tool handlers are hard-coded to call `IReadOnlyLibraryService` — an interface that only exposes query methods, with no write methods to accidentally call.
- Rate limiting applies to the MCP surface too (Admin-tier: user/service-account based), preventing a research client from being used to scrape the entire archive in one session.
- Only `Approved` content versions are queryable — `InReview`/`Draft`/`Rejected` content is invisible to MCP regardless of the requester's role, since the service account has no elevated content-visibility permission.

## Future Scope (not built in Phase 5 initial release)

- **Research assistance** — summarization over already-retrieved content (still read-only).
- **Draft generation** — MCP-assisted first-draft translations/summaries land in a `Draft` `ContentVersion`, never auto-published; a human reviewer approves exactly as with any other ingestion.
- **Curation support** — suggested tags/related-topics that a Content Reviewer accepts or rejects; the AI proposes, it never commits.

Any future write-adjacent tool requires: (1) output lands in `Draft` status only, (2) a human approval step identical to the existing content-review workflow, (3) an explicit ADR before implementation — this document does not pre-authorize it.

## Deployment

- `TheOne.McpServer` runs as its own process/container (per the solution structure), independently scalable from `TheOne.API` since research query load and web traffic have different scaling profiles.
- Exposed only over the internal network / API gateway — not a separate public ingress — so the same CORS, HTTPS, and gateway-level protections apply.
