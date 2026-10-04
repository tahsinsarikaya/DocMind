using System;
using System.Collections.Generic;
using System.Text;

namespace DocMind.Domain.Entities
{
    public class DocumentChunk
    {
        public Guid Id { get; private set; }
        public Guid DocumentId { get; private set; }
        public int Position { get; private set; }
        public string Content { get; private set; } = default!;
        public float[] Embedding { get; private set; } = default!;

        private DocumentChunk() { } // EF Core için

        public DocumentChunk(Guid id, Guid documentId, int position, string content, float[] embedding)
        {
            if (position < 0)
                throw new ArgumentOutOfRangeException(nameof(position), "Position cannot be negative.");

            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Chunk content cannot be empty.", nameof(content));

            if (embedding is null || embedding.Length == 0)
                throw new ArgumentException("Embedding cannot be empty.", nameof(embedding));

            Id = id;
            DocumentId = documentId;
            Position = position;
            Content = content;
            Embedding = embedding;
        }
    }
}
