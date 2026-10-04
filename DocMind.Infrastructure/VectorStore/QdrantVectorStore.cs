using DocMind.Domain.Abstractions;
using DocMind.Domain.Entities;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocMind.Infrastructure.VectorStore
{
    public class QdrantVectorStore : IVectorStore
    {
        private readonly QdrantClient _client;
        private const string CollectionName = "document_chunks";

        public QdrantVectorStore(QdrantClient client)
        {
            _client = client;
        }

        public async Task EnsureCollectionExistsAsync(int vectorSize, CancellationToken cancellationToken = default)
        {
            var collections = await _client.ListCollectionsAsync(cancellationToken: cancellationToken);
            if (collections.Contains(CollectionName))
                return;

            await _client.CreateCollectionAsync(
                CollectionName,
                new VectorParams { Size = (ulong)vectorSize, Distance = Distance.Cosine },
                cancellationToken: cancellationToken);
        }

        public async Task UpsertAsync(DocumentChunk chunk, CancellationToken cancellationToken = default)
        {
            var point = new PointStruct
            {
                Id = new PointId { Uuid = chunk.Id.ToString() },
                Vectors = chunk.Embedding
            };
            point.Payload["documentId"] = chunk.DocumentId.ToString();
            point.Payload["position"] = chunk.Position;
            point.Payload["content"] = chunk.Content;

            await _client.UpsertAsync(CollectionName, new[] { point }, cancellationToken: cancellationToken);
        }

        public async Task<IReadOnlyList<ScoredChunk>> SearchAsync(float[] queryEmbedding, int topK, CancellationToken cancellationToken = default)
        {
            var results = await _client.SearchAsync(
                CollectionName,
                queryEmbedding,
                limit: (ulong)topK,
                cancellationToken: cancellationToken);

            return results.Select(r =>
            {
                var chunk = new RetrievedChunk(
                    Guid.Parse(r.Id.Uuid),
                    Guid.Parse(r.Payload["documentId"].StringValue),
                    (int)r.Payload["position"].IntegerValue,
                    r.Payload["content"].StringValue);

                return new ScoredChunk(chunk, r.Score);
            }).ToList();
        }

        public async Task DeleteByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            await _client.DeleteAsync(
                CollectionName,
                new Filter
                {
                    Must = { new Condition { Field = new FieldCondition { Key = "documentId", Match = new Match { Keyword = documentId.ToString() } } } }
                },
                cancellationToken: cancellationToken);
        }
    }
}
