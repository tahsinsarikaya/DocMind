# DocMind

A RAG (Retrieval-Augmented Generation) document Q&A service: upload
documents, ask questions, get answers grounded in your own content — with
source citations. Built as a portfolio project with pluggable LLM
providers, so the full pipeline runs with zero API keys and zero cost
(via a local Ollama model or a deterministic Fake provider for CI).

## What this project demonstrates

- **A complete RAG pipeline** — chunking, embedding, vector search, and
  grounded answer generation, wired together end to end
- **Polyglot persistence done deliberately, not by default** — PostgreSQL
  for relational metadata, Qdrant (a dedicated vector database) for
  embeddings, chosen on technical merit rather than reusing a database
  already used elsewhere in this project group
  (see [ADR 0001](docs/adr/0001-qdrant-over-pgvector.md))
- **Pluggable embedding/chat providers** behind domain interfaces — a
  `Fake` provider needs no setup at all; a real `Ollama` provider runs
  fully locally, for free
  (see [ADR 0004](docs/adr/0004-pluggable-providers.md))
- **An honestly documented consistency gap** between the two data stores,
  reasoned through rather than ignored
  (see [ADR 0003](docs/adr/0003-dual-write-consistency-gap.md))
- **Real operational findings from running a local LLM**, not just
  "it works on my machine" — cold-start timing was measured, a
  readiness endpoint was added, and model choice was settled by a
  measured quality comparison, not a guess
  (see [ADR 0005](docs/adr/0005-ollama-cold-start.md))
- 20 automated tests spanning pure algorithmic logic (chunking, prompt
  building), deterministic unit tests, and Testcontainers-based
  integration tests against a real Qdrant instance

## Architecture

    Client
      |
      |  POST /documents            (upload + index a document)
      |  POST /query                (ask a question)
      |  DELETE /documents/{id}     (remove a document)
      v
    DocMind.Api
      |                                    |
      |  chunk -> embed -> store            |  embed question -> search -> prompt -> answer
      v                                    v
    PostgreSQL                          Qdrant                    IChatService
    documents                           document_chunks           (Fake | Ollama)
    document_chunks (metadata only,     (vectors + payload)
     no embedding column)

### Projects

| Project | Responsibility |
|---|---|
| `DocMind.Domain` | `Document`/`DocumentChunk` entities, `TextChunker`, `RagPromptBuilder`, and the `IEmbeddingService`/`IChatService`/`IVectorStore` abstractions — no infrastructure dependencies |
| `DocMind.Infrastructure` | EF Core + PostgreSQL (metadata only), Qdrant vector store, `Fake` and `Ollama` provider implementations |
| `DocMind.Api` | HTTP API for uploading documents and asking questions; readiness/warmup handling; global exception handling |
| `DocMind.Tests` | Unit tests for chunking/prompting/fake-embedding logic + Testcontainers-based integration tests against real Qdrant |

### Architecture Decision Records

- [0001 — Qdrant as a Dedicated Vector Store (Not pgvector)](docs/adr/0001-qdrant-over-pgvector.md)
- [0002 — RetrievedChunk as a Separate Type from DocumentChunk](docs/adr/0002-retrieved-chunk-split.md)
- [0003 — Known Consistency Gap Between PostgreSQL and Qdrant](docs/adr/0003-dual-write-consistency-gap.md)
- [0004 — Pluggable Embedding/Chat Providers Behind Domain Interfaces](docs/adr/0004-pluggable-providers.md)
- [0005 — Ollama Cold-Start Mitigation via Warmup and Readiness](docs/adr/0005-ollama-cold-start.md)

## Running locally

**Prerequisites:** .NET 10 SDK, Docker Desktop

1. Start PostgreSQL and Qdrant (and Ollama, if you want real answers
   instead of the Fake provider):

   docker compose up -d

2. If using Ollama, pull the models once:

   docker exec -it docmind-ollama ollama pull all-minilm
   docker exec -it docmind-ollama ollama pull llama3.2

3. Apply database migrations:

   dotnet ef database update --startup-project ../DocMind.Api

4. Set the provider in `appsettings.json` (`Fake` needs nothing further;
   `Ollama` requires step 2 above):

   "Providers": { "Embedding": "Ollama", "Chat": "Ollama" }

5. Run the API:

   cd DocMind.Api
   dotnet run

6. If using Ollama, wait for `GET /health/ready` to return `200` before
   sending real questions — the first run pays a one-time model
   load cost of roughly 1-3 minutes; `POST /query` will return a fast,
   clear `503` rather than hanging if you query too early.

### Trying it out

Upload a document:

    POST https://localhost:<port>/documents
    Content-Type: application/json

    {
      "title": "Example",
      "content": "Your document text here..."
    }

Ask a question:

    POST https://localhost:<port>/query
    Content-Type: application/json

    { "question": "What does this document say about X?", "topK": 3 }

The response includes the answer plus which chunks (with similarity
scores) were used to produce it.

## Running tests

    dotnet test

Requires Docker to be running — the Qdrant integration tests spin up a
real Qdrant container via Testcontainers. Ollama is intentionally **not**
part of the automated test suite (too slow and heavy to run reliably in
CI) — the `Fake` provider covers the pipeline's logic instead; Ollama was
verified manually during development (see ADR 0005 for the actual
findings from that process).

## Known simplifications

This is a portfolio project, not a production system:

- No authentication/authorization on the API
- `Document`/`Qdrant` writes are not transactionally consistent — see
  [ADR 0003](docs/adr/0003-dual-write-consistency-gap.md) for the exact
  failure modes and why this wasn't fixed with an Outbox pattern here
- Changing the embedding provider requires manually recreating the
  Qdrant collection and reindexing every document (embedding dimension
  is fixed at collection-creation time)
- No pagination on any list-style endpoint
- Ollama warmup has no retry — if it fails at startup, the app needs a
  restart (see ADR 0005)
- Chunking is character-based, not token-based, since token-accurate
  counting requires a provider-specific tokenizer
