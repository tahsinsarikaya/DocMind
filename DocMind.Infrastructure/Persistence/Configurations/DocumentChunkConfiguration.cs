using DocMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocMind.Infrastructure.Persistence.Configurations
{
    public class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
    {
        public void Configure(EntityTypeBuilder<DocumentChunk> builder)
        {
            builder.ToTable("document_chunks");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Content).IsRequired();

            builder.HasIndex(c => new { c.DocumentId, c.Position }).IsUnique();

            // Embedding'i BİLEREK PostgreSQL'e yazmıyoruz — Qdrant tek doğruluk kaynağı.
            // Domain nesnesi hâlâ embedding'i bellekte tutuyor (constructor'da zorunlu),
            // ama bu kolon persistence katmanında yok sayılıyor.
            builder.Ignore(c => c.Embedding);
        }
    }
}
