# ADR 0003: Known Consistency Gap Between PostgreSQL and Qdrant

## Status
Accepted (as a known limitation, not resolved)

## Context
`POST /documents` writes to PostgreSQL first, then upserts each chunk's
vector to Qdrant. `DELETE /documents/{id}` does the reverse — Qdrant
first, then PostgreSQL. Neither operation is wrapped in a distributed
transaction (there isn't a simple one available across two different
storage engines), so a failure partway through leaves the two stores
inconsistent — in different ways depending on the direction:
- Upload: Postgres succeeds, Qdrant upsert fails › a document exists but
  is never retrievable by search (invisible, not harmful).
- Delete: Qdrant delete succeeds, Postgres delete fails › a document still
  "exists" in Postgres but can never be found by search again (stale,
  not harmful, but confusing — this project reproduced exactly this
  scenario, by accident, while manually recreating the Qdrant collection
  to change embedding dimensions during provider switching).

## Decision
This gap is **not** resolved with an Outbox pattern (as PaySync does for
its own, structurally similar DB+message-broker consistency problem) in
this project. The reasoning for each write direction was chosen so that
a partial failure leaves the system in a safely inert state (a document
that can't be found) rather than a harmful one (not a case of duplicated
financial transactions, as in PaySync), making the extra complexity of
an Outbox-style fix lower priority for a portfolio-scope project.

## Consequences
- A document can become "invisible" (exists in Postgres, missing from
  Qdrant) or "orphaned" (missing from Postgres, impossible — Delete order
  prevents this specific case, but a Qdrant delete followed by a Postgres
  failure leaves a stale, harmless row) under rare failure conditions.
- A production system handling real user data would most likely apply
  the same Outbox pattern used in PaySync here too, or adopt a
  reconciliation job that periodically diffs the two stores.
- Documented explicitly rather than silently left as a bug — the
  dimension-mismatch incident during development is cited as a concrete,
  observed instance of this exact class of problem.