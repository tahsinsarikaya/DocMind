using DocMind.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocMind.Domain.Abstractions
{
    public record RetrievedChunk(Guid Id, Guid DocumentId, int Position, string Content);

    public record ScoredChunk(RetrievedChunk Chunk, float Score);

    public interface IVectorStore
    {
        Task UpsertAsync(DocMind.Domain.Entities.DocumentChunk chunk, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ScoredChunk>> SearchAsync(float[] queryEmbedding, int topK, CancellationToken cancellationToken = default);

        Task DeleteByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);
    }
}
