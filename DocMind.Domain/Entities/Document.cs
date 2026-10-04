using System;
using System.Collections.Generic;
using System.Text;

namespace DocMind.Domain.Entities
{
    public class Document
    {
        private readonly List<DocumentChunk> _chunks = new();

        public Guid Id { get; private set; }
        public string Title { get; private set; } = default!;
        public string Content { get; private set; } = default!;
        public DateTimeOffset CreatedAt { get; private set; }

        public IReadOnlyCollection<DocumentChunk> Chunks => _chunks.AsReadOnly();

        private Document() { } // EF Core için

        public Document(Guid id, string title, string content)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title cannot be empty.", nameof(title));

            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Content cannot be empty.", nameof(content));

            Id = id;
            Title = title;
            Content = content;
            CreatedAt = DateTimeOffset.UtcNow;
        }

        public void AddChunk(DocumentChunk chunk)
        {
            if (chunk.DocumentId != Id)
                throw new InvalidOperationException($"Chunk belongs to document '{chunk.DocumentId}', not '{Id}'.");

            if (_chunks.Any(c => c.Position == chunk.Position))
                throw new InvalidOperationException($"A chunk at position {chunk.Position} already exists on document '{Title}'.");

            _chunks.Add(chunk);
        }
    }
}
