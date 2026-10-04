# ADR 0001: Qdrant as a Dedicated Vector Store (Not pgvector)

## Status
Accepted

## Context
Vector similarity search can be added to PostgreSQL via the pgvector
extension, or handled by a dedicated vector database (Qdrant, Pinecone,
Weaviate, etc.). PostgreSQL is already used elsewhere in this portfolio
(PaySync, CoreCRM), which made pgvector tempting by default.

## Decision
This project deliberately evaluated pgvector against a dedicated vector
database on technical merit — not on prior familiarity — and chose
**Qdrant**. Document/DocumentChunk *metadata* (title, content, position)
stays in PostgreSQL via EF Core; the embeddings themselves live only in
Qdrant, which is purpose-built for vector similarity search.

This is an explicit instance of **polyglot persistence**: using the
right storage engine per data shape, rather than defaulting to a single
database because it was already in use elsewhere.

## Consequences
- Two systems must be kept consistent (see ADR 0003) instead of one.
- Local development requires running Qdrant alongside PostgreSQL
  (docker-compose), a small extra operational cost.
- Vector search scales independently of the relational workload, and
  Qdrant's dashboard gives direct visibility into collection health,
  which pgvector (as a plain Postgres column) would not.
- At this project's scale (a demo/portfolio project), either approach
  would have worked technically — this choice favors the architecture
  that would hold up better at a larger scale, and demonstrates a
  deliberate technology choice rather than defaulting to a familiar one.