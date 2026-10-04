# ADR 0002: RetrievedChunk as a Separate Type from DocumentChunk

## Status
Accepted

## Context
`IVectorStore.SearchAsync` originally returned `DocumentChunk`, the same
entity used elsewhere in the domain. But `DocumentChunk`'s constructor
requires an embedding (a deliberate domain rule — a chunk without an
embedding is invalid). A vector search result, however, only returns a
similarity score and the payload (content, position, document id) — not
the vector itself. Reusing `DocumentChunk` for search results meant
fabricating a fake embedding just to satisfy the constructor, silently
violating the very invariant the entity was designed to enforce.

## Decision
A new type, `RetrievedChunk` (id, documentId, position, content — no
embedding), represents what a vector search actually returns.
`ScoredChunk` pairs it with a similarity score. `RagPromptBuilder` was
updated to consume `RetrievedChunk` instead of `DocumentChunk`.

## Consequences
- `DocumentChunk`'s "always has a valid embedding" invariant is no longer
  compromised anywhere in the codebase.
- Two related-but-distinct concepts (a chunk being stored vs. a chunk
  being retrieved) now have their own types, which is slightly more
  surface area but removes an entire class of possible confusion.
- This is the second time in this group of portfolio projects a similar
  shortcut was caught and corrected by separating what looked like "the
  same data" into two purpose-built shapes (compare to why
  `EntityDefinitionDto` exists in CoreCRM for a different reason — cache
  serialization rather than domain-invariant protection).