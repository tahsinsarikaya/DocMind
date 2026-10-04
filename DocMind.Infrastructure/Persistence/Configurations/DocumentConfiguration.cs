using DocMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocMind.Infrastructure.Persistence.Configurations
{
    public class DocumentConfiguration : IEntityTypeConfiguration<Document>
    {
        public void Configure(EntityTypeBuilder<Document> builder)
        {
            builder.ToTable("documents");
            builder.HasKey(d => d.Id);

            builder.Property(d => d.Title).IsRequired().HasMaxLength(300);
            builder.Property(d => d.Content).IsRequired();

            builder.Metadata
                .FindNavigation(nameof(Document.Chunks))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);

            builder.HasMany(d => d.Chunks)
                .WithOne()
                .HasForeignKey(c => c.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
