# ADR 0004: Pluggable Embedding/Chat Providers Behind Domain Interfaces

## Status
Accepted

## Context
This project was built without an API key for any commercial LLM
provider. Rather than blocking on that, the embedding and chat
responsibilities were abstracted (`IEmbeddingService`, `IChatService` —
defined in `DocMind.Domain`, not `DocMind.Infrastructure`, unlike this
project group's usual pattern, because the RAG orchestration logic that
calls them needs to do so without depending on infrastructure).

## Decision
Two implementations exist: `Fake` (deterministic, no network calls, used
for CI and local tests) and `Ollama` (a real, local, free LLM runtime).
The active provider for each concern is selected independently via
configuration (`Providers:Embedding`, `Providers:Chat`), resolved through
a `switch` in `Program.cs` that **fails fast** (throws at startup) on an
unrecognized provider name, rather than silently falling back to a
default.

## Consequences
- The full pipeline (upload › chunk › embed › store › query › search ›
  answer) can be exercised end-to-end with zero external dependencies or
  API keys, via `Fake` — valuable for anyone evaluating this repo without
  setting anything up.
- Embedding and chat providers can differ independently (e.g. a cheap/
  fast local embedding model with a stronger hosted chat model), since
  they're selected separately.
- Adding Claude or OpenAI as providers later is additive — implement the
  same two interfaces, add a `case` to each switch. No existing code
  changes.
- Switching the *embedding* provider is not free: the embedding
  dimension is baked into the Qdrant collection at creation time (see
  `EnsureCollectionExistsAsync`), so changing providers requires
  recreating the collection and **reindexing every existing document** —
  this was discovered the hard way mid-development (see ADR 0003).